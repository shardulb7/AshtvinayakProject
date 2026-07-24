using AshtavinayakAPP.Models;
using Razorpay.Api;
using Microsoft.Extensions.Configuration;

namespace AshtavinayakAPP.Services.RazorPay
{
    /// <summary>
    /// Handles Razorpay payment order creation.
    /// Reads API credentials from configuration — never hard-coded.
    ///
    /// Required config keys (set via environment variables in Production):
    ///   Razorpay__Key     → Razorpay:Key
    ///   Razorpay__Secret  → Razorpay:Secret
    /// </summary>
    public class RazorpayService : IRazorpayService
    {
        private readonly string _razorpayKey;
        private readonly string _razorpaySecret;
        private readonly ILogger<RazorpayService> _logger;

        public RazorpayService(IConfiguration configuration, ILogger<RazorpayService> logger)
        {
            _logger = logger;

            _razorpayKey = configuration["Razorpay:Key"]
                ?? throw new InvalidOperationException(
                    "Razorpay Key 'Razorpay:Key' is not configured. " +
                    "In Development, set it in appsettings.Development.json. " +
                    "In Production, set the environment variable 'Razorpay__Key'.");

            _razorpaySecret = configuration["Razorpay:Secret"]
                ?? throw new InvalidOperationException(
                    "Razorpay Secret 'Razorpay:Secret' is not configured. " +
                    "In Development, set it in appsettings.Development.json. " +
                    "In Production, set the environment variable 'Razorpay__Secret'.");
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string Message, object Data)> GenarateRazopayOrderAsynch(
            RazorpayOrderRequestModel razorpayOrderRequestModel)
        {
            try
            {
                var result = await Task.Run(() =>
                {
                    var client = new RazorpayClient(_razorpayKey, _razorpaySecret);

                    string receipt = $"rcpt_{DateTime.UtcNow:yyyyMMddHHmmssfff}";

                    var options = new Dictionary<string, object>
                    {
                        { "amount",          razorpayOrderRequestModel.Amount * 100 }, // Convert to paise
                        { "currency",        "INR" },
                        { "receipt",         receipt },
                        { "payment_capture", 1 }
                    };

                    Order order = client.Order.Create(options);

                    return new
                    {
                        OrderId  = order["id"]?.ToString(),
                        Amount   = Convert.ToInt32(order["amount"]),
                        Currency = order["currency"]?.ToString(),
                        Receipt  = order["receipt"]?.ToString()
                    };
                });

                _logger.LogInformation("Razorpay order created. Receipt: {Receipt}", (string?)result.Receipt);
                return (true, "Razorpay order created successfully", (object)result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Razorpay order for amount {Amount}.",
                    razorpayOrderRequestModel.Amount);
                return (false, "Failed to create Razorpay order. Please try again.", null);
            }
        }
    }
}
