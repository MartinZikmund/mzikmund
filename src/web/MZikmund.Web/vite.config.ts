import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
	// Root directory for source files
	root: '.',
	
	// Base public path
	base: '/',
	
	// Build configuration
	build: {
		// Output directory
		outDir: 'wwwroot',
		
		// Empty output directory before build
		emptyOutDir: false,
		
		// Generate sourcemaps for debugging
		sourcemap: true,
		
		// Minification - always minify for optimal production size
		minify: 'terser',
		
		// Rollup options
		rollupOptions: {
			input: {
				// Entry point - includes both TypeScript and SCSS
				site: resolve(__dirname, 'Scripts/index.ts')
			},
			output: {
				// Output structure - generate both regular and .min versions
				entryFileNames: 'js/[name].js',
				chunkFileNames: 'js/[name]-[hash].js',
				assetFileNames: (assetInfo) => {
					// CSS files go to css folder
					if (assetInfo.name?.endsWith('.css')) {
						return 'css/site.css';
					}
					// Everything else to assets
					return 'assets/[name]-[hash][extname]';
				},
			},
		},
		
		// CSS configuration
		cssCodeSplit: false,
		
		// Terser options for better minification
		terserOptions: {
			compress: {
				drop_console: false, // Keep console for debugging
			},
		},
	},
	
	// CSS preprocessing
	css: {
		preprocessorOptions: {
			scss: {
				// Additional SCSS options if needed
			}
		}
	},
	
	// Development server configuration
	server: {
		// Port for Vite dev server
		port: 5173,
		
		// Enable CORS
		cors: true,
		
		// HMR (Hot Module Replacement)
		hmr: {
			// Use default WebSocket settings
			protocol: 'ws',
			host: 'localhost',
		},
		
		// Watch for changes
		watch: {
			usePolling: true,
		},
	},
	
	// Preview server configuration  
	preview: {
		port: 5173,
	},
});
