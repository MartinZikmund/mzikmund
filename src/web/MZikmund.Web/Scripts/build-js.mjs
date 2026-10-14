// Bundles Scripts/*.ts into wwwroot/js with esbuild.
//
//   theme-init.js  IIFE, render-blocking classic <script> in <head>
//   site.js        ESM, <script type="module">, every page
//   comments.js    ESM, <script type="module">, post pages only
//
// Two passes because the formats differ: a module script is ALWAYS deferred, so
// the theme applier - which must run before first paint - cannot be one.
//
// esbuild does NOT type-check. `npm run typecheck` (tsc --noEmit) is the gate,
// and it runs before this script in `npm run build`.
//
// Filenames are fixed and unhashed on purpose: asp-append-version="true" already
// cache-busts via query string, and MSBuild globs wwwroot at evaluation time (see
// the note in .gitignore), so a name that changes between builds would go missing
// from the publish output. Do not enable `splitting` - it emits hashed chunks.

import { build, context } from 'esbuild';
import { rmSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..');
const OUT = join(ROOT, 'wwwroot', 'js');
const prod = process.argv.includes('--prod');
const watch = process.argv.includes('--watch');

// esbuild never removes stale output, and wwwroot/js holds nothing but generated
// files (it is gitignored in full), so wipe it rather than leak renamed entries
// into the publish glob.
rmSync(OUT, { recursive: true, force: true });

const shared = {
	absWorkingDir: ROOT,
	bundle: true,
	target: ['es2020'],
	charset: 'utf8',
	legalComments: 'none',
	minify: prod,
	sourcemap: prod ? false : 'linked',
	logLevel: 'info',
};

const configs = [
	{
		...shared,
		entryPoints: [join(ROOT, 'Scripts', 'theme-init.ts')],
		outfile: join(OUT, 'theme-init.js'),
		format: 'iife',
	},
	{
		...shared,
		entryPoints: [
			join(ROOT, 'Scripts', 'site.ts'),
			join(ROOT, 'Scripts', 'comments.ts'),
		],
		outdir: OUT,
		format: 'esm',
		splitting: false,
	},
];

if (watch) {
	for (const config of configs) {
		const ctx = await context(config);
		await ctx.watch();
	}
	console.log('esbuild: watching Scripts/**');
} else {
	await Promise.all(configs.map(build));
}
