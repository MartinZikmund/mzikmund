import { getSession, signOut } from "../Auth/SupabaseAuth";
import {
	ApiError,
	createComment,
	deleteComment,
	getComments,
	getReplies,
	reportComment,
	setVote,
	updateComment,
	type CommentDto,
	type CommentSort,
	type ReportReason,
} from "./CommentsApi";
import { bodyMarkup, renderComment } from "./CommentRenderer";
import { CommentTurnstile } from "./CommentTurnstile";
import { SignInDialog, type SignInResult } from "./SignInDialog";

const DraftKey = "mz-comment-draft";
const MaxLength = 4000;

interface CommentDraft {
	postId: string;
	parentId: string | null;
	content: string;
	/** True when the reader pressed Post and only sign-in stood in the way. */
	submit: boolean;
}

export class CommentsSection {
	private root!: HTMLElement;
	private list!: HTMLElement;
	private composer: HTMLFormElement | null = null;
	private textarea: HTMLTextAreaElement | null = null;
	private counter: HTMLElement | null = null;
	private submitButton: HTMLButtonElement | null = null;
	private statusEl: HTMLElement | null = null;
	private parentInput: HTMLInputElement | null = null;
	private replyToBanner: HTMLElement | null = null;
	private replyToName: HTMLElement | null = null;
	private loadMoreButton: HTMLButtonElement | null = null;
	private totalEl: HTMLElement | null = null;
	private identityEl: HTMLElement | null = null;
	private identityName: HTMLElement | null = null;

	private signIn: SignInDialog | null = null;
	private reportDialog: HTMLDialogElement | null = null;
	private reportTargetId: string | null = null;

	private readonly captcha = new CommentTurnstile();
	/** Comments rendered by this module, so an edit can start from the raw markdown. */
	private readonly known = new Map<string, CommentDto>();

	private postId = "";
	private sort: CommentSort = "top";
	private page = 1;
	private total = 0;

	public init(): void {
		if (document.readyState === "loading") {
			document.addEventListener("DOMContentLoaded", () => void this.initialize());
		} else {
			void this.initialize();
		}
	}

	private async initialize(): Promise<void> {
		const root = document.querySelector<HTMLElement>("[data-comments-root]");
		const list = root?.querySelector<HTMLElement>("[data-comments-list]");
		if (!root || !list) {
			return;
		}

		this.root = root;
		this.list = list;
		this.composer = root.querySelector<HTMLFormElement>("[data-comment-composer]");
		this.textarea = root.querySelector<HTMLTextAreaElement>("[data-comment-content]");
		this.counter = root.querySelector<HTMLElement>("[data-comment-counter]");
		this.submitButton = root.querySelector<HTMLButtonElement>("[data-comment-submit]");
		this.statusEl = root.querySelector<HTMLElement>("[data-comment-status]");
		this.parentInput = root.querySelector<HTMLInputElement>("[data-comment-parent]");
		this.replyToBanner = root.querySelector<HTMLElement>("[data-comment-reply-to]");
		this.replyToName = root.querySelector<HTMLElement>("[data-comment-reply-to-name]");
		this.loadMoreButton = root.querySelector<HTMLButtonElement>("[data-load-more]");
		this.totalEl = root.querySelector<HTMLElement>("[data-comments-total]");
		this.identityEl = root.querySelector<HTMLElement>("[data-comment-identity]");
		this.identityName = root.querySelector<HTMLElement>("[data-comment-identity-name]");

		const dialogEl = root.querySelector<HTMLDialogElement>("[data-signin-dialog]");
		this.signIn = dialogEl ? new SignInDialog(dialogEl) : null;
		this.reportDialog = root.querySelector<HTMLDialogElement>("[data-report-dialog]");

		this.postId = root.dataset.postId ?? "";
		this.sort = root.dataset.sort === "new" ? "new" : "top";
		this.page = Number.parseInt(root.dataset.page ?? "1", 10) || 1;
		this.total = Number.parseInt(root.dataset.total ?? "0", 10) || 0;

		// Reply, report and the composer need JavaScript (they may have to sign the reader in).
		if (this.composer) {
			this.composer.hidden = false;
		}
		this.revealServerActions();

		this.wire();
		this.updateCounter();
		this.highlightDeepLink(true);

		const turnstileHost = root.querySelector<HTMLElement>("[data-comment-turnstile]");
		if (turnstileHost) {
			this.captcha.render(turnstileHost, turnstileHost.dataset.sitekey ?? "");
		}

		// The site has no cookie session, so the first paint always says hasUpvoted=false and
		// isOwn=false. A signed-in reader gets the real state by re-fetching the page they see.
		const session = await getSession();
		if (session) {
			await this.onSignedIn();
			await this.resumePendingDraft();
		} else {
			this.restoreDraftText();
		}
	}

