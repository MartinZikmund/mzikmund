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
import { actionsMarkup, bodyMarkup, renderComment, setEditWindow } from "./CommentRenderer";
import { CommentTurnstile } from "./CommentTurnstile";
import { SignInDialog, type SignInResult } from "./SignInDialog";

const DraftKey = "mz-comment-draft";
const MaxLength = 4000;
/** A draft older than this never posts itself after a sign-in redirect. */
const DraftFreshMs = 10 * 60 * 1000;

interface CommentDraft {
	postId: string;
	parentId: string | null;
	content: string;
	/** True when the reader pressed Post and only sign-in stood in the way. */
	submit: boolean;
	savedAt: number;
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
	private earlierWrap: HTMLElement | null = null;
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
	/** First and last loaded page: a deep link starts mid-list, so pages 1..firstPage-1 are not loaded. */
	private firstPage = 1;
	private page = 1;
	private pageSize = 20;
	private repliesPageSize = 20;
	private total = 0;
	private posting = false;
	private reporting = false;

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

		// The OAuth return is the only time a pending draft may post itself. Read it before the
		// Supabase client runs: it exchanges ?code= and strips it from the address.
		const oauthReturn = new URLSearchParams(window.location.search).has("code");

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
		this.firstPage = this.page;
		this.pageSize = Number.parseInt(root.dataset.pageSize ?? "20", 10) || 20;
		this.repliesPageSize = Number.parseInt(root.dataset.repliesPageSize ?? "20", 10) || 20;
		setEditWindow(Number.parseInt(root.dataset.editWindowMinutes ?? "15", 10) || 15);
		this.total = Number.parseInt(root.dataset.total ?? "0", 10) || 0;

