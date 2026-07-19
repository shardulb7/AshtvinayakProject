using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;
using Microsoft.CodeAnalysis.Scripting;
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
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _otpStorage = new();



        public UserController(AshtvinayakTravelContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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

            // Generate a random OTP (6-digit)
            var otp = new Random().Next(100000, 999999).ToString();
            var otpExpiry = DateTime.UtcNow.AddMinutes(5); // OTP expires after 5 minutes

            // Store OTP temporarily with expiry
            _otpStorage[mobileNoRequest.MobileNo] = (otp.ToString(), otpExpiry);

            var message = $"Welcome to iTas Tourism Your OTP for Ashtavinayak Yatra is {otp}. It is valid for 10 minutes. Do not share this with anyone.";
            var isSent = await SendSmsAsync(mobileNoRequest.MobileNo, message);

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

            Console.WriteLine($"Stored OTP: {storedOtp.Otp}, Expiry: {storedOtp.Expiry}");
            Console.WriteLine($"Received OTP: {otpRequest.OTP}");

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

        // Generate a 6-digit OTP
        private string GenerateRandomOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); // Ensures a 6-digit OTP
        }

        // Generate JWT Token
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));

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
                expires: DateTime.UtcNow.AddDays(365),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string baseUrl = "http://bulksmspune.mobi/sendurlcomma.aspx";
                    string encodedMessage = Uri.EscapeDataString(message);

                    string url = $"{baseUrl}?user=iTasT&pwd=Akshay@7995&senderid=ITAST&CountryCode=91" +
                                 $"&mobileno={phoneNumber}&msgtext={message}" +
                                 $"&pe_id=1701174522321104846&template_id=1707174737464547708";
                    var response = await client.GetAsync(url);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"SMS API Response: {responseContent}");

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while sending SMS: {ex.Message}");
                return false;
            }
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
}