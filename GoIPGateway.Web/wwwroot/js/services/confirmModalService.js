/**
 * GoIP Gateway - Confirmation Modal Service
 * Intercepts forms with data-confirm="true" and provides programmatic confirm dialogs.
 */
(function (window) {
    'use strict';

    let globalPendingConfirmAction = null;

    const ConfirmModalService = {
        init: function () {
            this.setupFormInterceptor();
            this.setupModalListener();
        },

        show: function (options) {
            const modalEl = document.getElementById('globalConfirmModal');
            if (!modalEl || !window.bootstrap) return;

            const titleEl = document.getElementById('globalConfirmTitle');
            const msgEl = document.getElementById('globalConfirmMessage');
            const detailBox = document.getElementById('globalConfirmDetailBox');
            const detailText = document.getElementById('globalConfirmDetailText');
            const detailLabel = document.getElementById('globalConfirmDetailLabel');
            const consequenceBox = document.getElementById('globalConfirmConsequenceBox');
            const consequenceEl = document.getElementById('globalConfirmConsequence');
            const btn = document.getElementById('globalConfirmActionBtn');
            const btnText = document.getElementById('globalConfirmActionBtnText');
            const btnIcon = document.getElementById('globalConfirmActionBtnIcon');
            const cancelBtn = document.getElementById('globalConfirmCancelBtn');
            const iconWrapper = document.getElementById('globalConfirmIconWrapper');
            const icon = document.getElementById('globalConfirmIcon');

            if (titleEl) titleEl.innerText = options.title || 'تأكيد الإجراء';
            if (msgEl) msgEl.innerText = options.message || 'هل أنت متأكد من تنفيذ هذه العملية؟';

            if (detailBox && detailText) {
                if (options.detail) {
                    detailBox.classList.remove('d-none');
                    detailText.innerText = options.detail;
                    if (detailLabel && options.detailLabel) detailLabel.innerText = options.detailLabel;
                } else {
                    detailBox.classList.add('d-none');
                }
            }

            if (consequenceBox && consequenceEl) {
                if (options.consequence) {
                    consequenceBox.classList.remove('d-none');
                    consequenceEl.innerText = options.consequence;
                } else {
                    consequenceBox.classList.add('d-none');
                }
            }

            const isAlertMode = (options.hideCancel === true || options.isAlert === true);
            if (cancelBtn) {
                cancelBtn.classList.toggle('d-none', isAlertMode);
            }

            const type = options.type || 'danger';
            if (iconWrapper) {
                iconWrapper.className = 'confirm-icon-wrapper mb-3 mx-auto confirm-icon-' + type;
            }

            let btnClass = 'btn-danger';
            let defaultIcon = 'bi-shield-x';

            if (type === 'danger') {
                btnClass = 'btn-danger';
                defaultIcon = 'bi-shield-x';
            } else if (type === 'warning') {
                btnClass = 'btn-warning text-dark';
                defaultIcon = 'bi-exclamation-triangle-fill';
            } else if (type === 'info') {
                btnClass = 'btn-primary';
                defaultIcon = 'bi-info-circle-fill';
            } else if (type === 'success') {
                btnClass = 'btn-success';
                defaultIcon = 'bi-check-circle-fill';
            }

            if (icon) icon.className = 'bi ' + defaultIcon;
            if (btn) btn.className = 'btn ' + btnClass + ' px-4 py-2 rounded-pill fw-bold fs-7 flex-grow-1 shadow-sm d-flex align-items-center justify-content-center gap-1.5';
            if (btnText) btnText.innerText = options.btnText || 'نعم، تأكيد';
            if (btnIcon) btnIcon.className = 'bi ' + (options.btnIcon || 'bi-check-circle-fill');

            globalPendingConfirmAction = options.onConfirm || null;
            const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);
            bsModal.show();
        },

        setupModalListener: function () {
            const confirmBtn = document.getElementById('globalConfirmActionBtn');
            if (confirmBtn) {
                confirmBtn.addEventListener('click', function () {
                    const modalEl = document.getElementById('globalConfirmModal');
                    if (modalEl && window.bootstrap) {
                        const bsModal = bootstrap.Modal.getInstance(modalEl);
                        if (bsModal) bsModal.hide();
                    }

                    if (typeof globalPendingConfirmAction === 'function') {
                        const act = globalPendingConfirmAction;
                        globalPendingConfirmAction = null;
                        act();
                    }
                });
            }
        },

        setupFormInterceptor: function () {
            const self = this;
            document.addEventListener('submit', function (e) {
                const form = e.target;
                if (form.getAttribute('data-confirm') === 'true' && !form.dataset.confirmed) {
                    e.preventDefault();
                    self.show({
                        title: form.getAttribute('data-confirm-title') || 'تأكيد العملية الحساسة',
                        message: form.getAttribute('data-confirm-message') || 'هل أنت متأكد من تنفيذ هذا الإجراء؟',
                        detail: form.getAttribute('data-confirm-detail') || '',
                        detailLabel: form.getAttribute('data-confirm-detail-label') || 'العنصر المراد:',
                        consequence: form.getAttribute('data-confirm-consequence') || '',
                        btnText: form.getAttribute('data-confirm-btn') || 'نعم، تأكيد الإجراء',
                        type: form.getAttribute('data-confirm-type') || 'danger',
                        onConfirm: function () {
                            form.dataset.confirmed = 'true';
                            form.submit();
                        }
                    });
                }
            });
        }
    };

    window.ConfirmModalService = ConfirmModalService;
    window.showConfirmModal = function (options) { ConfirmModalService.show(options); };
    window.showAlertModal = function (options, title, type) {
        let opts = {};
        if (typeof options === 'string') {
            opts = {
                message: options,
                title: title || 'تنبيه النظام',
                type: type || 'warning',
                btnText: 'حسناً، فهمت',
                hideCancel: true
            };
        } else {
            opts = Object.assign({}, options);
            opts.hideCancel = true;
        }
        ConfirmModalService.show(opts);
    };
})(window);

