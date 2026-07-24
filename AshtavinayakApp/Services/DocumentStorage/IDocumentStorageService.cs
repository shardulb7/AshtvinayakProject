using Microsoft.AspNetCore.Http;

namespace AshtavinayakAPP.Services.DocumentStorage
{
    /// <summary>
    /// Abstraction for saving/reading uploaded compliance documents (Aadhaar Card, Shop Act
    /// License, Udyam Certificate) to a storage location outside <c>wwwroot</c>, so they are
    /// never reachable via a public URL — only through an authenticated download action.
    /// </summary>
    public interface IDocumentStorageService
    {
        /// <summary>
        /// Validates and saves an uploaded file under the given subfolder.
        /// Returns the stored relative path on success, or a failure message.
        /// </summary>
        Task<(bool Success, string Message, string? RelativePath)> SaveAsync(IFormFile file, string subfolder);

        /// <summary>
        /// Opens a previously saved file for reading. Returns <c>null</c> if the file doesn't exist.
        /// </summary>
        Task<(Stream Stream, string ContentType, string FileName)?> OpenReadAsync(string relativePath);
    }
}
