const ScriptUrl = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";

/**
 * Owns the single Turnstile widget in the comment composer.
 *
 * A token is single-use and lives 300 seconds, so it is reset after EVERY submit, not only
 * after a failure - otherwise the second comment of a page session replays a spent token and
 * siteverify answers `timeout-or-duplicate`.
 */
export class CommentTurnstile {
	private widgetId: string | undefined;
	private token: string | null = null;
	private waiters: ((token: string | null) => void)[] = [];

	public render(container: HTMLElement, siteKey: string): void {
		if (!siteKey) {
			return;
		}

		const draw = (): void => {
			this.widgetId = turnstile.render(container, {
				sitekey: siteKey,
				action: "comment-create",
				theme: document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light",
				size: "flexible",
				"refresh-expired": "auto",
				// The token rides in the CF-Turnstile-Response header; no form post, no hidden input.
				"response-field": false,
				callback: (token: string) => this.settle(token),
				"error-callback": () => this.settle(null),
				"expired-callback": () => {
					this.token = null;
				},
				"timeout-callback": () => this.settle(null),
			});
		};

		// Loaded from here rather than from a script tag with ?onload=, so there is no global
		// callback to define before an async script can call it.
		const script = document.createElement("script");
		script.src = ScriptUrl;
		script.async = true;
		script.addEventListener("load", () => turnstile.ready(draw));
		document.head.appendChild(script);
	}

	/**
	 * Resolves with the current token, waiting for the challenge to solve itself if needed.
	 * Resolves with null on timeout or error: the server permits an absent token (that is what
	 * keeps the native app able to post), so a stuck widget must never block the reader.
	 */
	public getToken(timeoutMs = 8000): Promise<string | null> {
		if (this.widgetId === undefined) {
			return Promise.resolve(null);
		}

		if (this.token && !turnstile.isExpired(this.widgetId)) {
			return Promise.resolve(this.token);
		}

		const existing = turnstile.getResponse(this.widgetId);
		if (existing) {
			this.token = existing;
			return Promise.resolve(existing);
		}

		return new Promise<string | null>((resolve) => {
			const timer = window.setTimeout(() => {
				this.waiters = this.waiters.filter((w) => w !== waiter);
				resolve(null);
			}, timeoutMs);

			const waiter = (token: string | null): void => {
				window.clearTimeout(timer);
				resolve(token);
			};

			this.waiters.push(waiter);
		});
	}

	public reset(): void {
		this.token = null;
		if (this.widgetId !== undefined) {
			turnstile.reset(this.widgetId);
		}
	}

	private settle(token: string | null): void {
		this.token = token;
		const waiters = this.waiters;
		this.waiters = [];
		for (const waiter of waiters) {
			waiter(token);
		}
	}
}
