namespace AshtavinayakAPP.Services.SmsService;

/// <summary>
/// Abstraction for sending transactional SMS messages via the BulkSMSPune gateway.
/// Inject this interface instead of constructing HttpClient directly in controllers or services.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an SMS message using a specific registered template.
    /// </summary>
    /// <param name="phoneNumber">10-digit mobile number (without country code prefix).</param>
    /// <param name="message">Plain-text message body matching the DLT template.</param>
    /// <param name="templateId">DLT-registered template ID for this message type.</param>
    /// <returns><c>true</c> if the gateway returned HTTP 2xx; <c>false</c> otherwise.</returns>
    Task<bool> SendAsync(string phoneNumber, string message, string templateId);
}
