// Bundled to wwwroot/js/theme-init.js as an IIFE and loaded as a BLOCKING
// classic script in <head>. Its only job is to stamp data-theme on <html>
// before the first paint, so keep it free of any other import.
import { ThemeSwitchManager } from "./ThemeSwitchManager";

ThemeSwitchManager.applyStoredTheme();
