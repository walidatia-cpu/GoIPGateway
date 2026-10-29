/**
 * GoIP Gateway - SignalR Real-Time Service
 * Manages WebSocket connection to /hubs/gateway, presence groups, and event broadcasting.
 */
(function (window) {
    'use strict';

    const SignalRService = {
        connection: null,

        init: function (options) {
            if (!window.signalR) return;
            const tenantId = (options && options.tenantId) || '';

            const connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/gateway')
                .withAutomaticReconnect()
                .build();

            this.connection = connection;

            const indicator = document.getElementById('signalr-indicator');
            const statusLabel = document.getElementById('signalr-status');

            connection.start().then(() => {
                if (indicator) indicator.style.backgroundColor = '#22c55e';
                if (statusLabel) statusLabel.innerText = 'متصل لحظياً';
                connection.invoke('JoinAdminGroup').catch(err => console.debug(err));
                if (tenantId) {
                    connection.invoke('JoinTenantGroup', tenantId).catch(err => console.debug(err));
                }
            }).catch(err => {
                if (indicator) indicator.style.backgroundColor = '#ef4444';
                if (statusLabel) statusLabel.innerText = 'غير متصل';
            });

            connection.onreconnecting(() => {
                if (indicator) indicator.style.backgroundColor = '#f59e0b';
                if (statusLabel) statusLabel.innerText = 'جاري إعادة الاتصال...';
            });

            connection.onreconnected(() => {
                if (indicator) indicator.style.backgroundColor = '#22c55e';
                if (statusLabel) statusLabel.innerText = 'متصل لحظياً';
            });

            connection.onclose(() => {
                if (indicator) indicator.style.backgroundColor = '#ef4444';
                if (statusLabel) statusLabel.innerText = 'غير متصل';
            });

            connection.on('OnTransactionUpdated', function (tx) {
                window.dispatchEvent(new CustomEvent('goip:transactionUpdated', { detail: tx }));
            });

            connection.on('OnSimBalanceChanged', function (sim) {
                window.dispatchEvent(new CustomEvent('goip:simBalanceChanged', { detail: sim }));
            });

            connection.on('OnPortStatusChanged', function (port) {
                window.dispatchEvent(new CustomEvent('goip:portStatusChanged', { detail: port }));
            });
        }
    };

    window.SignalRService = SignalRService;
})(window);
