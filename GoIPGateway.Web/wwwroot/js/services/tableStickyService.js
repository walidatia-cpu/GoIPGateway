/**
 * GoIP Gateway - Table Sticky Service
 * Implements Google Standings-style frozen sticky columns and dynamic elevation shadow.
 * Attaches automatically to all .table-responsive and .table-responsive-custom wrappers.
 */
(function (window) {
    'use strict';

    const TableStickyService = {
        init: function () {
            this.attachScrollShadows();
            this.setupMutationObserver();
        },

        attachScrollShadows: function (container) {
            const root = container || document;
            const wrappers = root.querySelectorAll('.table-responsive, .table-responsive-custom');

            wrappers.forEach(function (wrapper) {
                if (wrapper._scrollShadowAttached) {
                    if (typeof wrapper._updateScrollState === 'function') wrapper._updateScrollState();
                    return;
                }
                wrapper._scrollShadowAttached = true;

                const table = wrapper.querySelector('table');

                function updateScrollState() {
                    // Check if content actually overflows horizontally
                    if (wrapper.scrollWidth <= wrapper.clientWidth + 2) {
                        wrapper.classList.remove('table-scrolled');
                        if (table) table.classList.remove('table-scrolled');
                        return;
                    }

                    // In RTL, scrollLeft can be negative or positive depending on browser implementation
                    const hasScrolled = Math.abs(wrapper.scrollLeft) > 3;
                    wrapper.classList.toggle('table-scrolled', hasScrolled);
                    if (table) table.classList.toggle('table-scrolled', hasScrolled);
                }

                wrapper._updateScrollState = updateScrollState;
                wrapper.addEventListener('scroll', updateScrollState, { passive: true });
                window.addEventListener('resize', updateScrollState, { passive: true });
                updateScrollState();
            });
        },

        setupMutationObserver: function () {
            if (!window.MutationObserver) return;
            const self = this;
            const observer = new MutationObserver(function () {
                self.attachScrollShadows();
            });
            observer.observe(document.body, { childList: true, subtree: true });
        }
    };

    window.TableStickyService = TableStickyService;
    window.initTableScrollShadows = function (container) { TableStickyService.attachScrollShadows(container); };
})(window);
