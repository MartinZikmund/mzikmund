"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ThemeSwitchManager = void 0;
var RequestedTheme;
(function (RequestedTheme) {
    RequestedTheme["Dark"] = "dark";
    RequestedTheme["Light"] = "light";
    RequestedTheme["Auto"] = "auto";
})(RequestedTheme || (RequestedTheme = {}));
var DisplayTheme;
(function (DisplayTheme) {
    DisplayTheme["Dark"] = "dark";
    DisplayTheme["Light"] = "light";
})(DisplayTheme || (DisplayTheme = {}));
var ThemeSwitchManager = /** @class */ (function () {
    function ThemeSwitchManager() {
    }
    ThemeSwitchManager.init = function () {
        $(function () {
            ThemeSwitchManager.initTheme();
        });
        //ensure theme is set immediately to avoid flash of unstyled content
        ThemeSwitchManager.updateTheme(false);
    };
    ThemeSwitchManager.determineOsTheme = function () {
        if (window.matchMedia &&
            window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return DisplayTheme.Dark;
        }
        else {
            return DisplayTheme.Light;
        }
    };
    ThemeSwitchManager.updateTheme = function (loaded) {
        var requestedTheme = localStorage.getItem('ui-theme');
        if (requestedTheme == null) {
            requestedTheme = RequestedTheme.Auto;
        }
        if (loaded) {
            //clear theme selection
            $("#theme-dropdown a.dropdown-item").removeClass("selected");
            $("#theme-dropdown a[data-value='" + requestedTheme.toString().toLowerCase() + "'").addClass("selected");
        }
        var displayTheme;
        if (requestedTheme == RequestedTheme.Auto) {
            displayTheme = ThemeSwitchManager.determineOsTheme();
        }
        else {
            displayTheme = requestedTheme;
        }
        document.documentElement.removeAttribute("data-theme");
        document.documentElement.setAttribute("data-theme", displayTheme.toString().toLowerCase());
    };
    ThemeSwitchManager.initTheme = function () {
        $("#theme-dropdown a.dropdown-item").click(function (e) {
            e.preventDefault();
            localStorage.setItem('ui-theme', $(this).data('value'));
            ThemeSwitchManager.updateTheme(true);
        });
        if (window.matchMedia) {
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener("change", function () {
                ThemeSwitchManager.updateTheme(true);
            });
        }
        ThemeSwitchManager.updateTheme(true);
    };
    return ThemeSwitchManager;
}());
exports.ThemeSwitchManager = ThemeSwitchManager;
//# sourceMappingURL=ThemeSwitchManager.js.map