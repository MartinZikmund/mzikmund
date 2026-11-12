import { isAuthApiError } from "@supabase/supabase-js";
import {
	sendEmailCode,
	signInWithApple,
	signInWithGoogle,
	verifyEmailCode,
} from "../Auth/SupabaseAuth";

const ResendSeconds = 60;

function isRateLimited(error: unknown): boolean {
	return isAuthApiError(error)
		&& (error.status === 429
			|| error.code === "over_email_send_rate_limit"
			|| error.code === "over_request_rate_limit");
}

export type SignInResult = "signed-in" | "cancelled" | "redirecting";

/**
 * The site's first modal surface. <dialog>.showModal() supplies the top layer, Escape, focus
 * trapping and a ::backdrop painted with Smoke.
 */
export class SignInDialog {
	private readonly dialog: HTMLDialogElement;
	private readonly emailStep: HTMLElement | null;
	private readonly codeStep: HTMLElement | null;
	private readonly emailInput: HTMLInputElement | null;
	private readonly codeInput: HTMLInputElement | null;
	private readonly sendButton: HTMLButtonElement | null;
	private readonly verifyButton: HTMLButtonElement | null;
	private readonly errorEl: HTMLElement | null;
	private readonly statusEl: HTMLElement | null;
	private readonly resendButton: HTMLButtonElement | null;
	private readonly changeEmailButton: HTMLButtonElement | null;
	private readonly resendLabel: string;
	private resendTimer: number | undefined;
	private resolve: ((result: SignInResult) => void) | null = null;
	private wired = false;
	private pending = false;

	public constructor(dialog: HTMLDialogElement) {
		this.dialog = dialog;
		this.emailStep = dialog.querySelector<HTMLElement>("[data-signin-email-step]");
		this.codeStep = dialog.querySelector<HTMLElement>("[data-signin-code-step]");
		this.emailInput = dialog.querySelector<HTMLInputElement>("[data-signin-email]");
		this.codeInput = dialog.querySelector<HTMLInputElement>("[data-signin-code]");
		this.sendButton = dialog.querySelector<HTMLButtonElement>("[data-signin-send]");
		this.verifyButton = dialog.querySelector<HTMLButtonElement>("[data-signin-verify]");
		this.errorEl = dialog.querySelector<HTMLElement>("[data-signin-error]");
		this.statusEl = dialog.querySelector<HTMLElement>("[data-signin-status]");
		this.resendButton = dialog.querySelector<HTMLButtonElement>("[data-signin-resend]");
		this.changeEmailButton = dialog.querySelector<HTMLButtonElement>("[data-signin-change-email]");
		this.resendLabel = this.resendButton?.textContent?.trim() ?? "Resend code";
	}

	public open(returnTo: string): Promise<SignInResult> {
		this.wire(returnTo);
		this.showError("");
		this.showStatus("");
		this.stopResendCountdown();
		if (this.emailStep) {
			this.emailStep.hidden = false;
		}
		if (this.codeStep) {
			this.codeStep.hidden = true;
		}
		if (this.codeInput) {
			this.codeInput.value = "";
		}

		const result = new Promise<SignInResult>((resolve) => {
			this.resolve = resolve;
		});
		this.dialog.showModal();
		this.emailInput?.focus();
		return result;
	}

	private wire(returnTo: string): void {
		if (this.wired) {
			return;
		}
		this.wired = true;

		// Escape and the method="dialog" close button both fire `close`.
		this.dialog.addEventListener("close", () => {
			this.stopResendCountdown();
			this.settle("cancelled");
		});

		for (const button of this.dialog.querySelectorAll<HTMLButtonElement>("[data-signin-provider]")) {
			button.addEventListener("click", () => {
				const provider = button.dataset.signinProvider;
				this.showError("");
				this.setProvidersDisabled(true);
				// Navigates the tab away; the draft is already in sessionStorage. Only a sign-in that
				// actually started counts as "redirecting"; on failure the dialog stays open.
				const started = provider === "apple" ? signInWithApple(returnTo) : signInWithGoogle(returnTo);
				started.then(
					() => this.settle("redirecting"),
					() => {
						this.setProvidersDisabled(false);
						this.showError("Sign-in could not be started. Please try again.");
					});
			});
		}

		this.sendButton?.addEventListener("click", () => void this.sendCode());
		this.verifyButton?.addEventListener("click", () => void this.verifyCode());

		this.resendButton?.addEventListener("click", () => void this.sendCode());
		this.changeEmailButton?.addEventListener("click", () => this.showEmailStep());

		this.emailInput?.addEventListener("keydown", (e) => {
			if (e.key === "Enter") {
				e.preventDefault();
				void this.sendCode();
			}
		});

		this.codeInput?.addEventListener("keydown", (e) => {
			if (e.key === "Enter") {
				e.preventDefault();
				void this.verifyCode();
			}
		});
	}

