/**
 * GoIP Gateway - Theme Service
 * Handles dark / light mode toggle, persistence in localStorage, and UI indicator updates.
 * Default Mode: Light (الوضع الفاتح)
 */
(function (window) {
    'use strict';

    const STORAGE_KEY = 'goip_theme';

    const ThemeService = {
        getTheme: function () {
            // Default theme is explicitly 'light' unless user saved another preference
            return localStorage.getItem(STORAGE_KEY) || 'light';
        },

        setTheme: function (theme) {
            const validTheme = theme === 'dark' ? 'dark' : 'light';
            document.documentElement.setAttribute('data-bs-theme', validTheme);
            localStorage.setItem(STORAGE_KEY, validTheme);
            this.updateThemeUI(validTheme);
            window.dispatchEvent(new CustomEvent('goip:themeChanged', { detail: { theme: validTheme } }));
        },

        toggleTheme: function () {
            const current = document.documentElement.getAttribute('data-bs-theme') || 'light';
            const next = current === 'dark' ? 'light' : 'dark';
            this.setTheme(next);
        },

        updateThemeUI: function (theme) {
            const icon = document.getElementById('themeIcon') || document.getElementById('theme-icon');
            const text = document.getElementById('themeLabel') || document.getElementById('theme-text');
            if (icon) {
                icon.className = theme === 'dark' 
                    ? 'bi bi-moon-stars-fill text-warning' 
                    : 'bi bi-sun-fill text-warning';
            }
            if (text) {
                text.innerText = theme === 'dark' ? 'الوضع الليلي' : 'الوضع الفاتح';
            }
        },

        init: function () {
            const currentTheme = this.getTheme();
            this.setTheme(currentTheme);
        }
    };

    window.ThemeService = ThemeService;
    window.toggleTheme = function () { ThemeService.toggleTheme(); };
})(window);
