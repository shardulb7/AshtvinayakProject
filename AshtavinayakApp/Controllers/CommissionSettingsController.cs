using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using AshtavinayakAPP.Models;

namespace AshtavinayakAPP.Controllers
{
    // Single-row global setting — the default commission percentage applied to agents
    // that don't have a per-agent override. Kept separate from AgentsController since
    // it's a global setting, not a per-agent action.
    public class CommissionSettingsController : Controller
    {
        private readonly AshtvinayakTravelContext _context;

        public CommissionSettingsController(AshtvinayakTravelContext context)
        {
            _context = context;
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

        public async Task<IActionResult> Index()
        {
            var setting = await _context.CommissionSettings.FirstOrDefaultAsync();
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(decimal defaultCommissionPercentage)
        {
            var setting = await _context.CommissionSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new CommissionSetting
                {
                    DefaultCommissionPercentage = defaultCommissionPercentage,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CommissionSettings.Add(setting);
            }
            else
            {
                setting.DefaultCommissionPercentage = defaultCommissionPercentage;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Default commission percentage updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
