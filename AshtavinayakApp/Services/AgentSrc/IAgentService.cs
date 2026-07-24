using AshtavinayakAPP.Models;

namespace AshtavinayakAPP.Services.AgentSrc
{
    public interface IAgentService
    {
        Task<(bool Success, string Message, object? Data)> RegisterAsync(AgentRegisterDto request);
        Task<(bool Success, string Message, object? Data)> LoginAsync(AgentLoginDto request);
        Task<(bool Success, string Message, int? UserId, string? CustomerName)> ResolveCustomerAsync(ResolveCustomerDto request);

        Task<(List<Agent> Agents, int TotalCount, int TotalPages)> GetAgentsAsync(string? status, int page, int pageSize);
        Task<Agent?> GetByIdAsync(int agentId);
        Task<(bool Success, string Message)> ApproveAsync(int agentId);
        Task<(bool Success, string Message)> RejectAsync(int agentId, string? remarks);
        Task<(bool Success, string Message)> SetActiveAsync(int agentId, bool isActive);
        Task<(bool Success, string Message)> UpdateCommissionOverrideAsync(int agentId, decimal? commissionPercentage);

        Task<decimal> GetEffectiveCommissionAsync(int agentId);
    }
}
