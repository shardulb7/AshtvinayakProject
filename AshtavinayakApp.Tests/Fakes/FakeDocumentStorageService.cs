using AshtavinayakAPP.Services.DocumentStorage;
using Microsoft.AspNetCore.Http;

namespace AshtavinayakApp.Tests.Fakes;

// Not exercised by the AgentService tests that need this dependency injected but never call it
// (e.g. commission resolution doesn't touch document storage) — throws if actually invoked.
public class FakeDocumentStorageService : IDocumentStorageService
{
    public Task<(bool Success, string Message, string? RelativePath)> SaveAsync(IFormFile file, string subfolder)
        => throw new NotImplementedException();

    public Task<(Stream Stream, string ContentType, string FileName)?> OpenReadAsync(string relativePath)
        => throw new NotImplementedException();
}
