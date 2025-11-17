#!/usr/bin/env node

import { build } from 'vite';
import { copyFileSync, existsSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

async function buildAssets() {
	console.log('Building assets with Vite...');
	
	// Build with Vite
	await build({
		configFile: resolve(__dirname, 'vite.config.ts'),
	});
	
	console.log('Build complete!');
	
	// Copy files to create .min versions
	const cssPath = resolve(__dirname, 'wwwroot/css/site.css');
	const cssMinPath = resolve(__dirname, 'wwwroot/css/site.min.css');
	const jsPath = resolve(__dirname, 'wwwroot/js/site.js');
	const jsMinPath = resolve(__dirname, 'wwwroot/js/site.min.js');
	
	if (existsSync(cssPath)) {
		console.log('Creating site.min.css...');
		copyFileSync(cssPath, cssMinPath);
	}
	
	if (existsSync(jsPath)) {
		console.log('Creating site.min.js...');
		copyFileSync(jsPath, jsMinPath);
	}
	
	console.log('All assets built successfully!');
}

buildAssets().catch((err) => {
	console.error('Build failed:', err);
	process.exit(1);
});
