/**
 * GoIP Gateway - Global Application Bootstrapper
 * Orchestrates client-side services (Theme, Sidebar, TableSticky, Combobox, Modal, Alerts, SignalR).
 */
document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    // 1. Initialize Theme Engine (Light / Dark)
    if (window.ThemeService) {
        window.ThemeService.init();
    }

    // 2. Initialize Sidebar & Responsive Navigation
    if (window.SidebarService) {
        window.SidebarService.init();
    }

    // 3. Initialize Sticky Tables & Dynamic Scroll Shadows
    if (window.TableStickyService) {
        window.TableStickyService.init();
    }

    // 4. Initialize Universal Searchable Comboboxes
    if (window.ComboboxService) {
        window.ComboboxService.init();
    }

    // 5. Initialize Confirmation Modal Interceptor
    if (window.ConfirmModalService) {
        window.ConfirmModalService.init();
    }

    // 6. Initialize Auto-dismissible Alerts with Linear Countdown
    if (window.AlertService) {
        window.AlertService.init();
    }

    // 7. Initialize SignalR Real-Time Hub
    if (window.SignalRService) {
        const tenantMeta = document.querySelector('meta[name="user-tenant-id"]');
        const tenantId = tenantMeta ? tenantMeta.getAttribute('content') : '';
        window.SignalRService.init({ tenantId: tenantId });
    }
});