	private revealServerActions(): void {
		for (const button of this.list.querySelectorAll<HTMLElement>("[data-comment-reply][hidden], [data-comment-report][hidden]")) {
			button.hidden = false;
		}
	}

	// --- Wiring --------------------------------------------------------------

	private wire(): void {
		this.textarea?.addEventListener("input", () => {
			this.updateCounter();
			this.saveDraft(false);
		});

		this.composer?.addEventListener("submit", (e) => {
			e.preventDefault();
			void this.submit();
		});

		this.root.querySelector<HTMLButtonElement>("[data-comment-cancel-reply]")
			?.addEventListener("click", () => this.setReplyTarget(null, null));

		this.root.querySelector<HTMLButtonElement>("[data-comment-signout]")
			?.addEventListener("click", () => void this.signOutReader());

		this.loadMoreButton?.addEventListener("click", () => void this.loadMore());

		// One delegated listener: comments are added and replaced constantly.
		this.list.addEventListener("click", (e) => {
			const target = e.target as HTMLElement;

			const upvote = target.closest<HTMLButtonElement>("[data-comment-upvote]");
			if (upvote) {
				void this.toggleVote(upvote);
				return;
			}

			const reply = target.closest<HTMLButtonElement>("[data-comment-reply]");
			if (reply) {
				this.startReply(reply);
				return;
			}

			const more = target.closest<HTMLButtonElement>("[data-load-replies]");
			if (more) {
				void this.loadReplies(more);
				return;
			}

			const edit = target.closest<HTMLButtonElement>("[data-comment-edit]");
			if (edit) {
				this.startEdit(edit);
				return;
			}

			const remove = target.closest<HTMLButtonElement>("[data-comment-delete]");
			if (remove) {
				void this.deleteOwn(remove);
				return;
			}

			const report = target.closest<HTMLButtonElement>("[data-comment-report]");
			if (report) {
				void this.openReport(report);
			}
		});

		this.reportDialog?.querySelector<HTMLButtonElement>("[data-report-send]")
			?.addEventListener("click", () => void this.sendReport());
	}

	// --- Sign-in -------------------------------------------------------------

	private returnUrl(): string {
		return `${window.location.origin}${window.location.pathname}${window.location.search}`;
	}

	/** Resolves "signed-in" immediately when a session exists, otherwise asks the reader to sign in. */
	private async ensureSignedIn(): Promise<SignInResult> {
		if (await getSession()) {
			return "signed-in";
		}

		const result = await this.signIn?.open(this.returnUrl()) ?? "cancelled";
		if (result === "signed-in") {
			await this.onSignedIn();
		}
		return result;
	}

	private async onSignedIn(): Promise<void> {
		const session = await getSession();
		if (this.identityEl && this.identityName && session) {
			this.identityName.textContent = `Signed in as ${session.user.email ?? "you"}`;
			this.identityEl.hidden = false;
		}
		await this.refreshCurrentPage();
	}

	private async signOutReader(): Promise<void> {
		await signOut();
		if (this.identityEl) {
			this.identityEl.hidden = true;
		}
		await this.refreshCurrentPage();
	}

	// --- Composer ------------------------------------------------------------

	private updateCounter(): void {
		if (this.counter && this.textarea) {
			this.counter.textContent = String(this.textarea.value.length);
		}
	}

	private startReply(button: HTMLElement): void {
		const article = button.closest<HTMLElement>("[data-comment-id]");
		const name = article?.querySelector<HTMLElement>(".comment__author")?.textContent ?? "";
		this.setReplyTarget(article?.dataset.commentId ?? null, name);
		this.composer?.scrollIntoView({ behavior: this.scrollBehavior(), block: "center" });
		this.textarea?.focus({ preventScroll: true });
	}

