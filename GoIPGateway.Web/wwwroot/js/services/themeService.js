/**
 * GoIP Gateway - Theme Service
 * Handles dark / light mode toggle, persistence in localStorage, and UI indicator updates.
 */
(function (window) {
    'use strict';

    const STORAGE_KEY = 'goip_theme';

    const ThemeService = {
        getTheme: function () {
            return localStorage.getItem(STORAGE_KEY) || 
                   (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
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
            const icon = document.getElementById('theme-icon');
            const text = document.getElementById('theme-text');
            if (icon) {
                icon.className = theme === 'dark' 
                    ? 'bi bi-moon-stars-fill text-warning fs-5' 
                    : 'bi bi-sun-fill text-warning fs-5';
            }
            if (text) {
                text.innerText = theme === 'dark' ? 'الوضع الليلي' : 'الوضع النهاري';
            }
        },

        init: function () {
            const currentTheme = document.documentElement.getAttribute('data-bs-theme') || this.getTheme();
            this.setTheme(currentTheme);
        }
    };

    window.ThemeService = ThemeService;
    window.toggleTheme = function () { ThemeService.toggleTheme(); };
})(window);
