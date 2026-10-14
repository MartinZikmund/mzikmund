import {
	createClient,
	type Session,
	type SupabaseClient,
} from "@supabase/supabase-js";

interface SupabaseAuthConfig {
	url: string;
	/** sb_publishable_... - never the secret key. */
	publishableKey: string;
}

let client: SupabaseClient | null = null;

/** Reads the JSON block Razor renders into the page, so no key is baked into the bundle. */
function readConfig(): SupabaseAuthConfig {
	const el = document.getElementById("supabase-config");
	if (!el?.textContent) {
		throw new Error("Missing #supabase-config JSON block.");
	}
	return JSON.parse(el.textContent) as SupabaseAuthConfig;
}

export function getClient(): SupabaseClient {
	if (client) {
		return client;
	}
	const cfg = readConfig();
	client = createClient(cfg.url, cfg.publishableKey, {
		auth: {
			// The library default is 'implicit', which puts the access token in the
			// URL fragment where it leaks via history and Referer.
			flowType: "pkce",
			detectSessionInUrl: true,
			persistSession: true,
			autoRefreshToken: true,
			// Pinned so a library change to the default key shape cannot sign everyone out.
			storage: window.localStorage,
			storageKey: "mz-auth",
		},
	});
	return client;
}

// signInWithOAuth navigates the tab away, so nothing after it runs. returnTo must be on
// the Redirect URLs allow list of the Supabase project.

// The library reports failure in the result instead of rejecting, so it is rethrown here.
async function startOAuth(provider: "google" | "apple", returnTo: string): Promise<void> {
	const { error } = await getClient().auth.signInWithOAuth({
		provider,
		options: { redirectTo: returnTo },
	});
	if (error) {
		throw error;
	}
}

export function signInWithGoogle(returnTo: string): Promise<void> {
	return startOAuth("google", returnTo);
}

export function signInWithApple(returnTo: string): Promise<void> {
	return startOAuth("apple", returnTo);
}

/**
 * Sends the 6-digit code. Code vs. magic link is decided by the Supabase email template:
 * `{{ .Token }}` is a code, `{{ .ConfirmationURL }}` is a link.
 */
export async function sendEmailCode(email: string): Promise<void> {
	const { error } = await getClient().auth.signInWithOtp({
		email,
		options: { shouldCreateUser: true },
	});
	if (error) {
		throw error;
	}
}

/** Type "email" covers both first sign-up and returning sign-in. */
export async function verifyEmailCode(email: string, token: string): Promise<Session> {
	const { data, error } = await getClient().auth.verifyOtp({ email, token, type: "email" });
	if (error || !data.session) {
		throw error ?? new Error("No session returned.");
	}
	return data.session;
}

/**
 * Reads from storage; the background ticker keeps the token fresh. It does not revalidate
 * the JWT - the API validates it against the Supabase JWKS, which is where the real check lives.
 */
export async function getSession(): Promise<Session | null> {
	const { data } = await getClient().auth.getSession();
	return data.session;
}

export async function getAccessToken(): Promise<string | null> {
	return (await getSession())?.access_token ?? null;
}

/** The default scope would sign the reader out on every device. */
export async function signOut(): Promise<void> {
	await getClient().auth.signOut({ scope: "local" });
}

/** Same-origin call to /api/v1/... with the Supabase access token attached. */
export async function apiFetch(input: string, init: RequestInit = {}): Promise<Response> {
	const token = await getAccessToken();
	const headers = new Headers(init.headers);
	if (token) {
		headers.set("Authorization", `Bearer ${token}`);
	}
	if (init.body && !headers.has("Content-Type")) {
		headers.set("Content-Type", "application/json");
	}
	return fetch(input, { ...init, headers, credentials: "same-origin" });
}
