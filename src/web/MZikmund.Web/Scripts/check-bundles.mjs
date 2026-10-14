// Guards the three bundles esbuild is supposed to produce.
//
// theme-init.js must stay a classic IIFE: a module script is ALWAYS deferred,
// and a deferred theme applier means every dark-mode visitor gets a flash of
// white before first paint.
//
// site.js must stay free of top-level import/export too, because the Uno app's
// Templates/PostPreviewTemplate.html loads it as a plain <script> - a stray
// `export` there is a SyntaxError that kills the whole file. Adding
// "type": "commonjs" to package.json is what makes esbuild emit one.

import { readFileSync, statSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const OUT = join(dirname(fileURLToPath(import.meta.url)), '..', 'wwwroot', 'js');

// Matches a real ESM statement, not the word "export" inside an identifier or a
// property access such as `module.exports`.
const TOP_LEVEL_ESM = /(?:^|[^.\w$])(?:import|export)\s*(?:default|[{*]|var |let |const |function |class )/;

const failures = [];

function read(name) {
	let size;
	try {
		size = statSync(join(OUT, name)).size;
	} catch {
		failures.push(`${name}: missing from wwwroot/js`);
		return null;
	}
	if (size === 0) {
		failures.push(`${name}: file is empty`);
		return null;
	}
	return readFileSync(join(OUT, name), 'utf8');
}

const themeInit = read('theme-init.js');
const site = read('site.js');
read('comments.js');

if (themeInit !== null) {
	if (TOP_LEVEL_ESM.test(themeInit)) {
		failures.push('theme-init.js: has a top-level import/export, so it is not a classic script');
	}
	if (!themeInit.includes('data-theme')) {
		failures.push('theme-init.js: never writes data-theme, so the theme applier is not in the bundle');
	}
}

if (site !== null && TOP_LEVEL_ESM.test(site)) {
	failures.push('site.js: has a top-level import/export, which breaks PostPreviewTemplate.html');
}

if (failures.length > 0) {
	console.error('check:js FAILED');
	for (const failure of failures) {
		console.error(`  - ${failure}`);
	}
	process.exit(1);
}

console.log('check:js OK - theme-init.js (iife), site.js, comments.js');
