using AshtavinayakAPP.Models;
using AshtavinayakAPP.Services.AgentSrc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;

namespace AshtavinayakAPP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly IAgentService _agentService;

        public AgentController(IAgentService agentService)
        {
            _agentService = agentService;
        }

        // POST: api/Agent/Register
        [HttpPost("Register")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Register([FromForm] AgentRegisterDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, data) = await _agentService.RegisterAsync(request);
            if (!success)
                return BadRequest(new { Message = message });

            return Ok(new { Message = message, Data = data });
        }

        // POST: api/Agent/Login
        [HttpPost("Login")]
        [EnableRateLimiting("agent-login")]
        public async Task<IActionResult> Login([FromBody] AgentLoginDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, data) = await _agentService.LoginAsync(request);
            if (!success)
                return Unauthorized(new { Message = message });

            return Ok(new { Message = message, Data = data });
        }

        // POST: api/Agent/ResolveCustomer
        [Authorize(Roles = "Agent")]
        [HttpPost("ResolveCustomer")]
        public async Task<IActionResult> ResolveCustomer([FromBody] ResolveCustomerDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, userId, customerName) = await _agentService.ResolveCustomerAsync(request);
            if (!success)
                return BadRequest(new { Message = message });

            return Ok(new { Message = message, UserId = userId, CustomerName = customerName });
        }

        // GET: api/Agent/Profile
        [Authorize(Roles = "Agent")]
        [HttpGet("Profile")]
        public async Task<IActionResult> Profile()
        {
            var agentId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!int.TryParse(agentId, out var id))
                return Unauthorized();

            var agent = await _agentService.GetByIdAsync(id);
            if (agent == null)
                return NotFound(new { Message = "Agent not found." });

            return Ok(new
            {
                agent.AgentId,
                agent.FullName,
                agent.BusinessName,
                agent.Email,
                agent.MobileNumber,
                agent.Address,
                agent.ApprovalStatus,
                agent.IsActive,
                agent.CommissionPercentage,
            });
        }

        // GET: api/Agent/MyBookings
        // Returns all bookings finalized by the logged-in agent with full tour + commission details.
        [Authorize(Roles = "Agent")]
        [HttpGet("MyBookings")]
        public async Task<IActionResult> MyBookings()
        {
            var agentId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!int.TryParse(agentId, out var id))
                return Unauthorized();

            var bookings = await _agentService.GetMyBookingsAsync(id);
            return Ok(new { Message = "Bookings fetched.", Data = bookings, Count = bookings.Count });
        }
    }
}
