// Ambient declarations for the Cloudflare Turnstile api.js global. A .d.ts emits nothing
// into the bundle and avoids an @types devDependency.

interface TurnstileRenderOptions {
	sitekey: string;
	action?: string;
	callback?: (token: string) => void;
	"error-callback"?: (errorCode: string) => void;
	"expired-callback"?: () => void;
	"timeout-callback"?: () => void;
	theme?: "light" | "dark" | "auto";
	size?: "normal" | "flexible" | "compact";
	retry?: "auto" | "never";
	"refresh-expired"?: "auto" | "manual" | "never";
	"response-field"?: boolean;
}

declare const turnstile: {
	ready(callback: () => void): void;
	render(container: string | HTMLElement, options: TurnstileRenderOptions): string | undefined;
	getResponse(widgetId?: string): string | undefined;
	isExpired(widgetId?: string): boolean;
	reset(widgetId?: string): void;
	remove(widgetId?: string): void;
};