	private setReplyTarget(parentId: string | null, displayName: string | null): void {
		if (this.parentInput) {
			this.parentInput.value = parentId ?? "";
		}
		if (this.replyToBanner) {
			this.replyToBanner.hidden = parentId === null;
		}
		if (this.replyToName) {
			this.replyToName.textContent = displayName ? `Replying to ${displayName}` : "";
		}
	}

	private async submit(): Promise<void> {
		const content = this.textarea?.value.trim() ?? "";
		if (!content) {
			this.setStatus("Write something first.");
			return;
		}

		// WRITE-THEN-AUTH: the draft is stored BEFORE sign-in is requested, so it survives
		// the full OAuth redirect out to Google/Apple and back.
		this.saveDraft(true);

		const result = await this.ensureSignedIn();
		if (result === "cancelled") {
			this.saveDraft(false);
			return;
		}
		if (result === "redirecting") {
			return;
		}

		await this.postDraft(content, this.parentInput?.value || null);
	}

	private async postDraft(content: string, parentId: string | null): Promise<void> {
		this.busy(true);
		this.setStatus("Posting…");

		try {
			const token = await this.captcha.getToken();
			const created = await createComment(this.postId, content, parentId, token);
			this.insertCreated(created);
			this.clearDraft();
			if (this.textarea) {
				this.textarea.value = "";
			}
			this.updateCounter();
			this.setReplyTarget(null, null);
			this.setStatus("Posted.");
		} catch (error) {
			this.setStatus(this.describe(error, "Your comment could not be posted."));
		} finally {
			// The token is spent either way - Cloudflare rejects a replay.
			this.captcha.reset();
			this.busy(false);
		}
	}

	private insertCreated(created: CommentDto): void {
		this.root.querySelector("[data-comments-empty]")?.remove();

		const node = this.render(created, created.parentId !== null);
		if (created.parentId) {
			const parent = this.findArticle(created.parentId);
			const replies = parent?.querySelector<HTMLElement>(":scope > [data-comment-replies]");
			replies?.insertBefore(node, replies.querySelector(":scope > [data-load-replies]"));
		} else {
			// Newest first reads naturally for both orders; under "top" a fresh comment would
			// otherwise land on a page the reader has not loaded.
			this.list.prepend(node);
			this.total += 1;
			this.syncTotal();
		}

		this.highlight(node);
	}

	// --- Draft ---------------------------------------------------------------

	private saveDraft(submit: boolean): void {
		const content = this.textarea?.value ?? "";
		if (!content.trim()) {
			this.clearDraft();
			return;
		}

		const draft: CommentDraft = {
			postId: this.postId,
			parentId: this.parentInput?.value || null,
			content,
			submit,
		};

		try {
			window.sessionStorage.setItem(DraftKey, JSON.stringify(draft));
		} catch {
			// Private mode or a full quota: the draft is a convenience, never a reason to break posting.
		}
	}

	private readDraft(): CommentDraft | null {
		try {
			const raw = window.sessionStorage.getItem(DraftKey);
			if (!raw) {
				return null;
			}
			const draft = JSON.parse(raw) as CommentDraft;
			return draft.postId === this.postId ? draft : null;
		} catch {
			return null;
		}
	}

	private clearDraft(): void {
		try {
			window.sessionStorage.removeItem(DraftKey);
		} catch {
			// See saveDraft.
		}
	}

	private restoreDraftText(): void {
		const draft = this.readDraft();
		if (!draft || !this.textarea) {
			return;
		}
		this.textarea.value = draft.content;
		this.setReplyTarget(draft.parentId, this.authorOf(draft.parentId));
		this.updateCounter();
	}

	/** Runs on the OAuth return: a draft marked submit posts itself. */
	private async resumePendingDraft(): Promise<void> {
		const draft = this.readDraft();
		if (!draft) {
			return;
		}

		if (this.textarea) {
			this.textarea.value = draft.content;
		}
		this.setReplyTarget(draft.parentId, this.authorOf(draft.parentId));
		this.updateCounter();

		if (draft.submit) {
			await this.postDraft(draft.content.trim(), draft.parentId);
		}
	}

