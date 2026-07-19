using AshtavinayakAPP.Models;

namespace AshtavinayakAPP.Services.RazorPay
{
    public interface IRazorpayService
    {
        Task<(bool Success, string Message, object Data)> GenarateRazopayOrderAsynch(RazorpayOrderRequestModel razorpayOrderRequestModel);
    }
}
