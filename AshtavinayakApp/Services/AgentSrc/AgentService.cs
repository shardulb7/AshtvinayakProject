using AshtavinayakAPP.Models;
using AshtavinayakAPP.Services.DocumentStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AshtavinayakAPP.Services.AgentSrc
{
    public class AgentService : IAgentService
    {
        private readonly AshtvinayakTravelContext _context;
        private readonly IConfiguration _configuration;
        private readonly IDocumentStorageService _documentStorageService;
        private readonly ILogger<AgentService> _logger;

        public AgentService(
            AshtvinayakTravelContext context,
            IConfiguration configuration,
            IDocumentStorageService documentStorageService,
            ILogger<AgentService> logger)
        {
            _context = context;
            _configuration = configuration;
            _documentStorageService = documentStorageService;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, object? Data)> RegisterAsync(AgentRegisterDto request)
        {
            if (await _context.Agents.AnyAsync(a => a.MobileNumber == request.MobileNumber && !a.IsDeleted))
                return (false, "This mobile number is already registered.", null);

            if (await _context.Agents.AnyAsync(a => a.Email == request.Email && !a.IsDeleted))
                return (false, "This email is already registered.", null);

            var (aadhaarOk, aadhaarMsg, aadhaarPath) = await _documentStorageService.SaveAsync(request.AadhaarDocument, "aadhaar");
            if (!aadhaarOk)
                return (false, $"Aadhaar Card: {aadhaarMsg}", null);

            var (shopActOk, shopActMsg, shopActPath) = await _documentStorageService.SaveAsync(request.ShopActLicense, "shopact");
            if (!shopActOk)
                return (false, $"Shop Act License: {shopActMsg}", null);

            var (udyamOk, udyamMsg, udyamPath) = await _documentStorageService.SaveAsync(request.UdyamCertificate, "udyam");
            if (!udyamOk)
                return (false, $"Udyam Registration Certificate: {udyamMsg}", null);

            var agent = new Agent
            {
                FullName = request.FullName,
                BusinessName = request.BusinessName,
                MobileNumber = request.MobileNumber,
                Email = request.Email,
                Address = request.Address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                AadhaarDocumentPath = aadhaarPath!,
                ShopActLicenseDocumentPath = shopActPath!,
                UdyamCertificatePath = udyamPath!,
                ApprovalStatus = "Pending",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Agents.Add(agent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[AgentService] New agent registered, AgentId={AgentId}, pending approval.", agent.AgentId);

            return (true, "Registration submitted. Your account is pending admin approval.", new { agent.AgentId });
        }

        public async Task<(bool Success, string Message, object? Data)> LoginAsync(AgentLoginDto request)
        {
            var agent = await _context.Agents.FirstOrDefaultAsync(a =>
                !a.IsDeleted && (a.MobileNumber == request.MobileOrEmail || a.Email == request.MobileOrEmail));

            if (agent == null || !BCrypt.Net.BCrypt.Verify(request.Password, agent.PasswordHash))
            {
                _logger.LogWarning("[AgentService] Failed agent login attempt for {MobileOrEmail}.", request.MobileOrEmail);
                return (false, "Invalid credentials.", null);
            }

            if (agent.ApprovalStatus == "Pending")
                return (false, "Your registration is still pending admin approval.", null);

            if (agent.ApprovalStatus == "Rejected")
                return (false, $"Your registration was rejected.{(string.IsNullOrWhiteSpace(agent.RejectionRemarks) ? "" : " Reason: " + agent.RejectionRemarks)}", null);

            if (!agent.IsActive)
                return (false, "Your account is currently inactive. Please contact the administrator.", null);

            var token = GenerateJwtToken(agent);

            return (true, "Login successful.", new
            {
                Token = token,
                Agent = new
                {
                    agent.AgentId,
                    agent.FullName,
                    agent.BusinessName,
                    agent.Email,
                    agent.MobileNumber
                }
            });
        }

        public async Task<(bool Success, string Message, int? UserId, string? CustomerName)> ResolveCustomerAsync(ResolveCustomerDto request)
        {
            var existing = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.CustomerPhone && !u.IsDeleted);
            if (existing != null)
                return (true, "Existing customer found.", existing.UserId, existing.UserName);

            try
            {
                var placeholderEmail = string.IsNullOrWhiteSpace(request.CustomerEmail)
                    ? $"{request.CustomerPhone}@no-email.ashtavinayak.local"
                    : request.CustomerEmail;

                var user = new User
                {
                    UserName = request.CustomerName,
                    PhoneNumber = request.CustomerPhone,
                    Email = placeholderEmail,
                    // Unusable random hash — this customer was registered by an agent in person
                    // and never sets a password; they'd use the normal OTP flow if they ever log in directly.
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    Role = "Customer"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return (true, "Customer created.", user.UserId, user.UserName);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "[AgentService] Failed to create customer for phone {Phone}.", request.CustomerPhone);
                return (false, "Unable to resolve this customer — the phone or email may already be in use with different details.", null, null);
            }
        }

        public async Task<(List<Agent> Agents, int TotalCount, int TotalPages)> GetAgentsAsync(string? status, int page, int pageSize)
        {
            var query = _context.Agents.Where(a => !a.IsDeleted);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.ApprovalStatus == status);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var agents = await query
                .OrderByDescending(a => a.AgentId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (agents, totalCount, totalPages);
        }

        public async Task<Agent?> GetByIdAsync(int agentId)
        {
            return await _context.Agents.FirstOrDefaultAsync(a => a.AgentId == agentId && !a.IsDeleted);
        }

        public async Task<(bool Success, string Message)> ApproveAsync(int agentId)
        {
            var agent = await GetByIdAsync(agentId);
            if (agent == null)
                return (false, "Agent not found.");

            agent.ApprovalStatus = "Approved";
            agent.IsActive = true;
            agent.RejectionRemarks = null;
            await _context.SaveChangesAsync();

            return (true, "Agent approved.");
        }

        public async Task<(bool Success, string Message)> RejectAsync(int agentId, string? remarks)
        {
            var agent = await GetByIdAsync(agentId);
            if (agent == null)
                return (false, "Agent not found.");

            agent.ApprovalStatus = "Rejected";
            agent.IsActive = false;
            agent.RejectionRemarks = remarks;
            await _context.SaveChangesAsync();

            return (true, "Agent rejected.");
        }

        public async Task<(bool Success, string Message)> SetActiveAsync(int agentId, bool isActive)
        {
            var agent = await GetByIdAsync(agentId);
            if (agent == null)
                return (false, "Agent not found.");

            agent.IsActive = isActive;
            await _context.SaveChangesAsync();

            return (true, isActive ? "Agent activated." : "Agent deactivated.");
        }

        public async Task<(bool Success, string Message)> UpdateCommissionOverrideAsync(int agentId, decimal? commissionPercentage)
        {
            var agent = await GetByIdAsync(agentId);
            if (agent == null)
                return (false, "Agent not found.");

            agent.CommissionPercentage = commissionPercentage;
            await _context.SaveChangesAsync();

            return (true, "Commission updated.");
        }

        public async Task<decimal> GetEffectiveCommissionAsync(int agentId)
        {
            var agent = await GetByIdAsync(agentId);
            if (agent?.CommissionPercentage != null)
                return agent.CommissionPercentage.Value;

            var setting = await _context.CommissionSettings.FirstOrDefaultAsync();
            return setting?.DefaultCommissionPercentage ?? 0;
        }

        private string GenerateJwtToken(Agent agent)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, agent.AgentId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, agent.Email),
                new Claim(ClaimTypes.Role, "Agent"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("MobileNumber", agent.MobileNumber)
            };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
