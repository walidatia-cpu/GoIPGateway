/**
 * GoIP Gateway - Alert Service
 * Manages auto-dismissible alerts with linear countdown progress bar and hover-pause.
 */
(function (window) {
    'use strict';

    const AlertService = {
        init: function (container) {
            this.setupAlerts(container);
            this.setupMutationObserver();
        },

        setupAlerts: function (container) {
            const root = container || document;
            const alerts = root.querySelectorAll('.alert-dismissible');

            alerts.forEach(function (alertEl) {
                if (alertEl.dataset.autoDismissInit === 'true') return;
                alertEl.dataset.autoDismissInit = 'true';
                alertEl.classList.add('auto-dismiss-alert', 'position-relative', 'overflow-hidden');

                let progressBar = alertEl.querySelector('.alert-progress-bar');
                if (!progressBar) {
                    progressBar = document.createElement('div');
                    progressBar.className = 'alert-progress-bar';
                    const progressInner = document.createElement('div');
                    progressInner.className = 'alert-progress-inner';
                    progressBar.appendChild(progressInner);
                    alertEl.appendChild(progressBar);
                }

                const progressInner = alertEl.querySelector('.alert-progress-inner');
                const timeoutMs = parseInt(alertEl.getAttribute('data-timeout') || '20000', 10);

                if (progressInner) {
                    progressInner.style.animationDuration = timeoutMs + 'ms';
                }

                let remainingMs = timeoutMs;
                let startTime = Date.now();
                let timerId = null;

                function startTimer() {
                    startTime = Date.now();
                    timerId = setTimeout(function () {
                        try {
                            if (window.bootstrap && window.bootstrap.Alert) {
                                const bsAlert = bootstrap.Alert.getOrCreateInstance(alertEl);
                                bsAlert.close();
                            } else {
                                closeFallback();
                            }
                        } catch {
                            closeFallback();
                        }
                    }, remainingMs);
                }

                function closeFallback() {
                    alertEl.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
                    alertEl.style.opacity = '0';
                    alertEl.style.transform = 'translateY(-10px)';
                    setTimeout(() => alertEl.remove(), 400);
                }

                function pauseTimer() {
                    if (timerId) {
                        clearTimeout(timerId);
                        timerId = null;
                        const elapsed = Date.now() - startTime;
                        remainingMs = Math.max(0, remainingMs - elapsed);
                        if (progressInner) {
                            progressInner.style.animationPlayState = 'paused';
                        }
                    }
                }

                function resumeTimer() {
                    if (!timerId && remainingMs > 0) {
                        if (progressInner) {
                            progressInner.style.animationPlayState = 'running';
                        }
                        startTimer();
                    }
                }

                alertEl.addEventListener('mouseenter', pauseTimer);
                alertEl.addEventListener('mouseleave', resumeTimer);

                startTimer();
            });
        },

        setupMutationObserver: function () {
            if (!window.MutationObserver) return;
            const self = this;
            const observer = new MutationObserver(function () {
                self.setupAlerts();
            });
            observer.observe(document.body, { childList: true, subtree: true });
        }
    };

    window.AlertService = AlertService;
    window.setupAutoDismissAlerts = function (container) { AlertService.setupAlerts(container); };
})(window);
