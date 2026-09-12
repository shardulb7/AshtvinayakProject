using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using AshtavinayakAPP.Services.SmsService;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Configuration;
using BCrypt.Net;
using Azure.Core;


namespace AshtavinayakAPP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly IConfiguration _configuration;
        private readonly ISmsService _smsService;
        private readonly ILogger<UserController> _logger; // LOW-08
        private readonly IWebHostEnvironment _env;
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _otpStorage = new();

        public UserController(AshtvinayakTravelContext context, IConfiguration configuration,
            ISmsService smsService, ILogger<UserController> logger, IWebHostEnvironment env)
        {
            _context       = context;
            _configuration = configuration;
            _smsService    = smsService;
            _logger        = logger; // LOW-08
            _env           = env;
        }

        // POST: api/Users/Register
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if the email is already registered
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                return Conflict("Email is already registered.");
            }

            // Check if the phone number is already registered (assuming the User model has PhoneNumber)
            if (await _context.Users.AnyAsync(u => u.PhoneNumber == user.PhoneNumber))
            {
                return Conflict("Phone number is already registered.");
            }

            // Hash the password before storing it
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

            // Add the user to the database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Return success message
            return Ok(new { Message = "User registered successfully!" });
        }


        // POST: api/Users/LoginByOTP
        [HttpPost("LoginByOTP")]
        public async Task<IActionResult> LoginByOTP([FromBody] MobileNoRequest mobileNoRequest)
        {
            if (string.IsNullOrEmpty(mobileNoRequest.MobileNo))
            {
                return BadRequest("Mobile number is required.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == mobileNoRequest.MobileNo);
            if (user == null)
            {
                return NotFound("This mobile number is not registered. Please register first.");
            }

            // MED-07: Use cryptographically secure RNG — System.Random is predictable
            var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var otpExpiry = DateTime.UtcNow.AddMinutes(5); // OTP expires after 5 minutes

            // Store OTP temporarily with expiry
            _otpStorage[mobileNoRequest.MobileNo] = (otp.ToString(), otpExpiry);

            var message = $"Welcome to iTas Tourism Your OTP for Ashtavinayak Yatra is {otp}. It is valid for 5 minutes. Do not share this with anyone.";
            var otpTemplateId = _configuration["SmsGateway:OtpTemplateId"] ?? string.Empty;
            var isSent = await _smsService.SendAsync(mobileNoRequest.MobileNo, message, otpTemplateId);

            if (!isSent)
            {
                // Log the failure but still allow OTP verification — the OTP is stored in memory
                // and the user can retry. Do not expose SMS failure to the caller (security).
                _logger.LogWarning("[OTP] SMS delivery failed for {Phone}. Gateway returned failure.", mobileNoRequest.MobileNo);
            }

            // DEV-ONLY: echo the OTP back in the response so the flow is testable without a live
            // SMS gateway. Gated on IsDevelopment() — never happens in Production, and the OTP is
            // still never written to logs (see VerifyOTP).
            if (_env.IsDevelopment())
            {
                return Ok(new { Message = "OTP sent successfully to your mobile.", DevOnlyOtp = otp });
            }

            return Ok(new { Message = "OTP sent successfully to your mobile." });
        }

        // POST: api/Users/VerifyOTP
        [HttpPost("VerifyOTP")]
        public async Task<IActionResult> VerifyOTP([FromBody] OTPRequest otpRequest)
        {
            if (string.IsNullOrEmpty(otpRequest.MobileNo) || string.IsNullOrEmpty(otpRequest.OTP))
            {
                return BadRequest("Mobile number and OTP are required.");
            }

            if (!_otpStorage.TryGetValue(otpRequest.MobileNo, out var storedOtp))
            {
                return BadRequest("Invalid OTP or OTP expired.");
            }

            // LOW-08: use LogDebug — disabled in Production (log level Warning) to prevent OTP leakage in logs
            _logger.LogDebug("OTP verification attempt for {Mobile} — expiry {Expiry}", otpRequest.MobileNo, storedOtp.Expiry);

            // Check OTP expiry
            if (storedOtp.Expiry < DateTime.UtcNow)
            {
                _otpStorage.TryRemove(otpRequest.MobileNo, out _);
                return BadRequest("OTP has expired. Please request a new OTP.");
            }

            // Check OTP match
            if (storedOtp.Otp != otpRequest.OTP)
            {
                return BadRequest("Invalid OTP.");
            }

            // Remove OTP after successful verification
            _otpStorage.TryRemove(otpRequest.MobileNo, out _);

            // Find user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == otpRequest.MobileNo);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Generate JWT Token
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                Message = "OTP verified successfully.",
                Token = token,
                User = new
                {
                    user.UserId,
                    user.UserName,
                    user.Email,
                    user.PhoneNumber,
                    user.Role
                }
            });
        }


        // POST: api/User/TestLogin
        // Testing-only bypass endpoint — issues a real JWT without OTP verification.
        // DISABLED by default. Enable by setting Testing__Key to a secret value in
        // Azure App Service Configuration. Leave blank/missing to disable entirely.
        // Never expose this in production without a strong, random Testing__Key.
        [AllowAnonymous]
        [HttpPost("TestLogin")]
        public async Task<IActionResult> TestLogin([FromBody] TestLoginRequest request)
        {
            var testingKey = _configuration["Testing:Key"];
            if (string.IsNullOrWhiteSpace(testingKey))
                return NotFound(); // Endpoint invisible when not configured

            if (request.TestKey != testingKey)
                return Unauthorized(new { Message = "Invalid testing key." });

            if (string.IsNullOrEmpty(request.MobileNo))
                return BadRequest(new { Message = "MobileNo is required." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.MobileNo);
            if (user == null)
                return NotFound(new { Message = "Mobile number not registered." });

            var token = GenerateJwtToken(user);
            _logger.LogWarning("[TestLogin] JWT issued without OTP for {Phone} — testing bypass used.", request.MobileNo);

            return Ok(new
            {
                Message = "Test login successful.",
                Token = token,
                User = new
                {
                    user.UserId,
                    user.UserName,
                    user.Email,
                    user.PhoneNumber,
                    user.Role
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings  = _configuration.GetSection("JwtSettings");
            var secretKey    = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("PhoneNumber", user.PhoneNumber)
    };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24), // MED-08: was AddDays(365) — short-lived tokens limit blast radius
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }

    public class MobileNoRequest
    {
        public string MobileNo { get; set; }
    }

    public class OTPRequest
    {
        public string MobileNo { get; set; }
        public string OTP { get; set; }
    }

    public class TestLoginRequest
    {
        public string MobileNo { get; set; }
        public string TestKey { get; set; }
    }
}