	private async sendCode(): Promise<void> {
		if (this.pending) {
			return;
		}

		const email = this.emailInput?.value.trim() ?? "";
		if (!email) {
			this.showError("Enter your email address first.");
			return;
		}

		const resending = this.codeStep?.hidden === false;
		this.busy(true);
		try {
			await sendEmailCode(email);
			this.showError("");
			this.showCodeStep(resending);
		} catch (error) {
			if (isRateLimited(error)) {
				this.showError("Please wait a minute before requesting another code.");
				if (resending) {
					this.startResendCountdown();
				}
			} else if (isAuthApiError(error) && error.status >= 400 && error.status < 500) {
				this.showError("That code could not be sent. Check the address and try again.");
			} else {
				this.showError("The code could not be sent. Check your connection and try again.");
			}
		} finally {
			this.busy(false);
		}
	}

	private showCodeStep(resent: boolean): void {
		if (this.emailStep) {
			this.emailStep.hidden = true;
		}
		if (this.codeStep) {
			this.codeStep.hidden = false;
		}
		this.codeInput?.focus();
		this.startResendCountdown();
		this.showStatus(resent
			? "A new code was sent."
			: `We sent a 6-digit code to ${this.emailInput?.value.trim() ?? "your email"}.`);
	}

	private showEmailStep(): void {
		this.stopResendCountdown();
		this.showError("");
		if (this.codeInput) {
			this.codeInput.value = "";
		}
		if (this.codeStep) {
			this.codeStep.hidden = true;
		}
		if (this.emailStep) {
			this.emailStep.hidden = false;
		}
		this.emailInput?.focus();
		this.showStatus("Enter your email address.");
	}

	private startResendCountdown(): void {
		this.stopResendCountdown();
		const button = this.resendButton;
		if (!button) {
			return;
		}

		let remaining = ResendSeconds;
		const tick = (): void => {
			if (remaining <= 0) {
				this.stopResendCountdown();
				return;
			}
			button.disabled = true;
			button.textContent = `${this.resendLabel} (${remaining}s)`;
			remaining -= 1;
		};
		tick();
		this.resendTimer = window.setInterval(tick, 1000);
	}

	private stopResendCountdown(): void {
		window.clearInterval(this.resendTimer);
		this.resendTimer = undefined;
		if (this.resendButton) {
			this.resendButton.disabled = false;
			this.resendButton.textContent = this.resendLabel;
		}
	}

	private async verifyCode(): Promise<void> {
		if (this.pending) {
			return;
		}

		const email = this.emailInput?.value.trim() ?? "";
		const code = this.codeInput?.value.trim() ?? "";
		if (code.length !== 6) {
			this.showError("Enter the 6-digit code from the email.");
			return;
		}

		this.busy(true);
		try {
			await verifyEmailCode(email, code);
			this.settle("signed-in");
			this.dialog.close();
		} catch (error) {
			if (isRateLimited(error)) {
				this.showError("Too many attempts. Please wait a minute and try again.");
			} else if (isAuthApiError(error) && error.status >= 400 && error.status < 500) {
				this.showError("That code is not valid or has expired. Check it, or request a new one.");
			} else {
				this.showError("The code could not be checked. Check your connection and try again.");
			}
		} finally {
			this.busy(false);
		}
	}

	private setProvidersDisabled(value: boolean): void {
		for (const button of this.dialog.querySelectorAll<HTMLButtonElement>("[data-signin-provider]")) {
			button.disabled = value;
		}
	}

	private settle(result: SignInResult): void {
		const resolve = this.resolve;
		this.resolve = null;
		resolve?.(result);
	}

	private busy(value: boolean): void {
		this.pending = value;
		if (this.sendButton) {
			this.sendButton.disabled = value;
		}
		if (this.verifyButton) {
			this.verifyButton.disabled = value;
		}
	}

	private showStatus(message: string): void {
		if (this.statusEl) {
			this.statusEl.textContent = message;
		}
	}

	private showError(message: string): void {
		if (this.errorEl) {
			this.errorEl.textContent = message;
		}
	}
}