	private authorOf(commentId: string | null): string | null {
		if (!commentId) {
			return null;
		}
		return this.findArticle(commentId)?.querySelector<HTMLElement>(".comment__author")?.textContent ?? null;
	}

	// --- Voting --------------------------------------------------------------

	private async toggleVote(button: HTMLButtonElement): Promise<void> {
		const commentId = button.closest<HTMLElement>("[data-comment-id]")?.dataset.commentId;
		if (!commentId) {
			return;
		}

		const wantUpvote = button.getAttribute("aria-pressed") !== "true";

		const result = await this.ensureSignedIn();
		if (result !== "signed-in") {
			return;
		}

		// Signing in re-renders the list, so the clicked button may be gone.
		const fresh = this.findArticle(commentId)
			?.querySelector<HTMLButtonElement>(":scope > .comment__actions [data-comment-upvote]");
		if (!fresh || (fresh.getAttribute("aria-pressed") === "true") === wantUpvote) {
			return;
		}

		const countEl = fresh.querySelector<HTMLElement>("[data-upvote-count]");
		const previousCount = Number.parseInt(countEl?.textContent ?? "0", 10) || 0;

		// Optimistic: apply first, roll back on failure.
		this.paintVote(fresh, countEl, wantUpvote, previousCount + (wantUpvote ? 1 : -1));
		fresh.disabled = true;

		try {
			const updated = await setVote(commentId, wantUpvote);
			this.paintVote(fresh, countEl, updated.hasUpvoted, updated.upvoteCount);
		} catch (error) {
			this.paintVote(fresh, countEl, !wantUpvote, previousCount);
			this.flash(fresh, this.describe(error, "Your vote could not be saved."));
		} finally {
			fresh.disabled = false;
		}
	}

	private paintVote(button: HTMLButtonElement, countEl: HTMLElement | null, pressed: boolean, count: number): void {
		button.setAttribute("aria-pressed", pressed ? "true" : "false");
		button.querySelector("use")?.setAttribute("href", pressed ? "#i-hand-thumbs-up-fill" : "#i-hand-thumbs-up");
		if (countEl) {
			countEl.textContent = String(count);
		}
	}

	// --- Paging --------------------------------------------------------------

	private async refreshCurrentPage(): Promise<void> {
		try {
			const response = await getComments(this.postId, this.sort, this.page);
			this.list.replaceChildren(...response.data.map((comment) => this.render(comment, false)));
			this.total = response.totalCount;
			this.syncTotal();
			this.syncLoadMore();
			this.highlightDeepLink(false);
		} catch {
			// Keep the server-rendered list: still correct, just without the reader's own state.
		}
	}

	private async loadMore(): Promise<void> {
		if (!this.loadMoreButton) {
			return;
		}

		this.loadMoreButton.disabled = true;
		try {
			const response = await getComments(this.postId, this.sort, this.page + 1);
			for (const comment of response.data) {
				if (!this.findArticle(comment.id)) {
					this.list.appendChild(this.render(comment, false));
				}
			}
			this.page += 1;
			this.total = response.totalCount;
			this.syncTotal();
			this.syncLoadMore();
		} catch (error) {
			this.setStatus(this.describe(error, "More comments could not be loaded."));
		} finally {
			this.loadMoreButton.disabled = false;
		}
	}

	private syncTotal(): void {
		if (this.totalEl) {
			this.totalEl.textContent = String(this.total);
		}
	}

	private syncLoadMore(): void {
		if (this.loadMoreButton) {
			this.loadMoreButton.hidden = this.list.querySelectorAll(":scope > [data-comment-id]").length >= this.total;
		}
	}

