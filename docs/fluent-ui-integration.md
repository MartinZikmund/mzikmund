# Fluent UI Web Components Integration

## Overview

This document describes the integration of Microsoft Fluent UI Web Components into the MZikmund website.

## What is Fluent UI Web Components?

Fluent UI Web Components are a library of web components based on the Fluent Design System. They provide a modern, accessible, and consistent UI component library that can be used in any web application.

Learn more: https://learn.microsoft.com/en-us/fluent-ui/web-components/

## Integration Details

### 1. Package Installation

The `@fluentui/web-components` package has been added to the project's npm dependencies:

```json
{
  "dependencies": {
    "@fluentui/web-components": "^2.6.1"
  }
}
```

### 2. CDN Integration

Fluent UI Web Components are loaded via CDN in the `_HeadScriptsPartial.cshtml` file:

```html
<script type="module" src="https://cdn.jsdelivr.net/npm/@fluentui/web-components/dist/web-components.min.js"></script>
```

This loads the components as ES modules, making all Fluent UI web components available throughout the application.

### 3. TypeScript Integration

A new TypeScript manager has been created at `Scripts/FluentUIManager.ts` to handle any custom initialization logic:

```typescript
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
```

This manager is initialized in `Scripts/index.ts` alongside other site initialization code.

### 4. Demo Page

A comprehensive demo page has been created at `/FluentUIDemo` that showcases various Fluent UI components including:

- **Buttons**: Different button appearances (accent, neutral, outline, stealth)
- **Form Controls**: Text fields, text areas, checkboxes, radio buttons, switches, sliders
- **Progress**: Progress indicators
- **Select/Combobox**: Dropdown selection controls
- **Badges**: Status and label badges
- **Cards**: Container components for content
- **Accordion**: Expandable/collapsible sections
- **Tabs**: Tabbed content organization
- **Dialog/Modal**: Popup dialogs for user interactions

The demo page is accessible via the navigation menu and provides a live showcase of all integrated components.

## Usage

To use Fluent UI Web Components in your Razor pages or views, simply add the component tags to your HTML:

```html
<!-- Button -->
<fluent-button appearance="accent">Click Me</fluent-button>

<!-- Text Input -->
<fluent-text-field placeholder="Enter text">Label</fluent-text-field>

<!-- Card -->
<fluent-card>
    <h3>Card Title</h3>
    <p>Card content goes here</p>
</fluent-card>

<!-- Checkbox -->
<fluent-checkbox checked>Option</fluent-checkbox>

<!-- Switch -->
<fluent-switch checked>Toggle</fluent-switch>
```

## Available Components

The following components are available through this integration:

- `fluent-button`
- `fluent-text-field`
- `fluent-text-area`
- `fluent-checkbox`
- `fluent-radio` / `fluent-radio-group`
- `fluent-switch`
- `fluent-slider`
- `fluent-progress`
- `fluent-select` / `fluent-option`
- `fluent-badge`
- `fluent-card`
- `fluent-accordion` / `fluent-accordion-item`
- `fluent-tabs` / `fluent-tab` / `fluent-tab-panel`
- `fluent-dialog`

For a complete list and documentation, visit: https://docs.microsoft.com/en-us/fluent-ui/web-components/components/overview

## Browser Support

Fluent UI Web Components use modern web standards and support:
- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)

## Building the Project

The TypeScript files are compiled to JavaScript using the TypeScript compiler:

```bash
cd src/web/MZikmund.Web
npx tsc
```

This generates the `wwwroot/js/site.js` file that includes the Fluent UI initialization code.

## Styling and Theming

Fluent UI Web Components use the Shadow DOM and come with built-in styling. The components automatically adapt to the page's color scheme and can be customized using CSS custom properties.

For theming documentation, see: https://docs.microsoft.com/en-us/fluent-ui/web-components/design-system/design-system

## Performance Considerations

- Components are loaded via CDN, which provides caching benefits
- Components are loaded as ES modules, enabling tree-shaking and code splitting
- Shadow DOM encapsulation prevents style conflicts
- Components are lazy-loaded as they appear on the page

## Future Enhancements

Potential future improvements:

1. Custom theming to match the website's brand colors
2. Additional component integrations as needed
3. Local bundling of components for offline support
4. Integration with existing Bootstrap components for hybrid UI
