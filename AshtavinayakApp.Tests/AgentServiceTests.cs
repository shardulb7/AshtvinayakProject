using AshtavinayakAPP.Models;
using AshtavinayakAPP.Services.AgentSrc;
using AshtavinayakApp.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AshtavinayakApp.Tests;

public class AgentServiceTests
{
    private static (AshtvinayakTravelContext Context, AgentService Service) MakeService()
    {
        var options = new DbContextOptionsBuilder<AshtvinayakTravelContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AshtvinayakTravelContext(options);
        var config = new ConfigurationBuilder().Build();
        var service = new AgentService(context, config, new FakeDocumentStorageService(), NullLogger<AgentService>.Instance);
        return (context, service);
    }

    [Fact]
    public async Task GetEffectiveCommissionAsync_UsesPerAgentOverride_WhenSet()
    {
        var (context, service) = MakeService();
        context.CommissionSettings.Add(new CommissionSetting { DefaultCommissionPercentage = 10, UpdatedAt = DateTime.UtcNow });
        var agent = new Agent
        {
            FullName = "Test Agent", BusinessName = "Test Travels", MobileNumber = "9000000001",
            Email = "agent@example.com", Address = "Addr", PasswordHash = "hash",
            AadhaarDocumentPath = "x", ShopActLicenseDocumentPath = "x", UdyamCertificatePath = "x",
            ApprovalStatus = "Approved", IsActive = true, CommissionPercentage = 15
        };
        context.Agents.Add(agent);
        await context.SaveChangesAsync();

        var effective = await service.GetEffectiveCommissionAsync(agent.AgentId);

        Assert.Equal(15m, effective);
    }

    [Fact]
    public async Task GetEffectiveCommissionAsync_FallsBackToDefault_WhenNoOverride()
    {
        var (context, service) = MakeService();
        context.CommissionSettings.Add(new CommissionSetting { DefaultCommissionPercentage = 10, UpdatedAt = DateTime.UtcNow });
        var agent = new Agent
        {
            FullName = "Test Agent 2", BusinessName = "Test Travels 2", MobileNumber = "9000000002",
            Email = "agent2@example.com", Address = "Addr", PasswordHash = "hash",
            AadhaarDocumentPath = "x", ShopActLicenseDocumentPath = "x", UdyamCertificatePath = "x",
            ApprovalStatus = "Approved", IsActive = true, CommissionPercentage = null
        };
        context.Agents.Add(agent);
        await context.SaveChangesAsync();

        var effective = await service.GetEffectiveCommissionAsync(agent.AgentId);

        Assert.Equal(10m, effective);
    }
}