	private async loadReplies(button: HTMLButtonElement): Promise<void> {
		const commentId = button.closest<HTMLElement>("[data-comment-id]")?.dataset.commentId;
		const container = button.parentElement;
		if (!commentId || !container) {
			return;
		}

		const page = Number.parseInt(button.dataset.replyPage ?? "1", 10) || 1;
		button.disabled = true;

		try {
			const response = await getReplies(commentId, page);
			for (const reply of response.data) {
				if (!this.findArticle(reply.id)) {
					container.insertBefore(this.render(reply, true), button);
				}
			}

			const shown = container.querySelectorAll(":scope > [data-comment-id]").length;
			if (shown >= response.totalCount || response.data.length === 0) {
				button.remove();
			} else {
				button.dataset.replyPage = String(page + 1);
				button.textContent = `Show more replies (${response.totalCount - shown})`;
			}
		} catch (error) {
			this.flash(button, this.describe(error, "Replies could not be loaded."));
		} finally {
			button.disabled = false;
		}
	}

	// --- Edit, delete and report ---------------------------------------------

	private startEdit(button: HTMLElement): void {
		const article = button.closest<HTMLElement>("[data-comment-id]");
		const known = article ? this.known.get(article.dataset.commentId ?? "") : undefined;
		const body = article?.querySelector<HTMLElement>(":scope > .comment__body");
		if (!article || !known || !body || article.querySelector(":scope > .comment__editor")) {
			return;
		}

		const editor = document.createElement("div");
		editor.className = "comment__editor";

		const input = document.createElement("textarea");
		input.className = "comment__editor-input";
		input.rows = 4;
		input.maxLength = MaxLength;
		input.value = known.content;
		input.setAttribute("aria-label", "Edit your comment");

		const actions = document.createElement("div");
		actions.className = "comment__editor-actions";

		const cancel = document.createElement("button");
		cancel.type = "button";
		cancel.className = "btn btn--subtle btn--sm";
		cancel.textContent = "Cancel";

		const save = document.createElement("button");
		save.type = "button";
		save.className = "btn btn--accent btn--sm";
		save.textContent = "Save";

		actions.append(cancel, save);
		editor.append(input, actions);
		body.hidden = true;
		body.after(editor);
		input.focus();

		const close = (): void => {
			editor.remove();
			body.hidden = false;
		};

		cancel.addEventListener("click", close);
		save.addEventListener("click", () => {
			const content = input.value.trim();
			if (!content) {
				this.flash(save, "A comment cannot be empty.");
				return;
			}

			save.disabled = true;
			updateComment(known.id, content)
				.then((updated) => {
					const merged: CommentDto = { ...known, ...updated, replies: known.replies };
					this.known.set(known.id, merged);
					body.innerHTML = bodyMarkup(merged);
					this.markEdited(article);
					close();
				})
				.catch((error: unknown) => {
					save.disabled = false;
					this.flash(save, this.describe(error, "Your comment could not be saved."));
				});
		});
	}

	private markEdited(article: HTMLElement): void {
		const identity = article.querySelector<HTMLElement>(":scope > .comment__identity");
		if (identity && !identity.querySelector(".comment__edited")) {
			const label = document.createElement("span");
			label.className = "comment__edited";
			label.textContent = "edited";
			identity.appendChild(label);
		}
	}

	private async deleteOwn(button: HTMLButtonElement): Promise<void> {
		const article = button.closest<HTMLElement>("[data-comment-id]");
		const commentId = article?.dataset.commentId;
		if (!commentId || !article) {
			return;
		}

		if (!window.confirm("Delete this comment?")) {
			return;
		}

		article.classList.add("is-busy");
		try {
			await deleteComment(commentId);
		} catch (error) {
			article.classList.remove("is-busy");
			this.flash(button, this.describe(error, "Your comment could not be deleted."));
			return;
		}

		if (article.dataset.parentId) {
			// A deleted reply is simply gone; only a top-level comment leaves a tombstone.
			article.remove();
			return;
		}

		const known = this.known.get(commentId);
		if (!known) {
			article.remove();
			return;
		}

		const tombstone = this.render({ ...known, status: "DeletedByAuthor", content: "", contentHtml: "", replies: [] }, false);
		const replies = article.querySelector(":scope > [data-comment-replies]");
		const slot = tombstone.querySelector(":scope > [data-comment-replies]");
		if (replies && slot) {
			slot.replaceWith(replies);
		}
		article.replaceWith(tombstone);
	}

