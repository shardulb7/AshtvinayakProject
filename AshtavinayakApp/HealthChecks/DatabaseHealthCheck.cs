using AshtavinayakAPP.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AshtavinayakAPP.HealthChecks
{
    // Readiness check — confirms the app can actually reach the database, not just that
    // the process is alive. Kept as a small custom check rather than pulling in an extra
    // NuGet package for something this simple.
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly AshtvinayakTravelContext _context;

        public DatabaseHealthCheck(AshtvinayakTravelContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                return canConnect
                    ? HealthCheckResult.Healthy("Database connection is healthy.")
                    : HealthCheckResult.Unhealthy("Cannot connect to the database.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database health check threw an exception.", ex);
            }
        }
    }
}
