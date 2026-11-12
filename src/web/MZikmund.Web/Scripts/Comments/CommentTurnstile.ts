const ScriptUrl = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";

export const VerificationFailedMessage =
	"Verification couldn't load. If you use an ad or script blocker, allow challenges.cloudflare.com and try again.";

/** Thrown instead of sending a comment the server would reject for lacking a token. */
export class VerificationUnavailableError extends Error {
	public constructor() {
		super(VerificationFailedMessage);
		this.name = "VerificationUnavailableError";
	}
}

/**
 * Owns the single Turnstile widget in the comment composer.
 *
 * A token is single-use and lives 300 seconds, so it is reset after EVERY submit, not only
 * after a failure - otherwise the second comment of a page session replays a spent token and
 * siteverify answers `timeout-or-duplicate`.
 */
export class CommentTurnstile {
	private widgetId: string | undefined;
	private container: HTMLElement | null = null;
	private siteKey = "";
	private scriptLoaded = false;
	private scriptFailed = false;
	private renderedTheme: "light" | "dark" | null = null;
	private token: string | null = null;
	private waiters: ((token: string | null) => void)[] = [];

	public render(container: HTMLElement, siteKey: string): void {
		if (!siteKey) {
			return;
		}

		this.container = container;
		this.siteKey = siteKey;

		// Turnstile fixes its theme at render time and "auto" follows the OS, not the site's
		// switch, so the widget is redrawn whenever data-theme changes.
		new MutationObserver(() => {
			if (this.scriptLoaded && this.renderedTheme !== this.currentTheme()) {
				this.redraw();
			}
		}).observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme"] });

		const draw = (): void => {
			this.scriptLoaded = true;
			this.draw();
		};

		// Loaded from here rather than from a script tag with ?onload=, so there is no global
		// callback to define before an async script can call it.
		const script = document.createElement("script");
		script.src = ScriptUrl;
		// Not async/defer: Cloudflare only allows turnstile.ready() on a synchronously loaded tag,
		// and an explicit render() after `load` needs no ready() at all.
		script.addEventListener("load", draw);
		// A blocker or an offline reader: fail now so the message appears promptly, not after the timeout.
		script.addEventListener("error", () => {
			this.scriptFailed = true;
			this.settle(null);
		});
		document.head.appendChild(script);
	}

	private currentTheme(): "light" | "dark" {
		return document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
	}

	private draw(): void {
		if (!this.container) {
			return;
		}

		this.renderedTheme = this.currentTheme();
		this.widgetId = turnstile.render(this.container, {
			sitekey: this.siteKey,
			action: "comment-create",
			theme: this.renderedTheme,
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
	}

	private redraw(): void {
		if (this.widgetId !== undefined) {
			turnstile.remove(this.widgetId);
		}
		this.widgetId = undefined;
		this.token = null;
		this.draw();
	}

	/**
	 * Resolves with the current token, waiting for the challenge to solve itself if needed.
	 * Resolves with null when none can be had (script blocked, widget error, or no solve within
	 * the timeout). The server rejects a web comment without a token, so the caller must not send.
	 */
	public getToken(timeoutMs = 8000): Promise<string | null> {
		if (this.scriptFailed || !this.container) {
			return Promise.resolve(null);
		}

		if (this.widgetId !== undefined) {
			if (this.token && !turnstile.isExpired(this.widgetId)) {
				return Promise.resolve(this.token);
			}

			const existing = turnstile.getResponse(this.widgetId);
			if (existing) {
				this.token = existing;
				return Promise.resolve(existing);
			}
		}

		// Also covers a script that is still loading: draw() settles the waiters once it solves.
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