		// Reply, report and the composer need JavaScript (they may have to sign the reader in).
		if (this.composer) {
			this.composer.hidden = false;
		}
		this.revealServerActions();
		this.addEarlierButton();

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
			await this.resumePendingDraft(oauthReturn);
		} else {
			this.restoreDraftText();
		}
	}

	private revealServerActions(): void {
		for (const button of this.list.querySelectorAll<HTMLElement>("[data-comment-reply][hidden], [data-comment-report][hidden]")) {
			button.hidden = false;
		}
	}

	private addEarlierButton(): void {
		if (this.firstPage <= 1) {
			return;
		}

		const wrap = document.createElement("div");
		wrap.className = "comments__more";
		const button = document.createElement("button");
		button.type = "button";
		button.className = "btn btn--standard";
		button.textContent = "Show earlier comments";
		button.addEventListener("click", () => void this.loadEarlier(button));
		wrap.appendChild(button);
		this.list.before(wrap);
		this.earlierWrap = wrap;
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
		await this.dropSession();
		await this.refreshCurrentPage();
	}

	private async dropSession(): Promise<void> {
		try {
			await signOut();
		} catch {
			// Nothing to revoke is fine; the local session is gone either way.
		}
		if (this.identityEl) {
			this.identityEl.hidden = true;
		}
	}

	/**
	 * Runs a write. A 401 means the session is stale or revoked, so the reader signs in again and
	 * the write is retried once instead of ending in a dead-end message.
	 */
	private async authed<T>(operation: () => Promise<T>, beforeRetry?: () => void): Promise<T> {
		try {
			return await operation();
		} catch (error) {
			if (!(error instanceof ApiError) || error.status !== 401 || !this.signIn) {
				throw error;
			}

			await this.dropSession();
			const result = await this.signIn.open(this.returnUrl());
			if (result !== "signed-in") {
				throw error;
			}

			await this.onSignedIn();
			beforeRetry?.();
			return await operation();
		}
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

	/** One post at a time: a double click or Enter-repeat must not create the comment twice. */
	private async guardPost(action: () => Promise<void>): Promise<void> {
		if (this.posting) {
			return;
		}

		this.posting = true;
		this.busy(true);
		try {
			await action();
		} finally {
			this.posting = false;
			this.busy(false);
		}
	}

	private submit(): Promise<void> {
		return this.guardPost(() => this.submitCore());
	}

	private async submitCore(): Promise<void> {
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
		this.setStatus("Posting…");

		try {
			const created = await this.authed(
				async () => createComment(this.postId, content, parentId, await this.captcha.getToken()),
				() => this.captcha.reset());
			this.insertCreated(created);
			this.clearDraft();
			if (this.textarea) {
				this.textarea.value = "";
			}
			this.updateCounter();
			this.setReplyTarget(null, null);
			this.setStatus("Posted.");
		} catch (error) {
			// A failed post must never leave the draft armed to post itself later.
			this.saveDraft(false);
			this.setStatus(this.describe(error, "Your comment could not be posted."));
		} finally {
			// The token is spent either way - Cloudflare rejects a replay.
			this.captcha.reset();
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
			savedAt: Date.now(),
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

	/** A draft marked submit posts itself, but only straight after the OAuth return and only once. */
	private async resumePendingDraft(oauthReturn: boolean): Promise<void> {
		const draft = this.readDraft();
		if (!draft) {
			return;
		}

		// Disarm first, so a failure, a reload or a later visit can never retry it.
		const armed = draft.submit && oauthReturn && Date.now() - (draft.savedAt ?? 0) <= DraftFreshMs;
		if (draft.submit) {
			try {
				window.sessionStorage.setItem(DraftKey, JSON.stringify({ ...draft, submit: false }));
			} catch {
				// See saveDraft.
			}
		}

		if (this.textarea) {
			this.textarea.value = draft.content;
		}
		this.setReplyTarget(draft.parentId, this.authorOf(draft.parentId));
		this.updateCounter();

		if (armed) {
			await this.guardPost(() => this.postDraft(draft.content.trim(), draft.parentId));
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

		// A 401 re-signs the reader in, which swaps the action buttons: paint whichever is live.
		const live = (): [HTMLButtonElement, HTMLElement | null] => {
			const button = this.findArticle(commentId)
				?.querySelector<HTMLButtonElement>(":scope > .comment__actions [data-comment-upvote]") ?? fresh;
			return [button, button.querySelector<HTMLElement>("[data-upvote-count]")];
		};

		try {
			const updated = await this.authed(() => setVote(commentId, wantUpvote));
			const [button, count] = live();
			this.paintVote(button, count, updated.hasUpvoted, updated.upvoteCount);
		} catch (error) {
			const [button, count] = live();
			this.paintVote(button, count, !wantUpvote, previousCount);
			this.flash(button, this.describe(error, "Your vote could not be saved."));
		} finally {
			fresh.disabled = false;
			live()[0].disabled = false;
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

	/**
	 * Re-reads the loaded pages and patches the reader-specific state (own, upvoted) onto what is
	 * already rendered, so loaded pages, expanded replies and open editors survive a sign-in change.
	 */
	private async refreshCurrentPage(): Promise<void> {
		try {
			const signedIn = (await getSession()) !== null;
			const fresh = new Map<string, CommentDto>();
			let total = this.total;

			for (let page = this.firstPage; page <= this.page; page++) {
				const response = await getComments(this.postId, this.sort, page);
				total = response.totalCount;
				for (const comment of response.data) {
					fresh.set(comment.id, comment);
					for (const reply of comment.replies) {
						fresh.set(reply.id, reply);
					}
				}
			}

			await this.fetchLoadedReplies(fresh);

			for (const article of this.list.querySelectorAll<HTMLElement>("[data-comment-id]")) {
				const id = article.dataset.commentId ?? "";
				// Anything the server did not return (a comment posted a moment ago) keeps its state
				// while signed in, and loses the reader's flags on sign-out.
				const known = this.known.get(id);
				const dto = fresh.get(id) ?? (!signedIn && known ? { ...known, isOwn: false, hasUpvoted: false } : undefined);
				if (dto) {
					this.patch(article, dto);
				}
			}

			this.total = total;
			this.syncTotal();
			this.syncLoadMore();
			this.highlightDeepLink(false);
		} catch {
			// Keep what is on screen: still correct, just without the reader's own state.
		}
	}

	/** Replies expanded beyond the inline ones are not part of the page response. */
	private async fetchLoadedReplies(fresh: Map<string, CommentDto>): Promise<void> {
		for (const container of this.list.querySelectorAll<HTMLElement>("[data-comment-replies]")) {
			const parentId = container.closest<HTMLElement>("[data-comment-id]")?.dataset.commentId;
			const rendered = Array.from(container.querySelectorAll<HTMLElement>(":scope > [data-comment-id]"));
			if (!parentId || rendered.every((reply) => fresh.has(reply.dataset.commentId ?? ""))) {
				continue;
			}

			for (let page = 1; ; page++) {
				const response = await getReplies(parentId, page);
				for (const reply of response.data) {
					fresh.set(reply.id, reply);
				}
				if (response.data.length === 0
					|| page * this.repliesPageSize >= response.totalCount
					|| rendered.every((reply) => fresh.has(reply.dataset.commentId ?? ""))) {
					break;
				}
			}
		}
	}

	private patch(article: HTMLElement, comment: CommentDto): void {
		this.known.set(comment.id, comment);
		const actions = article.querySelector<HTMLElement>(":scope > .comment__actions");
		if (actions && comment.status === "Visible") {
			actions.outerHTML = actionsMarkup(comment);
		}
	}

	private async loadEarlier(button: HTMLButtonElement): Promise<void> {
		button.disabled = true;
		try {
			const response = await getComments(this.postId, this.sort, this.firstPage - 1);
			const anchor = this.list.firstElementChild;
			const before = anchor?.getBoundingClientRect().top ?? 0;

			for (const comment of response.data) {
				if (!this.findArticle(comment.id)) {
					this.list.insertBefore(this.render(comment, false), anchor);
				}
			}

			// Keep what the reader was looking at where it was.
			if (anchor) {
				window.scrollBy(0, anchor.getBoundingClientRect().top - before);
			}

			this.firstPage -= 1;
			this.total = response.totalCount;
			this.syncTotal();
			if (this.firstPage <= 1) {
				this.earlierWrap?.remove();
				this.earlierWrap = null;
			}
		} catch (error) {
			this.setStatus(this.describe(error, "Earlier comments could not be loaded."));
		} finally {
			button.disabled = false;
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
			const loaded = this.list.querySelectorAll(":scope > [data-comment-id]").length;
			this.loadMoreButton.hidden = loaded >= this.total || this.page * this.pageSize >= this.total;
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
			this.authed(() => updateComment(known.id, content))
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
			await this.authed(() => deleteComment(commentId));
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
		if (this.reporting || !this.reportTargetId || !this.reportDialog) {
			return;
		}

		this.reporting = true;
		const send = this.reportDialog.querySelector<HTMLButtonElement>("[data-report-send]");
		if (send) {
			send.disabled = true;
		}

		try {
			await this.sendReportCore();
		} finally {
			this.reporting = false;
			if (send) {
				send.disabled = false;
			}
		}
	}

	private async sendReportCore(): Promise<void> {
		if (!this.reportTargetId || !this.reportDialog) {
			return;
		}

		const reason = (this.reportDialog.querySelector<HTMLSelectElement>("[data-report-reason]")?.value ?? "Other") as ReportReason;
		const note = this.reportDialog.querySelector<HTMLTextAreaElement>("[data-report-note]")?.value.trim() ?? "";
		const errorEl = this.reportDialog.querySelector<HTMLElement>("[data-report-error]");

		try {
			const targetId = this.reportTargetId;
			await this.authed(() => reportComment(targetId, reason, note.length > 0 ? note : null));
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
