using AshtavinayakAPP.Services.DocumentStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AshtavinayakApp.Tests;

public class DocumentStorageServiceTests : IDisposable
{
    private readonly string _rootPath;
    private readonly DocumentStorageService _service;

    public DocumentStorageServiceTests()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "AshtavinayakDocsTests_" + Guid.NewGuid().ToString("N"));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DocumentStorage:RootPath"] = _rootPath })
            .Build();
        _service = new DocumentStorageService(config, NullLogger<DocumentStorageService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
            Directory.Delete(_rootPath, recursive: true);
    }

    private static IFormFile MakeFormFile(byte[] content, string fileName, string contentType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static readonly byte[] ValidPdfBytes = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4
    private static readonly byte[] ValidJpgBytes = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
    private static readonly byte[] ValidPngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    [Fact]
    public async Task SaveAsync_AcceptsValidPdf()
    {
        var file = MakeFormFile(ValidPdfBytes, "aadhaar.pdf", "application/pdf");
        var (success, _, relativePath) = await _service.SaveAsync(file, "aadhaar");
        Assert.True(success);
        Assert.NotNull(relativePath);
        Assert.True(File.Exists(Path.Combine(_rootPath, relativePath!)));
    }

    [Fact]
    public async Task SaveAsync_AcceptsValidJpgAndPng()
    {
        var jpg = MakeFormFile(ValidJpgBytes, "license.jpg", "image/jpeg");
        var (jpgSuccess, _, _) = await _service.SaveAsync(jpg, "shopact");
        Assert.True(jpgSuccess);

        var png = MakeFormFile(ValidPngBytes, "cert.png", "image/png");
        var (pngSuccess, _, _) = await _service.SaveAsync(png, "udyam");
        Assert.True(pngSuccess);
    }

    [Fact]
    public async Task SaveAsync_RejectsDisallowedExtension()
    {
        var file = MakeFormFile(new byte[] { 1, 2, 3 }, "malware.exe", "application/octet-stream");
        var (success, message, relativePath) = await _service.SaveAsync(file, "aadhaar");
        Assert.False(success);
        Assert.Null(relativePath);
        Assert.Contains("PDF, JPG, and PNG", message);
    }

    [Fact]
    public async Task SaveAsync_RejectsMagicByteMismatch()
    {
        // .pdf extension and application/pdf content-type, but the actual bytes are a PNG —
        // this is exactly the spoofing scenario the signature check exists to catch.
        var file = MakeFormFile(ValidPngBytes, "fake.pdf", "application/pdf");
        var (success, message, relativePath) = await _service.SaveAsync(file, "aadhaar");
        Assert.False(success);
        Assert.Null(relativePath);
        Assert.Contains("does not match", message);
    }

    [Fact]
    public async Task SaveAsync_RejectsOversizedFile()
    {
        var oversized = new byte[6 * 1024 * 1024]; // 6MB > 5MB limit
        Array.Copy(ValidPdfBytes, oversized, ValidPdfBytes.Length);
        var file = MakeFormFile(oversized, "big.pdf", "application/pdf");
        var (success, message, _) = await _service.SaveAsync(file, "aadhaar");
        Assert.False(success);
        Assert.Contains("5MB", message);
    }

    [Fact]
    public async Task SaveAsync_RejectsContentTypeMismatchWithExtension()
    {
        // .pdf extension but an image content-type header — rejected before the byte check even runs.
        var file = MakeFormFile(ValidPdfBytes, "doc.pdf", "image/png");
        var (success, _, _) = await _service.SaveAsync(file, "aadhaar");
        Assert.False(success);
    }

    [Fact]
    public async Task OpenReadAsync_RejectsPathTraversal()
    {
        var result = await _service.OpenReadAsync("..\\..\\Program.cs");
        Assert.Null(result);
    }
}
