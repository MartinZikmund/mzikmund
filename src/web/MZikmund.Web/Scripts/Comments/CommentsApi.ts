import { apiFetch } from "../Auth/SupabaseAuth";

/** Mirrors MZikmund.DataContracts.Blog.Comments.CommentDto (camelCase, string enums). */
export interface CommentDto {
	id: string;
	postId: string;
	parentId: string | null;
	authorDisplayName: string;
	authorAvatarUrl: string | null;
	authorIsAdmin: boolean;
	mentionedDisplayName: string | null;
	content: string;
	contentHtml: string;
	status: "Visible" | "DeletedByAuthor" | "HiddenByModerator";
	createdOn: string;
	editedOn: string | null;
	upvoteCount: number;
	replyCount: number;
	hasUpvoted: boolean;
	isOwn: boolean;
	replies: CommentDto[];
}

export interface PagedResponse<T> {
	data: T[];
	pageNumber: number;
	pageSize: number;
	totalCount: number;
}

export type CommentSort = "top" | "new";

export type ReportReason = "Spam" | "Abuse" | "OffTopic" | "Other";

export class ApiError extends Error {
	public readonly status: number;
	public readonly retryAfterSeconds: number | null;
	/** The curated ProblemDetails message, safe to show to the reader. */
	public readonly detail: string | null;

	public constructor(status: number, retryAfterSeconds: number | null, detail: string | null) {
		super(detail ?? `Request failed with status ${status}.`);
		this.name = "ApiError";
		this.status = status;
		this.retryAfterSeconds = retryAfterSeconds;
		this.detail = detail;
	}
}

async function toApiError(response: Response): Promise<ApiError> {
	const header = response.headers.get("Retry-After");
	const retryAfter = header === null ? Number.NaN : Number.parseInt(header, 10);

	let detail: string | null = null;
	try {
		// ProblemDetails carries `detail`; the captcha rejection carries `error`.
		const body = (await response.json()) as { detail?: unknown; error?: unknown };
		const text = body.detail ?? body.error;
		detail = typeof text === "string" && text.length > 0 ? text : null;
	} catch {
		// No body (401, 429) or not JSON.
	}

	return new ApiError(response.status, Number.isFinite(retryAfter) ? retryAfter : null, detail);
}

async function readJson<T>(response: Response): Promise<T> {
	if (!response.ok) {
		throw await toApiError(response);
	}
	return (await response.json()) as T;
}

async function expectOk(response: Response): Promise<void> {
	if (!response.ok) {
		throw await toApiError(response);
	}
}

export async function getComments(
	postId: string,
	sort: CommentSort,
	pageNumber: number
): Promise<PagedResponse<CommentDto>> {
	const response = await apiFetch(`/api/v1/posts/${postId}/comments?sort=${sort}&pageNumber=${pageNumber}`);
	return readJson<PagedResponse<CommentDto>>(response);
}

export async function getReplies(commentId: string, pageNumber: number): Promise<PagedResponse<CommentDto>> {
	const response = await apiFetch(`/api/v1/comments/${commentId}/replies?pageNumber=${pageNumber}`);
	return readJson<PagedResponse<CommentDto>>(response);
}

/**
 * The Turnstile token travels in the CF-Turnstile-Response header, never in the body: the
 * DTO is shared with the native app, which has no captcha. If the header is present it must
 * verify; its absence is permitted.
 */
export async function createComment(
	postId: string,
	content: string,
	parentId: string | null,
	turnstileToken: string | null
): Promise<CommentDto> {
	const headers: Record<string, string> = {};
	if (turnstileToken) {
		headers["CF-Turnstile-Response"] = turnstileToken;
	}

	const response = await apiFetch(`/api/v1/posts/${postId}/comments`, {
		method: "POST",
		headers,
		body: JSON.stringify({ content, parentId }),
	});
	return readJson<CommentDto>(response);
}

export async function updateComment(commentId: string, content: string): Promise<CommentDto> {
	const response = await apiFetch(`/api/v1/comments/${commentId}`, {
		method: "PATCH",
		body: JSON.stringify({ content }),
	});
	return readJson<CommentDto>(response);
}

export async function deleteComment(commentId: string): Promise<void> {
	await expectOk(await apiFetch(`/api/v1/comments/${commentId}`, { method: "DELETE" }));
}

export async function setVote(commentId: string, upvoted: boolean): Promise<CommentDto> {
	const response = await apiFetch(`/api/v1/comments/${commentId}/vote`, {
		method: upvoted ? "PUT" : "DELETE",
	});
	return readJson<CommentDto>(response);
}

export async function reportComment(
	commentId: string,
	reason: ReportReason,
	note: string | null
): Promise<void> {
	const response = await apiFetch(`/api/v1/comments/${commentId}/report`, {
		method: "POST",
		body: JSON.stringify({ reason, note }),
	});
	await expectOk(response);
}
