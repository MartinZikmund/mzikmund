import {
	sendEmailCode,
	signInWithApple,
	signInWithGoogle,
	verifyEmailCode,
} from "../Auth/SupabaseAuth";

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
	private resolve: ((result: SignInResult) => void) | null = null;
	private wired = false;

	public constructor(dialog: HTMLDialogElement) {
		this.dialog = dialog;
		this.emailStep = dialog.querySelector<HTMLElement>("[data-signin-email-step]");
		this.codeStep = dialog.querySelector<HTMLElement>("[data-signin-code-step]");
		this.emailInput = dialog.querySelector<HTMLInputElement>("[data-signin-email]");
		this.codeInput = dialog.querySelector<HTMLInputElement>("[data-signin-code]");
		this.sendButton = dialog.querySelector<HTMLButtonElement>("[data-signin-send]");
		this.verifyButton = dialog.querySelector<HTMLButtonElement>("[data-signin-verify]");
		this.errorEl = dialog.querySelector<HTMLElement>("[data-signin-error]");
	}

	public open(returnTo: string): Promise<SignInResult> {
		this.wire(returnTo);
		this.showError("");
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
		this.dialog.addEventListener("close", () => this.settle("cancelled"));

		for (const button of this.dialog.querySelectorAll<HTMLButtonElement>("[data-signin-provider]")) {
			button.addEventListener("click", () => {
				const provider = button.dataset.signinProvider;
				this.showError("");
				// Navigates the tab away; the draft is already in sessionStorage.
				const started = provider === "apple" ? signInWithApple(returnTo) : signInWithGoogle(returnTo);
				void started.catch(() => this.showError("Sign-in could not be started. Please try again."));
				this.settle("redirecting");
			});
		}

		this.sendButton?.addEventListener("click", () => void this.sendCode());
		this.verifyButton?.addEventListener("click", () => void this.verifyCode());

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
		const email = this.emailInput?.value.trim() ?? "";
		if (!email) {
			this.showError("Enter your email address first.");
			return;
		}

		this.busy(true);
		try {
			await sendEmailCode(email);
			if (this.emailStep) {
				this.emailStep.hidden = true;
			}
			if (this.codeStep) {
				this.codeStep.hidden = false;
			}
			this.codeInput?.focus();
			this.showError("");
		} catch {
			this.showError("That code could not be sent. Check the address and try again.");
		} finally {
			this.busy(false);
		}
	}

	private async verifyCode(): Promise<void> {
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
		} catch {
			this.showError("That code is not valid. Request a new one if it has expired.");
		} finally {
			this.busy(false);
		}
	}

	private settle(result: SignInResult): void {
		const resolve = this.resolve;
		this.resolve = null;
		resolve?.(result);
	}

	private busy(value: boolean): void {
		if (this.sendButton) {
			this.sendButton.disabled = value;
		}
		if (this.verifyButton) {
			this.verifyButton.disabled = value;
		}
	}

	private showError(message: string): void {
		if (this.errorEl) {
			this.errorEl.textContent = message;
		}
	}
}