	private async openReport(button: HTMLButtonElement): Promise<void> {
		const commentId = button.closest<HTMLElement>("[data-comment-id]")?.dataset.commentId;
		if (!commentId || !this.reportDialog) {
			return;
		}

		const result = await this.ensureSignedIn();
		if (result !== "signed-in") {
			return;
		}

		this.reportTargetId = commentId;
		const error = this.reportDialog.querySelector<HTMLElement>("[data-report-error]");
		if (error) {
			error.textContent = "";
		}
		const note = this.reportDialog.querySelector<HTMLTextAreaElement>("[data-report-note]");
		if (note) {
			note.value = "";
		}
		this.reportDialog.showModal();
	}

	private async sendReport(): Promise<void> {
		if (!this.reportTargetId || !this.reportDialog) {
			return;
		}

		const reason = (this.reportDialog.querySelector<HTMLSelectElement>("[data-report-reason]")?.value ?? "Other") as ReportReason;
		const note = this.reportDialog.querySelector<HTMLTextAreaElement>("[data-report-note]")?.value.trim() ?? "";
		const errorEl = this.reportDialog.querySelector<HTMLElement>("[data-report-error]");

		try {
			await reportComment(this.reportTargetId, reason, note.length > 0 ? note : null);
			this.reportDialog.close();
			const article = this.findArticle(this.reportTargetId);
			const target = article?.querySelector<HTMLElement>(":scope > .comment__actions [data-comment-report]");
			if (target) {
				this.flash(target, "Thanks, the report was sent.");
			}
		} catch (error) {
			if (errorEl) {
				errorEl.textContent = this.describe(error, "The report could not be sent.");
			}
		}
	}

	// --- Deep link -----------------------------------------------------------

	private highlightDeepLink(scroll: boolean): void {
		const commentId = this.root.dataset.highlightComment;
		const target = commentId ? this.findArticle(commentId) : null;
		if (target) {
			this.highlight(target, scroll);
		}
	}

	private highlight(target: HTMLElement, scroll = true): void {
		for (const el of this.root.querySelectorAll(".comment.is-highlighted")) {
			el.classList.remove("is-highlighted");
		}
		target.classList.add("is-highlighted");
		if (scroll) {
			target.scrollIntoView({ behavior: this.scrollBehavior(), block: "center" });
		}
	}

	// --- Shared --------------------------------------------------------------

	private render(comment: CommentDto, isReply: boolean): HTMLElement {
		this.known.set(comment.id, comment);
		for (const reply of comment.replies) {
			this.known.set(reply.id, reply);
		}
		return renderComment(comment, isReply);
	}

	private findArticle(commentId: string): HTMLElement | null {
		return this.list.querySelector<HTMLElement>(`[data-comment-id="${CSS.escape(commentId)}"]`);
	}

	private scrollBehavior(): ScrollBehavior {
		return window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth";
	}

	private busy(value: boolean): void {
		if (this.submitButton) {
			this.submitButton.disabled = value;
		}
	}

	private setStatus(message: string): void {
		if (this.statusEl) {
			this.statusEl.textContent = message;
		}
	}

	/** Shows a short-lived message right next to the control that failed. */
	private flash(anchor: HTMLElement, message: string): void {
		const container = anchor.closest<HTMLElement>(".comment__actions, .comment__editor-actions") ?? anchor.parentElement;
		if (!container) {
			this.setStatus(message);
			return;
		}

		container.parentElement?.querySelector(":scope > .comment__notice")?.remove();
		const notice = document.createElement("p");
		notice.className = "comment__notice";
		notice.setAttribute("role", "alert");
		notice.textContent = message;
		container.after(notice);
		window.setTimeout(() => notice.remove(), 6000);
	}

	private describe(error: unknown, fallback: string): string {
		if (!(error instanceof ApiError)) {
			return fallback;
		}

		switch (error.status) {
			case 429:
				return error.retryAfterSeconds !== null
					? `Slow down a little. Try again in ${error.retryAfterSeconds} seconds.`
					: "Slow down a little. Try again shortly.";
			case 401:
				return "Your session has expired. Please sign in again.";
			case 403:
				return error.detail ?? "You are not allowed to do that.";
			case 404:
				return error.detail ?? "That comment no longer exists.";
			case 400:
			case 409:
				return error.detail ?? fallback;
			default:
				return fallback;
		}
	}
}
