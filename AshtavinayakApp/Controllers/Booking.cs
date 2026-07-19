using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Linq;
using Humanizer;
using AshtavinayakAPP.Services.BookingSrc;
using Microsoft.AspNetCore.Authorization;


namespace AshtavinayakAPP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookinService;

        public BookingController(IBookingService bookinService)
        {
            _bookinService= bookinService;
        }

        [HttpGet("FamilyBookingHistory/{userId}")]
        public async Task<ActionResult> GetFamilyBookingHistory(int userId)
        {
            var (success, message, data) = await _bookinService.GetFamilyBookingHistoryAsync(userId);

            if (success)
                return Ok(new { Message = message, Data = data });

            return NotFound(new { Message = message });
        }

        [HttpGet("HistoryByUser/{userId}")]
        public async Task<ActionResult> GetHistoryByUser(int userId)
        {
            var (success, message, data) = await _bookinService.GetBookingHistoryByUserAsync(userId);

            if (success)
                return Ok(new { Message = message, Data = data });

            return NotFound(new { Message = message });
        }



        [HttpPost("CreateBookingWithSeats")]
        public async Task<ActionResult> CreateBookingWithSeats([FromBody] BookingRequestDto request)
        {
            var (success, message, data) = await _bookinService.CreateBookingWithSeatsAsync(request);

            if (success)
                return Ok(new { Message = message, Data = data });

            return BadRequest(new { Message = message, Data = data });
        }


        [HttpPost("BookCar")]
        public async Task<ActionResult> BookCar([FromBody] CarBookingDTOModel request)
        {
            var (success, message, data) = await _bookinService.BookCarAsync(request);

            if (success)
                return Ok(new { Message = message, Data = data });

            return BadRequest(new { Message = message, Data = data });
        }

        /// <summary>
        /// Update payment details for a booking by adding a new transaction.
        /// </summary>
        /// <param name="dto">PaymentUpdateDto with booking ID, user ID, amount, payment method, and transaction reference.</param>
        /// <returns>Booking updated payment details (total paid, pending amount)</returns>
        [HttpPost("UpdatePayment")]
        public async Task<IActionResult> UpdatePayment([FromBody] Transaction dto)
        {
            var result = await _bookinService.UpdatePaymentAsync(dto);

            if (!result.Success)
                return BadRequest(new { result.Message, result.Data });

            return Ok(new { result.Message, result.Data });
        }

        [HttpGet("DownLoadInvoice/{bookingId}")]
        public async Task<ActionResult> GetInvoice(int bookingId)
        {
            var (success, message, data) = await _bookinService.GetInvoiceAsync(bookingId);

            if (success)
                return Ok(new { Message = message, Data = data });

            return NotFound(new { Message = message });
        }
    }
}
