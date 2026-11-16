/**
 * GoIP Gateway - Universal Searchable Combobox Service
 * Transforms native <select> into modern, searchable dropdowns with generous sizing.
 * Automatically enhances <select class="searchable-select"> and <select data-searchable="true">.
 */
(function (window) {
    'use strict';

    const ComboboxService = {
        init: function (container) {
            this.enhanceAll(container);
            this.setupGlobalListeners();
            this.setupMutationObserver();
        },

        enhanceAll: function (container) {
            const root = container || document;
            const selects = root.querySelectorAll('select.searchable-select, select[data-searchable="true"], select.form-select:not([data-searchable="false"]):not([multiple]):not(.no-search)');
            const self = this;
            selects.forEach(function (select) {
                self.enhance(select);
            });
        },

        enhance: function (select) {
            if (!select || select.dataset.searchableInit === 'true') return;
            select.dataset.searchableInit = 'true';

            const isSm = select.classList.contains('form-select-sm') || select.classList.contains('input-sm');
            const placeholder = select.getAttribute('data-placeholder') || 'اختر أو ابحث...';
            const searchPlaceholder = select.getAttribute('data-search-placeholder') || 'ابحث هنا...';
            const allowClear = select.getAttribute('data-allow-clear') === 'true';

            // Create Wrapper
            const wrapper = document.createElement('div');
            wrapper.className = 'searchable-combobox ' + (isSm ? 'combobox-sm ' : '') + (select.className.includes('w-100') ? 'w-100 ' : '');
            wrapper._sourceSelect = select;
            select._comboboxWrapper = wrapper;

            // Create Trigger
            const trigger = document.createElement('div');
            trigger.className = 'combobox-trigger d-flex align-items-center justify-content-between ' + (isSm ? 'combobox-trigger-sm' : '');
            trigger.setAttribute('tabindex', '0');
            trigger.setAttribute('role', 'combobox');
            trigger.setAttribute('aria-expanded', 'false');

            const triggerContent = document.createElement('div');
            triggerContent.className = 'd-flex align-items-center gap-2 overflow-hidden text-truncate flex-grow-1';

            const triggerIcons = document.createElement('div');
            triggerIcons.className = 'd-flex align-items-center gap-1.5 ms-2 flex-shrink-0';

            // Optional Clear Button
            if (allowClear) {
                const clearBtn = document.createElement('i');
                clearBtn.className = 'bi bi-x-circle text-muted fs-7 combobox-clear-btn d-none';
                clearBtn.setAttribute('title', 'إلغاء التحديد');
                clearBtn.addEventListener('click', function (e) {
                    e.stopPropagation();
                    select.value = '';
                    select.dispatchEvent(new Event('change', { bubbles: true }));
                    select.dispatchEvent(new Event('input', { bubbles: true }));
                    updateTriggerDisplay();
                });
                triggerIcons.appendChild(clearBtn);
            }

            const caret = document.createElement('i');
            caret.className = 'bi bi-chevron-down text-muted fs-7 combobox-caret';
            triggerIcons.appendChild(caret);

            trigger.appendChild(triggerContent);
            trigger.appendChild(triggerIcons);
            wrapper.appendChild(trigger);

            // Create Elevated Menu Dropdown
            const menu = document.createElement('div');
            menu.className = 'combobox-menu shadow-lg';

            // Search Box inside Dropdown
            const searchBox = document.createElement('div');
            searchBox.className = 'combobox-search-box';
            searchBox.innerHTML = `
                <div class="combobox-search-input-group">
                    <i class="bi bi-search text-muted fs-7 flex-shrink-0 ms-1 me-2"></i>
                    <input type="text" class="combobox-search-input" placeholder="${searchPlaceholder}" autocomplete="off" />
                </div>
            `;
            menu.appendChild(searchBox);

            // Items Container
            const itemsContainer = document.createElement('div');
            itemsContainer.className = 'combobox-items-list';
            menu.appendChild(itemsContainer);

            // Empty State Notice
            const noResults = document.createElement('div');
            noResults.className = 'combobox-no-results text-center py-4 text-muted small d-none';
            noResults.innerHTML = '<i class="bi bi-search d-block mb-1 fs-5 opacity-50"></i> لا توجد نتائج مطابقة للبحث';
            menu.appendChild(noResults);

            wrapper.appendChild(menu);

            // Check if search box should be visible (only if > 4 options or explicitly requested)
            function evaluateSearchBoxVisibility() {
                const totalOptions = Array.from(select.options).filter(o => o.value !== '' || o.text.trim() !== '').length;
                const explicitlyShow = select.getAttribute('data-search-box') === 'true';
                const explicitlyHide = select.getAttribute('data-search-box') === 'false';
                if (explicitlyHide || (totalOptions <= 4 && !explicitlyShow)) {
                    searchBox.style.display = 'none';
                } else {
                    searchBox.style.display = 'block';
                }
            }

            // Build Option Items
            function buildItems() {
                itemsContainer.innerHTML = '';
                evaluateSearchBoxVisibility();

                Array.from(select.children).forEach(function (child) {
                    if (child.tagName === 'OPTGROUP') {
                        const groupHeader = document.createElement('div');
                        groupHeader.className = 'combobox-group-header';
                        groupHeader.textContent = child.label;
                        itemsContainer.appendChild(groupHeader);
                        Array.from(child.children).forEach(opt => appendOptionItem(opt));
                    } else if (child.tagName === 'OPTION') {
                        appendOptionItem(child);
                    }
                });
            }

            function appendOptionItem(opt) {
                const item = document.createElement('div');
                const isSel = opt.selected;
                item.className = 'combobox-item' + (isSel ? ' selected' : '');
                item.setAttribute('data-value', opt.value);
                item.setAttribute('data-search', (opt.text + ' ' + (opt.getAttribute('data-search') || '')).toLowerCase());

                const badge = opt.getAttribute('data-badge');
                const badgeColor = opt.getAttribute('data-badge-color');
                const badgeClass = opt.getAttribute('data-badge-class') || (badgeColor ? `badge bg-${badgeColor} text-white` : 'badge bg-body-secondary text-body border');
                const icon = opt.getAttribute('data-icon');

                let itemHtml = '<div class="d-flex align-items-center gap-2 overflow-hidden text-truncate">';
                if (icon) {
                    itemHtml += `<i class="bi ${icon} text-primary flex-shrink-0"></i>`;
                }
                itemHtml += `<span class="text-truncate">${opt.text}</span></div>`;
                if (badge) {
                    itemHtml += `<span class="${badgeClass} fs-8 ms-2 flex-shrink-0 px-2.5 py-1 rounded-pill">${badge}</span>`;
                }
                item.innerHTML = itemHtml;

                item.addEventListener('click', function (e) {
                    e.stopPropagation();
                    select.value = opt.value;
                    select.dispatchEvent(new Event('change', { bubbles: true }));
                    select.dispatchEvent(new Event('input', { bubbles: true }));
                    updateTriggerDisplay();
                    closeMenu();
                });

                itemsContainer.appendChild(item);
            }

            function updateTriggerDisplay() {
                const currentOpt = select.options[select.selectedIndex];
                const clearBtn = triggerIcons.querySelector('.combobox-clear-btn');

                if (!currentOpt || currentOpt.value === '') {
                    const promptText = currentOpt ? currentOpt.text : placeholder;
                    triggerContent.innerHTML = `<span class="text-muted text-truncate">${promptText}</span>`;
                    if (clearBtn) clearBtn.classList.add('d-none');
                } else {
                    const icon = currentOpt.getAttribute('data-icon');
                    const badge = currentOpt.getAttribute('data-badge');
                    const badgeColor = currentOpt.getAttribute('data-badge-color');
                    const badgeClass = currentOpt.getAttribute('data-badge-class') || (badgeColor ? `badge bg-${badgeColor} text-white` : 'badge bg-primary-subtle text-primary border border-primary-subtle');
                    let html = '';
                    if (icon) html += `<i class="bi ${icon} text-primary flex-shrink-0"></i> `;
                    html += `<span class="fw-semibold text-main text-truncate">${currentOpt.text}</span>`;
                    if (badge) html += ` <span class="${badgeClass} fs-8 ms-1.5 px-2.5 py-0.5 rounded-pill">${badge}</span>`;
                    triggerContent.innerHTML = html;
                    if (clearBtn) clearBtn.classList.remove('d-none');
                }

                itemsContainer.querySelectorAll('.combobox-item').forEach(function (it) {
                    if (it.getAttribute('data-value') === select.value) {
                        it.classList.add('selected');
                    } else {
                        it.classList.remove('selected');
                    }
                });
            }

            function openMenu() {
                document.querySelectorAll('.combobox-menu.show').forEach(function (m) {
                    if (m !== menu) {
                        m.classList.remove('show');
                        m.parentElement.querySelector('.combobox-trigger')?.classList.remove('active');
                    }
                });
                menu.classList.add('show');
                trigger.classList.add('active');
                trigger.setAttribute('aria-expanded', 'true');

                const input = searchBox.querySelector('.combobox-search-input');
                if (input && searchBox.style.display !== 'none') {
                    input.value = '';
                    filterItems('');
                    setTimeout(() => input.focus(), 60);
                }
            }

            function closeMenu() {
                menu.classList.remove('show');
                trigger.classList.remove('active');
                trigger.setAttribute('aria-expanded', 'false');
            }

            function filterItems(query) {
                const q = (query || '').trim().toLowerCase();
                let visibleCount = 0;
                itemsContainer.querySelectorAll('.combobox-item').forEach(function (it) {
                    const searchData = it.getAttribute('data-search') || '';
                    const matches = !q || searchData.includes(q);
                    it.style.display = matches ? 'flex' : 'none';
                    if (matches) visibleCount++;
                });

                itemsContainer.querySelectorAll('.combobox-group-header').forEach(function (header) {
                    let next = header.nextElementSibling;
                    let hasVisible = false;
                    while (next && !next.classList.contains('combobox-group-header')) {
                        if (next.style.display !== 'none') hasVisible = true;
                        next = next.nextElementSibling;
                    }
                    header.style.display = hasVisible ? 'block' : 'none';
                });

                noResults.classList.toggle('d-none', visibleCount > 0);
            }

            const searchInput = searchBox.querySelector('.combobox-search-input');
            if (searchInput) {
                searchInput.addEventListener('input', e => filterItems(e.target.value));
                searchInput.addEventListener('click', e => e.stopPropagation());
            }

            trigger.addEventListener('click', function (e) {
                e.stopPropagation();
                if (menu.classList.contains('show')) closeMenu();
                else openMenu();
            });

            trigger.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ' || e.key === 'ArrowDown') {
                    e.preventDefault();
                    openMenu();
                }
            });

            // Sync when select value changes programmatically or via form reset
            select.addEventListener('change', updateTriggerDisplay);
            if (select.form) {
                select.form.addEventListener('reset', function () {
                    setTimeout(updateTriggerDisplay, 50);
                });
            }

            // Expose rebuild method
            wrapper._rebuild = function () {
                buildItems();
                updateTriggerDisplay();
            };

            // Hide original select and insert combobox right after it
            select.style.display = 'none';
            select.parentNode.insertBefore(wrapper, select.nextSibling);

            buildItems();
            updateTriggerDisplay();
        },

        rebuild: function (select) {
            if (select && select._comboboxWrapper && typeof select._comboboxWrapper._rebuild === 'function') {
                select._comboboxWrapper._rebuild();
            }
        },

        setupGlobalListeners: function () {
            if (this._globalListenersSet) return;
            this._globalListenersSet = true;

            document.addEventListener('click', function (e) {
                if (!e.target.closest('.searchable-combobox')) {
                    document.querySelectorAll('.combobox-menu.show').forEach(function (m) {
                        m.classList.remove('show');
                        m.parentElement.querySelector('.combobox-trigger')?.classList.remove('active');
                        m.parentElement.querySelector('.combobox-trigger')?.setAttribute('aria-expanded', 'false');
                    });
                }
            });

            document.addEventListener('keydown', function (e) {
                if (e.key === 'Escape') {
                    document.querySelectorAll('.combobox-menu.show').forEach(function (m) {
                        m.classList.remove('show');
                        m.parentElement.querySelector('.combobox-trigger')?.classList.remove('active');
                        m.parentElement.querySelector('.combobox-trigger')?.setAttribute('aria-expanded', 'false');
                    });
                }
            });
        },

        setupMutationObserver: function () {
            if (this._observerSet || !window.MutationObserver) return;
            this._observerSet = true;
            const self = this;
            const observer = new MutationObserver(function (mutations) {
                let shouldEnhance = false;
                mutations.forEach(function (m) {
                    if (m.addedNodes && m.addedNodes.length > 0) {
                        for (let i = 0; i < m.addedNodes.length; i++) {
                            const node = m.addedNodes[i];
                            if (node.nodeType === 1 && (node.matches('select.searchable-select, select.form-select') || node.querySelector('select.searchable-select, select.form-select'))) {
                                shouldEnhance = true;
                                break;
                            }
                        }
                    }
                });
                if (shouldEnhance) {
                    self.enhanceAll();
                }
            });

            observer.observe(document.body, { childList: true, subtree: true });
        }
    };

    window.ComboboxService = ComboboxService;

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            ComboboxService.init();
        });
    } else {
        ComboboxService.init();
    }
})(window);
