namespace AshtavinayakAPP.Services.SmsService;

/// <summary>
/// Sends transactional SMS messages via the BulkSMSPune HTTP gateway.
///
/// All credentials are read from <c>IConfiguration</c> — never hard-coded.
/// Required config keys (set via environment variables in Production):
///   SmsGateway__BaseUrl, SmsGateway__User, SmsGateway__Password,
///   SmsGateway__SenderId, SmsGateway__PeId
/// </summary>
public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    private readonly string _baseUrl;
    private readonly string _user;
    private readonly string _password;
    private readonly string _senderId;
    private readonly string _peId;
    private readonly IHttpClientFactory _httpClientFactory;

    public SmsService(
        IConfiguration configuration,
        ILogger<SmsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger          = logger;
        _httpClientFactory = httpClientFactory;

        _baseUrl  = configuration["SmsGateway:BaseUrl"]  ?? "https://bulksmspune.mobi/sendurlcomma.aspx"; // MED-09: use HTTPS
        _user     = configuration["SmsGateway:User"]     ?? string.Empty;
        _password = configuration["SmsGateway:Password"] ?? string.Empty;
        _senderId = configuration["SmsGateway:SenderId"] ?? "ITAST";
        _peId     = configuration["SmsGateway:PeId"]     ?? string.Empty;
    }

    /// <inheritdoc/>
    public async Task<bool> SendAsync(string phoneNumber, string message, string templateId)
    {
        if (string.IsNullOrWhiteSpace(_user) || string.IsNullOrWhiteSpace(_password))
        {
            _logger.LogWarning("[SmsService] SMS credentials are not configured. Skipping SMS to {Phone}.", phoneNumber);
            return false;
        }

        try
        {
            string encodedMessage = Uri.EscapeDataString(message);
            string url = $"{_baseUrl}?user={_user}&pwd={_password}&senderid={_senderId}" +
                         $"&CountryCode=91&mobileno={phoneNumber}&msgtext={encodedMessage}" +
                         $"&smstype=9&pe_id={_peId}&template_id={templateId}";

            var client = _httpClientFactory.CreateClient("SmsGateway");
            var response = await client.GetAsync(url);
            string responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("[SmsService] SMS to {Phone} — Status: {Status}, Response: {Response}",
                phoneNumber, response.StatusCode, responseContent);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SmsService] Exception while sending SMS to {Phone}.", phoneNumber);
            return false;
        }
    }
}
