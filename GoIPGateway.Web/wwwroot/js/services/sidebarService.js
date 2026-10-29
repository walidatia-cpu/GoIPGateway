/**
 * GoIP Gateway - Sidebar Service
 * Handles collapsible sidebar state, persistence, responsive breakpoints, and mobile drawer.
 */
(function (window) {
    'use strict';

    const STORAGE_KEY = 'goip_sidebar_collapsed';

    const SidebarService = {
        toggleDesktop: function () {
            const sidebar = document.getElementById('desktopSidebar');
            if (!sidebar) return;
            const isCollapsed = sidebar.classList.toggle('sidebar-collapsed');
            localStorage.setItem(STORAGE_KEY, isCollapsed ? 'true' : 'false');
            this.updateToggleIcon(isCollapsed);
            window.dispatchEvent(new CustomEvent('goip:sidebarToggled', { detail: { isCollapsed: isCollapsed } }));
        },

        updateToggleIcon: function (isCollapsed) {
            const btn = document.getElementById('sidebarToggleBtn');
            if (!btn) return;
            const icon = btn.querySelector('i');
            if (!icon) return;
            const isRTL = document.documentElement.dir === 'rtl' || document.body.dir === 'rtl';

            if (isCollapsed) {
                icon.className = isRTL ? 'bi bi-chevron-left fs-6' : 'bi bi-chevron-right fs-6';
                btn.setAttribute('title', 'توسيع القائمة الجانبية');
            } else {
                icon.className = isRTL ? 'bi bi-chevron-right fs-6' : 'bi bi-chevron-left fs-6';
                btn.setAttribute('title', 'تصغير القائمة الجانبية');
            }
        },

        init: function () {
            const sidebar = document.getElementById('desktopSidebar');
            if (!sidebar) return;
            const savedState = localStorage.getItem(STORAGE_KEY);
            const isTablet = window.innerWidth >= 768 && window.innerWidth < 992;
            const shouldCollapse = savedState === 'true' || (savedState === null && isTablet);

            if (shouldCollapse) {
                sidebar.classList.add('sidebar-collapsed');
                this.updateToggleIcon(true);
            } else {
                sidebar.classList.remove('sidebar-collapsed');
                this.updateToggleIcon(false);
            }

            // Auto-close mobile offcanvas drawer upon navigation link click
            document.querySelectorAll('#mobileSidebar .sidebar-link').forEach(function (link) {
                link.addEventListener('click', function () {
                    const offcanvasEl = document.getElementById('mobileSidebar');
                    if (offcanvasEl && window.bootstrap && window.bootstrap.Offcanvas) {
                        const bsOffcanvas = bootstrap.Offcanvas.getInstance(offcanvasEl);
                        if (bsOffcanvas) bsOffcanvas.hide();
                    }
                });
            });
        }
    };

    window.SidebarService = SidebarService;
    window.toggleDesktopSidebar = function () { SidebarService.toggleDesktop(); };
})(window);
