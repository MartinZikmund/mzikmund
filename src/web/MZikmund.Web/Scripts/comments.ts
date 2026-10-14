// Bundled to wwwroot/js/comments.js as an ES module and loaded only on post pages.
// It carries @supabase/supabase-js, which is why it is a separate entry point from site.js.
import { CommentsSection } from "./Comments/CommentsSection";

new CommentsSection().init();
