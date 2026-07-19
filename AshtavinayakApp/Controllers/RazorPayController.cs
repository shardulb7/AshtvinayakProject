using AshtavinayakAPP.Models;
using AshtavinayakAPP.Services.RazorPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AshtavinayakAPP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RazorPayController : ControllerBase
    {
        private readonly IRazorpayService _razorpayService;
        public RazorPayController(IRazorpayService razorpayService)
        {
            _razorpayService = razorpayService;
        }
        [HttpPost("CreateRazorPayOrder")]
        public async Task<ActionResult> CreateRazorPayOrder([FromBody] RazorpayOrderRequestModel model)
        {
            var (success, message, data) = await _razorpayService.GenarateRazopayOrderAsynch(model);
            if (success)
                return Ok(new { Message = message, Data = data });

            return BadRequest(new { Message = message, Data = data });
        }
    }
}
