using AshtavinayakAPP.Models;
using Razorpay.Api;
using Microsoft.Extensions.Configuration;

namespace AshtavinayakAPP.Services.RazorPay
{
    public class RazorpayService : IRazorpayService
    {
        private readonly string _razorpayKey = "rzp_live_RZB9b8zsHwLGjY";
        private readonly string _razorpaySecret = "IL0f3TDRsp43P13muih0CnWv";
        public async Task<(bool Success, string Message, object Data)> GenarateRazopayOrderAsynch(RazorpayOrderRequestModel razorpayOrderRequestModel)
        {
            try
            {
                var result = await Task.Run(() =>
                {
                    var client = new RazorpayClient(_razorpayKey, _razorpaySecret);

                    string receipt = $"rcpt_{DateTime.UtcNow:yyyyMMddHHmmssfff}";

                    var options = new Dictionary<string, object>
                    {
                        { "amount", razorpayOrderRequestModel.Amount * 100 }, // Amount in paise
                        { "currency", "INR" },
                        { "receipt", receipt },
                        { "payment_capture", 1 }
                    };

                    Order order = client.Order.Create(options);

                    var orderId = order["id"]?.ToString();
                    var amount = Convert.ToInt32(order["amount"]);
                    var currency = order["currency"]?.ToString();
                    var receiptValue = order["receipt"]?.ToString();

                    return new
                    {
                        OrderId = orderId,
                        Amount = amount,
                        Currency = currency,
                        Receipt = receiptValue
                    };
                });

                return (true, "Razorpay order created successfully", result);
            }
            catch (Exception ex)
            {
                return (false, "Failed to create Razorpay order", new { Error = ex.Message });
            }
        }
    }
}
