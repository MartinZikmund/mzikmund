import type { CommentDto } from "./CommentsApi";

/**
 * Builds the same DOM shape _Comments.cshtml renders server-side. The two must stay
 * identical: page 1 comes from Razor and everything after it from here.
 */

function escapeHtml(value: string): string {
	return value
		.replace(/&/g, "&amp;")
		.replace(/</g, "&lt;")
		.replace(/>/g, "&gt;")
		.replace(/"/g, "&quot;");
}

const Relative = new Intl.RelativeTimeFormat(undefined, { numeric: "auto" });

const Minute = 60 * 1000;
const Hour = 60 * Minute;
const Day = 24 * Hour;

const Units: [Intl.RelativeTimeFormatUnit, number][] = [
	["month", 30 * Day],
	["day", Day],
	["hour", Hour],
	["minute", Minute],
];

/** Matches the Razor rule: relative inside 30 days, absolute beyond it. */
function formatTimestamp(iso: string): string {
	const date = new Date(iso);
	const elapsed = date.getTime() - Date.now();
	const absolute = Math.abs(elapsed);

	if (absolute > 30 * Day) {
		return date.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
	}

	for (const [unit, ms] of Units) {
		if (absolute >= ms) {
			return Relative.format(Math.round(elapsed / ms), unit);
		}
	}

	return Relative.format(Math.round(elapsed / 1000), "second");
}

const FallbackAvatar =
	'<svg class="icon comment__avatar comment__avatar--fallback" aria-hidden="true"><use href="#i-person-circle"></use></svg>';

function identityMarkup(comment: CommentDto): string {
	const time = `<time class="comment__time" datetime="${escapeHtml(comment.createdOn)}">${escapeHtml(formatTimestamp(comment.createdOn))}</time>`;

	if (comment.status !== "Visible") {
		return `${FallbackAvatar}${time}`;
	}

	const avatar = comment.authorAvatarUrl
		? `<img class="comment__avatar" src="${escapeHtml(comment.authorAvatarUrl)}" alt="" width="32" height="32" loading="lazy" referrerpolicy="no-referrer" />`
		: FallbackAvatar;
	const badge = comment.authorIsAdmin ? '<span class="comment__badge">Author</span>' : "";
	const edited = comment.editedOn ? '<span class="comment__edited">edited</span>' : "";

	return `${avatar}<span class="comment__author">${escapeHtml(comment.authorDisplayName)}</span>${badge}${time}${edited}`;
}

/** Inner HTML of `.comment__body`. contentHtml is sanitized server-side on write; the client never sanitizes. */
export function bodyMarkup(comment: CommentDto): string {
	const mention = comment.mentionedDisplayName
		? `<span class="comment__mention">@${escapeHtml(comment.mentionedDisplayName)}</span>`
		: "";
	return `${mention}${comment.contentHtml}`;
}

let editWindowMs = 15 * Minute;

/** The server decides the window; the page hands it over so Edit is hidden once it has passed. */
export function setEditWindow(minutes: number): void {
	editWindowMs = minutes * Minute;
}

function canEdit(comment: CommentDto): boolean {
	return comment.isOwn && Date.now() - new Date(comment.createdOn).getTime() <= editWindowMs;
}

export function actionsMarkup(comment: CommentDto): string {
	const pressed = comment.hasUpvoted ? "true" : "false";
	const icon = comment.hasUpvoted ? "#i-hand-thumbs-up-fill" : "#i-hand-thumbs-up";
	const button = (attribute: string, symbol: string, label: string): string =>
		`<button type="button" class="comment__action" ${attribute}><svg class="icon" aria-hidden="true"><use href="#i-${symbol}"></use></svg>${label}</button>`;

	const own = comment.isOwn
		? (canEdit(comment) ? button("data-comment-edit", "pencil", "Edit") : "") + button("data-comment-delete", "trash3", "Delete")
		: button("data-comment-report", "flag", "Report");

	return `<div class="comment__actions">
		<button type="button" class="comment__action" data-comment-upvote aria-pressed="${pressed}" aria-label="Upvote this comment">
			<svg class="icon" aria-hidden="true"><use href="${icon}"></use></svg>
			<span data-upvote-count>${comment.upvoteCount}</span>
		</button>
		${button("data-comment-reply", "reply", "Reply")}${own}
	</div>`;
}

function repliesMarkup(comment: CommentDto): string {
	const rendered = comment.replies.map((reply) => commentHtml(reply, true)).join("");
	const remaining = comment.replyCount - comment.replies.length;
	const more = remaining > 0
		? `<button type="button" class="btn btn--subtle btn--sm comment__more-replies" data-load-replies data-reply-page="1">Show more replies (${remaining})</button>`
		: "";

	return `<div class="comment__replies" data-comment-replies>${rendered}${more}</div>`;
}

function commentHtml(comment: CommentDto, isReply: boolean): string {
	const visible = comment.status === "Visible";
	const body = visible
		? `<div class="comment__body">${bodyMarkup(comment)}</div>${actionsMarkup(comment)}`
		: `<div class="comment__body comment__body--tombstone"><p>[${comment.status === "DeletedByAuthor" ? "deleted" : "removed"}]</p></div>`;

	return `<article class="comment ${isReply ? "comment--reply" : ""} ${visible ? "" : "comment--tombstone"}"
		id="comment-${escapeHtml(comment.id)}"
		data-comment-id="${escapeHtml(comment.id)}"
		data-parent-id="${comment.parentId ? escapeHtml(comment.parentId) : ""}">
		<div class="comment__identity">${identityMarkup(comment)}</div>
		${body}
		${isReply ? "" : repliesMarkup(comment)}
	</article>`;
}

export function renderComment(comment: CommentDto, isReply: boolean): HTMLElement {
	const host = document.createElement("div");
	host.innerHTML = commentHtml(comment, isReply).trim();
	return host.firstElementChild as HTMLElement;
}
