namespace MZikmund.FluentUI {
	export class FluentUIManager {

		public static init() {
			document.addEventListener("DOMContentLoaded", () => {
				FluentUIManager.initFluentUI();
			});
		}

		private static initFluentUI() {
			// Fluent UI Web Components will be loaded from CDN
			// This function can be used for any custom initialization if needed
			console.log('Fluent UI Web Components initialized');
		}
	}
}
