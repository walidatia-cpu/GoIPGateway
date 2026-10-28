using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GoIPGateway.Infrastructure.Services;

public class WebhookDispatcher : IWebhookDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(HttpClient httpClient, ILogger<WebhookDispatcher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> DispatchTransactionWebhookAsync(TenantTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transaction.CallbackUrl))
            return true; // No callback configured

        try
        {
            var payload = new
            {
                @event = "transaction.completed",
                transactionRef = transaction.TransactionRef,
                externalRef = transaction.ExternalRef,
                status = transaction.Status.ToString(),
                result = new
                {
                    success = transaction.Status == Domain.Enums.TransactionStatus.Completed,
                    rawResponse = transaction.RawResponse,
                    simBalanceBefore = transaction.SimBalanceBefore,
                    simBalanceAfter = transaction.SimBalanceAfter,
                    bankBalance = transaction.BankBalanceAfter
                },
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, transaction.CallbackUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // HMAC signature header
            var secret = "default-secret"; // or tenant secret
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(json))).ToLower();
            request.Headers.Add("X-Signature", hash);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            _logger.LogInformation("Dispatched Webhook for Tx {TxRef} to {Url} with status code {Code}",
                transaction.TransactionRef, transaction.CallbackUrl, response.StatusCode);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook dispatch failed for Tx {TxRef} to {Url}", transaction.TransactionRef, transaction.CallbackUrl);
            return false;
        }
    }
}
