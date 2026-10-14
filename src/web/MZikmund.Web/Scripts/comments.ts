// Bundled to wwwroot/js/comments.js as an ES module and loaded only on post pages.
// It carries @supabase/supabase-js, which is why it is a separate entry point from site.js.
import { getSession } from "./Auth/SupabaseAuth";

if (document.querySelector("[data-comments-root]")) {
	void getSession();
}
