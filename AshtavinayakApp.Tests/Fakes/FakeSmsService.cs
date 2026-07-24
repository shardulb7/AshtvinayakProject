using AshtavinayakAPP.Services.SmsService;

namespace AshtavinayakApp.Tests.Fakes;

// Minimal stand-in for ISmsService — tests care about booking/commission logic, not SMS delivery.
public class FakeSmsService : ISmsService
{
    public Task<bool> SendAsync(string phoneNumber, string message, string templateId) => Task.FromResult(true);
}
