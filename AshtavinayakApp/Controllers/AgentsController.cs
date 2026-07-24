using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AshtavinayakAPP.Services.AgentSrc;
using AshtavinayakAPP.Services.DocumentStorage;

namespace AshtavinayakAPP.Controllers
{
    public class AgentsController : Controller
    {
        private readonly IAgentService _agentService;
        private readonly IDocumentStorageService _documentStorageService;

        public AgentsController(IAgentService agentService, IDocumentStorageService documentStorageService)
        {
            _agentService = agentService;
            _documentStorageService = documentStorageService;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userSession = context.HttpContext.Session.GetString("User");
            if (string.IsNullOrEmpty(userSession))
            {
                context.Result = new RedirectToActionResult("Login", "Home", null);
            }
            base.OnActionExecuting(context);
        }

        // GET: Agents
        public async Task<IActionResult> Index(int page = 1, string? status = null)
        {
            const int pageSize = 10;
            var (agents, totalCount, totalPages) = await _agentService.GetAgentsAsync(status, page, pageSize);

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            ViewData["StatusFilter"] = status;

            return View(agents);
        }

        // GET: Agents/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var agent = await _agentService.GetByIdAsync(id);
            if (agent == null)
                return NotFound();

            return View(agent);
        }

        // GET: Agents/Edit/5 — set/clear the per-agent commission override
        public async Task<IActionResult> Edit(int id)
        {
            var agent = await _agentService.GetByIdAsync(id);
            if (agent == null)
                return NotFound();

            return View(agent);
        }

        // POST: Agents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, decimal? commissionPercentage)
        {
            var (success, message) = await _agentService.UpdateCommissionOverrideAsync(id, commissionPercentage);
            if (!success)
                return NotFound();

            TempData["Message"] = message;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Agents/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            await _agentService.ApproveAsync(id);
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Agents/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? remarks)
        {
            await _agentService.RejectAsync(id, remarks);
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Agents/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var agent = await _agentService.GetByIdAsync(id);
            if (agent == null)
                return NotFound();

            await _agentService.SetActiveAsync(id, !agent.IsActive);
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Agents/DownloadDocument/5?documentType=aadhaar
        public async Task<IActionResult> DownloadDocument(int agentId, string documentType)
        {
            var agent = await _agentService.GetByIdAsync(agentId);
            if (agent == null)
                return NotFound();

            // Fixed allowlist — documentType must never be used to build a filesystem path directly.
            var relativePath = documentType?.ToLowerInvariant() switch
            {
                "aadhaar" => agent.AadhaarDocumentPath,
                "shopact" => agent.ShopActLicenseDocumentPath,
                "udyam" => agent.UdyamCertificatePath,
                _ => null
            };

            if (relativePath == null)
                return NotFound();

            var result = await _documentStorageService.OpenReadAsync(relativePath);
            if (result == null)
                return NotFound();

            var (stream, contentType, fileName) = result.Value;
            return File(stream, contentType, fileName);
        }
    }
}
