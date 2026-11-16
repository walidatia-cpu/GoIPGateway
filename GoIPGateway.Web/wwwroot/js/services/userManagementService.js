/**
 * GoIP Gateway - User & Role Management Modular Service
 * Handles client-side actions, temp password modals, live search filtering, and permission matrix controls.
 * ZERO inline scripts compliance.
 */
(function (window, document) {
    'use strict';

    const UserManagementService = {
        init: function () {
            this.bindTempPasswordButtons();
            this.bindPermissionGroupToggles();
            this.bindCopyButtons();
        },

        bindTempPasswordButtons: function () {
            document.addEventListener('click', function (e) {
                const btn = e.target.closest('[data-action="set-temp-password"]');
                if (!btn) return;

                e.preventDefault();

                const url = btn.getAttribute('data-url');
                const entityId = btn.getAttribute('data-id');
                const entityType = btn.getAttribute('data-type') || 'user'; // 'user' or 'tenant'
                const entityName = btn.getAttribute('data-name') || 'الحساب';
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

                if (!url || !entityId) return;

                const executeSetTempPassword = function () {
                    btn.disabled = true;
                    const originalHtml = btn.innerHTML;
                    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> جاري التعيين...';

                    const formData = new FormData();
                    if (entityType === 'tenant') {
                        formData.append('tenantId', entityId);
                    } else {
                        formData.append('userId', entityId);
                    }
                    formData.append('__RequestVerificationToken', token);

                    fetch(url, {
                        method: 'POST',
                        body: formData,
                        headers: {
                            'X-Requested-With': 'XMLHttpRequest'
                        }
                    })
                    .then(res => res.json())
                    .then(data => {
                        btn.disabled = false;
                        btn.innerHTML = originalHtml;

                        if (data.success) {
                            UserManagementService.showPasswordModal({
                                title: 'تم تعيين كلمة المرور المؤقتة بنجاح',
                                name: data.userName || entityName,
                                email: data.email || '',
                                password: data.password,
                                message: data.message
                            });
                        } else {
                            if (typeof window.showAlertModal === 'function') {
                                window.showAlertModal(data.message || 'حدث خطأ أثناء تعيين كلمة المرور.', 'خطأ في التعيين', 'danger');
                            } else {
                                alert(data.message || 'حدث خطأ أثناء تعيين كلمة المرور.');
                            }
                        }
                    })
                    .catch(err => {
                        btn.disabled = false;
                        btn.innerHTML = originalHtml;
                        console.error('Error setting temp password:', err);
                        if (typeof window.showAlertModal === 'function') {
                            window.showAlertModal('فشل الاتصال بالخادم. يرجى إعادة المحاولة.', 'فشل الاتصال', 'danger');
                        } else {
                            alert('فشل الاتصال بالخادم. يرجى إعادة المحاولة.');
                        }
                    });
                };

                if (typeof window.showConfirmModal === 'function') {
                    window.showConfirmModal({
                        title: 'تأكيد تعيين كلمة مرور مؤقتة',
                        message: `هل أنت متأكد من تعيين كلمة مرور مؤقتة جديدة؟`,
                        detail: entityName,
                        detailLabel: entityType === 'tenant' ? 'الشركة المستهدفة:' : 'الحساب المستهدف:',
                        consequence: 'سيتم إلغاء كلمة المرور الحالية وإجبار المستخدم على تعيين كلمة مرور شخصية جديدة فور أول تسجيل دخول.',
                        btnText: 'نعم، تعيين كلمة المرور',
                        btnIcon: 'bi-key-fill',
                        type: 'warning',
                        onConfirm: executeSetTempPassword
                    });
                } else if (confirm(`هل أنت متأكد من تعيين كلمة مرور مؤقتة لـ "${entityName}"؟\nسيتم إجبار المستخدم على تغييرها فور أول تسجيل دخول.`)) {
                    executeSetTempPassword();
                }
            });
        },

        showPasswordModal: function (options) {
            let modalEl = document.getElementById('tempPasswordResultModal');
            if (!modalEl) {
                const modalHtml = `
                <div class="modal fade" id="tempPasswordResultModal" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog modal-dialog-centered">
                        <div class="modal-content portal-card shadow-lg border">
                            <div class="modal-header border-bottom pb-3">
                                <h5 class="modal-title fw-black text-main d-flex align-items-center gap-2">
                                    <i class="bi bi-shield-check text-success"></i>
                                    <span id="tempPwdModalTitle">كلمة المرور المؤقتة</span>
                                </h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                            </div>
                            <div class="modal-body py-4">
                                <div class="text-center mb-3">
                                    <div class="avatar-box mx-auto mb-2 p-3 rounded-circle bg-success bg-opacity-10 text-success d-inline-flex">
                                        <i class="bi bi-key-fill fs-2"></i>
                                    </div>
                                    <h6 class="fw-bold mb-1" id="tempPwdModalName"></h6>
                                    <div class="text-muted small font-monospace" id="tempPwdModalEmail"></div>
                                </div>
                                
                                <div class="p-3 rounded-3 mb-3" style="background: rgba(37, 99, 235, 0.06); border: 1px dashed rgba(37, 99, 235, 0.3);">
                                    <label class="form-label fs-8 text-muted fw-bold mb-1">كلمة المرور المؤقتة (احفظها لمشاركتها مع المستخدم):</label>
                                    <div class="input-group">
                                        <input type="text" id="tempPwdModalValue" class="form-control font-monospace fw-bold text-center fs-5" readonly />
                                        <button class="btn btn-primary px-3" type="button" id="tempPwdCopyBtn">
                                            <i class="bi bi-clipboard"></i> نسخ
                                        </button>
                                    </div>
                                </div>

                                <div class="alert alert-warning py-2 px-3 small rounded-3 mb-0 d-flex align-items-center gap-2">
                                    <i class="bi bi-exclamation-triangle-fill flex-shrink-0"></i>
                                    <div>
                                        <strong>ملاحظة أمان:</strong> سيُطلب من المستخدم إجبارياً تعيين كلمة مرور جديدة فور أول تسجيل دخول قبل الوصول لأي صفحة.
                                    </div>
                                </div>
                            </div>
                            <div class="modal-footer border-top pt-3">
                                <button type="button" class="btn btn-secondary px-4 rounded-pill" data-bs-dismiss="modal">إغلاق</button>
                            </div>
                        </div>
                    </div>
                </div>`;
                document.body.insertAdjacentHTML('beforeend', modalHtml);
                modalEl = document.getElementById('tempPasswordResultModal');
            }

            document.getElementById('tempPwdModalTitle').textContent = options.title || 'كلمة المرور المؤقتة';
            document.getElementById('tempPwdModalName').textContent = options.name || '';
            document.getElementById('tempPwdModalEmail').textContent = options.email || '';
            const inputVal = document.getElementById('tempPwdModalValue');
            inputVal.value = options.password || '';

            const copyBtn = document.getElementById('tempPwdCopyBtn');
            copyBtn.onclick = function () {
                inputVal.select();
                navigator.clipboard.writeText(inputVal.value).then(() => {
                    copyBtn.innerHTML = '<i class="bi bi-check2"></i> تم النسخ!';
                    setTimeout(() => {
                        copyBtn.innerHTML = '<i class="bi bi-clipboard"></i> نسخ';
                    }, 2500);
                });
            };

            const bsModal = new bootstrap.Modal(modalEl);
            bsModal.show();
        },

        bindCopyButtons: function () {
            document.addEventListener('click', function (e) {
                const btn = e.target.closest('[data-copy-target]');
                if (!btn) return;

                const targetSelector = btn.getAttribute('data-copy-target');
                const targetEl = document.querySelector(targetSelector);
                if (!targetEl) return;

                const textToCopy = targetEl.value !== undefined ? targetEl.value : targetEl.textContent;
                navigator.clipboard.writeText(textToCopy.trim()).then(() => {
                    const originalText = btn.innerHTML;
                    btn.innerHTML = '<i class="bi bi-check2"></i> تم النسخ!';
                    setTimeout(() => {
                        btn.innerHTML = originalText;
                    }, 2000);
                });
            });
        },

        bindPermissionGroupToggles: function () {
            document.addEventListener('change', function (e) {
                const toggle = e.target.closest('[data-toggle-group]');
                if (!toggle) return;

                const groupKey = toggle.getAttribute('data-toggle-group');
                const isChecked = toggle.checked;

                const groupCheckboxes = document.querySelectorAll(`[data-group-perm="${groupKey}"]`);
                groupCheckboxes.forEach(cb => {
                    if (!cb.disabled) {
                        cb.checked = isChecked;
                    }
                });
            });
        },

        // Client-side live filter for user lists
        filterUsers: function () {
            const searchInput = document.getElementById('userSearchInput');
            const statusSelect = document.getElementById('userStatusFilter');
            const roleSelect = document.getElementById('userRoleFilter');
            const tenantSelect = document.getElementById('userTenantFilter');
            const counter = document.getElementById('usersVisibleCount');
            const emptyState = document.getElementById('usersEmptyState');
            const tableContainer = document.getElementById('usersTableContainer');

            const query = searchInput ? searchInput.value.toLowerCase().trim() : '';
            const status = statusSelect ? statusSelect.value : 'ALL';
            const role = roleSelect ? roleSelect.value : 'ALL';
            const tenant = tenantSelect ? tenantSelect.value : 'ALL';

            const rows = document.querySelectorAll('.user-table-row');
            let visibleCount = 0;

            rows.forEach(row => {
                const name = (row.getAttribute('data-name') || '').toLowerCase();
                const email = (row.getAttribute('data-email') || '').toLowerCase();
                const rowStatus = row.getAttribute('data-status') || '';
                const rowRoles = (row.getAttribute('data-roles') || '').split(',');
                const rowTenant = row.getAttribute('data-tenant') || '';

                const matchesQuery = !query || name.includes(query) || email.includes(query);
                const matchesStatus = status === 'ALL' || rowStatus === status;
                const matchesRole = role === 'ALL' || rowRoles.includes(role);
                const matchesTenant = tenant === 'ALL' || rowTenant === tenant;

                if (matchesQuery && matchesStatus && matchesRole && matchesTenant) {
                    row.style.display = '';
                    visibleCount++;
                } else {
                    row.style.display = 'none';
                }
            });

            if (counter) counter.textContent = visibleCount;
            if (emptyState && tableContainer) {
                if (visibleCount === 0) {
                    emptyState.classList.remove('d-none');
                    tableContainer.classList.add('d-none');
                } else {
                    emptyState.classList.add('d-none');
                    tableContainer.classList.remove('d-none');
                }
            }
        },

        resetUserFilters: function () {
            const searchInput = document.getElementById('userSearchInput');
            const statusSelect = document.getElementById('userStatusFilter');
            const roleSelect = document.getElementById('userRoleFilter');
            const tenantSelect = document.getElementById('userTenantFilter');

            if (searchInput) searchInput.value = '';
            if (statusSelect) {
                statusSelect.value = 'ALL';
                statusSelect.dispatchEvent(new Event('change'));
            }
            if (roleSelect) {
                roleSelect.value = 'ALL';
                roleSelect.dispatchEvent(new Event('change'));
            }
            if (tenantSelect) {
                tenantSelect.value = 'ALL';
                tenantSelect.dispatchEvent(new Event('change'));
            }

            this.filterUsers();
        },

        // Client-side live filter for roles
        filterRoles: function () {
            const searchInput = document.getElementById('roleSearchInput');
            const counter = document.getElementById('rolesVisibleCount');
            const emptyState = document.getElementById('rolesEmptyState');
            const tableContainer = document.getElementById('rolesTableContainer');

            const query = searchInput ? searchInput.value.toLowerCase().trim() : '';
            const rows = document.querySelectorAll('.role-table-row');
            let visibleCount = 0;

            rows.forEach(row => {
                const name = (row.getAttribute('data-name') || '').toLowerCase();
                const desc = (row.getAttribute('data-desc') || '').toLowerCase();

                const matches = !query || name.includes(query) || desc.includes(query);
                if (matches) {
                    row.style.display = '';
                    visibleCount++;
                } else {
                    row.style.display = 'none';
                }
            });

            if (counter) counter.textContent = visibleCount;
            if (emptyState && tableContainer) {
                if (visibleCount === 0) {
                    emptyState.classList.remove('d-none');
                    tableContainer.classList.add('d-none');
                } else {
                    emptyState.classList.add('d-none');
                    tableContainer.classList.remove('d-none');
                }
            }
        },

        resetRoleFilters: function () {
            const searchInput = document.getElementById('roleSearchInput');
            if (searchInput) searchInput.value = '';
            this.filterRoles();
        }
    };

    window.UserManagementService = UserManagementService;

    // Expose helpers globally for SearchFilterBar inline hook callbacks
    window.filterUserTable = function () { UserManagementService.filterUsers(); };
    window.resetUserFilters = function () { UserManagementService.resetUserFilters(); };
    window.filterRoleTable = function () { UserManagementService.filterRoles(); };
    window.resetRoleFilters = function () { UserManagementService.resetRoleFilters(); };

})(window, document);

