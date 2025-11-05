// ============================================================================
// Maharashtra Government - Track Application Status API SDK
// Version: 1.0.0
// Compatible with: API V3 Specification (November 2025)
//
// Installation:
//   Copy this file to your project, or
//   Install-Package MaharashtraGov.TrackApplicationAPI (when available)
//
// Usage:
//   var client = new TrackApplicationClient(baseUrl, encryptKey, encryptIV, deptName);
//   var response = await client.GetApplicationStatusAsync("INC12345678", "4111");
//   Console.WriteLine(response.ServiceName);
//
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MaharashtraGov.TrackApplicationAPI
{
    #region Public API

    /// <summary>
    /// Main client for Track Application Status API
    /// </summary>
    public class TrackApplicationClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _encryptionKey;
        private readonly string _encryptionIV;
        private readonly string _departmentName;
        private readonly ClientConfiguration _config;

        /// <summary>
        /// Initialize Track Application API client
        /// </summary>
        /// <param name="apiBaseUrl">Base URL of the API (e.g., https://api.maharashtra.gov.in)</param>
        /// <param name="encryptionKey">TripleDES encryption key provided by API team</param>
        /// <param name="encryptionIV">TripleDES encryption IV provided by API team</param>
        /// <param name="departmentName">Your department name (e.g., "Revenue Department")</param>
        public TrackApplicationClient(
            string apiBaseUrl,
            string encryptionKey,
            string encryptionIV,
            string departmentName)
            : this(new ClientConfiguration
            {
                ApiBaseUrl = apiBaseUrl,
                EncryptionKey = encryptionKey,
                EncryptionIV = encryptionIV,
                DepartmentName = departmentName
            })
        {
        }

        /// <summary>
        /// Initialize Track Application API client with configuration
        /// </summary>
        public TrackApplicationClient(ClientConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(config.ApiBaseUrl))
                throw new ArgumentException("ApiBaseUrl is required", nameof(config));

            if (string.IsNullOrEmpty(config.EncryptionKey))
                throw new ArgumentException("EncryptionKey is required", nameof(config));

            if (string.IsNullOrEmpty(config.EncryptionIV))
                throw new ArgumentException("EncryptionIV is required", nameof(config));

            if (string.IsNullOrEmpty(config.DepartmentName))
                throw new ArgumentException("DepartmentName is required", nameof(config));

            _config = config;
            _encryptionKey = config.EncryptionKey;
            _encryptionIV = config.EncryptionIV;
            _departmentName = config.DepartmentName;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(config.ApiBaseUrl),
                Timeout = config.Timeout
            };

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TrackApplicationSDK/1.0");
        }

        /// <summary>
        /// Get application status by ID
        /// </summary>
        /// <param name="applicationId">Application ID (e.g., "INC12345678")</param>
        /// <param name="serviceId">Service ID (e.g., "4111")</param>
        /// <param name="language">Response language (EN or MR)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Application status details</returns>
        public async Task<ApplicationStatusResponse> GetApplicationStatusAsync(
            string applicationId,
            string serviceId,
            Language language = Language.English,
            CancellationToken cancellationToken = default)
        {
            var request = new ApplicationStatusRequest
            {
                AppID = applicationId,
                ServiceID = serviceId,
                DeptName = _departmentName,
                Language = language == Language.English ? "EN" : "MR"
            };

            return await GetApplicationStatusAsync(request, cancellationToken);
        }

        /// <summary>
        /// Get application status with full request object
        /// </summary>
        public async Task<ApplicationStatusResponse> GetApplicationStatusAsync(
            ApplicationStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate request
            var validation = ValidateRequest(request);
            if (!validation.IsValid)
            {
                throw new ValidationException(
                    "Request validation failed",
                    validation.Errors
                );
            }

            Log($"Requesting status for application: {request.AppID}");

            // Retry logic
            Exception lastException = null;

            for (int attempt = 0; attempt <= _config.MaxRetries; attempt++)
            {
                try
                {
                    // Serialize request to JSON
                    var requestJson = JsonConvert.SerializeObject(request, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Include
                    });

                    LogDebug($"Request JSON: {requestJson}");

                    // Encrypt request
                    var encryptedRequest = Encrypt(requestJson);
                    LogDebug("Request encrypted successfully");

                    // Create encrypted payload
                    var payload = new { data = encryptedRequest };
                    var payloadJson = JsonConvert.SerializeObject(payload);

                    // Send HTTP request
                    var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                    var httpResponse = await _httpClient.PostAsync(
                        _config.ApiEndpoint,
                        content,
                        cancellationToken
                    );

                    LogDebug($"HTTP response: {httpResponse.StatusCode}");

                    // Read response
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();

                    // Check for HTTP errors
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        throw new ApiException(
                            $"API returned error status {httpResponse.StatusCode}",
                            httpResponse.StatusCode,
                            responseContent
                        );
                    }

                    // Parse encrypted response
                    var encryptedResponse = JsonConvert.DeserializeObject<EncryptedResponse>(responseContent);

                    if (encryptedResponse?.data == null)
                    {
                        throw new ApiException(
                            "Response does not contain encrypted data",
                            httpResponse.StatusCode,
                            responseContent
                        );
                    }

                    // Decrypt response
                    var decryptedJson = Decrypt(encryptedResponse.data);
                    LogDebug($"Response decrypted: {decryptedJson}");

                    // Parse response
                    var response = JsonConvert.DeserializeObject<ApplicationStatusResponse>(decryptedJson);

                    if (response == null)
                    {
                        throw new ApiException(
                            "Failed to parse API response",
                            httpResponse.StatusCode,
                            decryptedJson
                        );
                    }

                    Log($"Status retrieved successfully for {request.AppID}");
                    return response;
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    LogError($"HTTP request failed (attempt {attempt + 1}): {ex.Message}");
                }
                catch (EncryptionException ex)
                {
                    lastException = ex;
                    LogError($"Encryption error: {ex.Message}");
                    throw; // Don't retry encryption errors
                }
                catch (DecryptionException ex)
                {
                    lastException = ex;
                    LogError($"Decryption error: {ex.Message}");
                    throw; // Don't retry decryption errors
                }
                catch (ValidationException)
                {
                    throw; // Don't retry validation errors
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    LogError($"Unexpected error (attempt {attempt + 1}): {ex.Message}");
                }

                // Wait before retry
                if (attempt < _config.MaxRetries)
                {
                    var delay = _config.RetryDelay * (attempt + 1);
                    Log($"Retrying in {delay.TotalSeconds} seconds...");
                    await Task.Delay(delay, cancellationToken);
                }
            }

            // All retries failed
            throw new TrackApplicationException(
                $"Request failed after {_config.MaxRetries + 1} attempts",
                lastException
            );
        }

        /// <summary>
        /// Validate request without sending
        /// </summary>
        public ValidationResult ValidateRequest(ApplicationStatusRequest request)
        {
            var result = new ValidationResult();

            if (request == null)
            {
                result.AddError("Request cannot be null");
                return result;
            }

            if (string.IsNullOrWhiteSpace(request.AppID))
                result.AddError("AppID is required");

            if (string.IsNullOrWhiteSpace(request.ServiceID))
                result.AddError("ServiceID is required");

            if (string.IsNullOrWhiteSpace(request.DeptName))
                result.AddError("DeptName is required");

            if (string.IsNullOrWhiteSpace(request.Language))
                result.AddError("Language is required");
            else if (request.Language != "EN" && request.Language != "MR")
                result.AddError("Language must be 'EN' or 'MR'");

            return result;
        }

        #region Encryption/Decryption (Matching their exact implementation)

        private string Encrypt(string plainText)
        {
            try
            {
                byte[] key = Encoding.UTF8.GetBytes(_encryptionKey);
                byte[] iv = Encoding.UTF8.GetBytes(_encryptionIV);
                byte[] data = Encoding.UTF8.GetBytes(plainText);

                using (TripleDES tdes = TripleDES.Create())
                {
                    tdes.IV = iv;
                    tdes.Key = key;
                    tdes.Mode = CipherMode.CBC;
                    tdes.Padding = PaddingMode.Zeros;

                    using (ICryptoTransform encryptor = tdes.CreateEncryptor())
                    {
                        byte[] encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
                        return ByteArrayToHexString(encrypted);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new EncryptionException("Failed to encrypt request data", ex);
            }
        }

        private string Decrypt(string cipherText)
        {
            try
            {
                byte[] key = Encoding.UTF8.GetBytes(_encryptionKey);
                byte[] iv = Encoding.UTF8.GetBytes(_encryptionIV);
                byte[] data = HexStringToByteArray(cipherText);

                using (TripleDES tdes = TripleDES.Create())
                {
                    tdes.IV = iv;
                    tdes.Key = key;
                    tdes.Mode = CipherMode.CBC;
                    tdes.Padding = PaddingMode.Zeros;

                    using (ICryptoTransform decryptor = tdes.CreateDecryptor())
                    {
                        byte[] decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);
                        return Encoding.UTF8.GetString(decrypted).TrimEnd('\0');
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DecryptionException("Failed to decrypt response data", ex);
            }
        }

        private string ByteArrayToHexString(byte[] bytes)
        {
            string hex = BitConverter.ToString(bytes);
            return hex.Replace("-", "");
        }

        private byte[] HexStringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_config.EnableLogging)
            {
                Console.WriteLine($"[TrackApplicationSDK] {message}");
            }
        }

        private void LogDebug(string message)
        {
            if (_config.EnableLogging && _config.LogLevel == LogLevel.Debug)
            {
                Console.WriteLine($"[TrackApplicationSDK][DEBUG] {message}");
            }
        }

        private void LogError(string message)
        {
            if (_config.EnableLogging)
            {
                Console.Error.WriteLine($"[TrackApplicationSDK][ERROR] {message}");
            }
        }

        #endregion

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Configuration for Track Application API client
    /// </summary>
    public class ClientConfiguration
    {
        /// <summary>
        /// API base URL (required)
        /// </summary>
        public string ApiBaseUrl { get; set; }

        /// <summary>
        /// API endpoint path (default: /api/SampleAPI/sendappstatus_encrypted)
        /// </summary>
        public string ApiEndpoint { get; set; } = "/api/SampleAPI/sendappstatus_encrypted";

        /// <summary>
        /// TripleDES encryption key (required)
        /// </summary>
        public string EncryptionKey { get; set; }

        /// <summary>
        /// TripleDES encryption IV (required)
        /// </summary>
        public string EncryptionIV { get; set; }

        /// <summary>
        /// Department name (required)
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// Request timeout (default: 30 seconds)
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum retry attempts (default: 3)
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Delay between retries (default: 2 seconds)
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Enable logging (default: false)
        /// </summary>
        public bool EnableLogging { get; set; } = false;

        /// <summary>
        /// Log level (default: Info)
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Info;
    }

    /// <summary>
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        Info,
        Debug
    }

    #endregion

    #region Request/Response Models (Matching V3 Specification)

    /// <summary>
    /// Request to get application status
    /// </summary>
    public class ApplicationStatusRequest
    {
        /// <summary>
        /// Application ID (e.g., "INC12345678")
        /// </summary>
        [JsonProperty("AppID")]
        public string AppID { get; set; }

        /// <summary>
        /// Service ID (e.g., "4111")
        /// </summary>
        [JsonProperty("ServiceID")]
        public string ServiceID { get; set; }

        /// <summary>
        /// Department name (e.g., "Revenue Department")
        /// </summary>
        [JsonProperty("DeptName")]
        public string DeptName { get; set; }

        /// <summary>
        /// Language: "EN" (English) or "MR" (Marathi)
        /// </summary>
        [JsonProperty("Language")]
        public string Language { get; set; }
    }

    /// <summary>
    /// Application status response matching V3 specification
    /// </summary>
    public class ApplicationStatusResponse
    {
        /// <summary>
        /// Application ID
        /// </summary>
        [JsonProperty("ApplicationID")]
        public string ApplicationID { get; set; }

        /// <summary>
        /// Service name (e.g., "Income Certificate" or "उत्पन्नाचा दाखला")
        /// </summary>
        [JsonProperty("ServiceName")]
        public string ServiceName { get; set; }

        /// <summary>
        /// Applicant name
        /// </summary>
        [JsonProperty("ApplicantName")]
        public string ApplicantName { get; set; }

        /// <summary>
        /// Estimated days to disburse
        /// </summary>
        [JsonProperty("EstimatedDisbursalDays")]
        public int EstimatedDisbursalDays { get; set; }

        /// <summary>
        /// Application submission date/time
        /// Format: DD-MMM-YYYY,hh:mm:ss (24hr)
        /// Empty string if not available
        /// </summary>
        [JsonProperty("ApplicationSubmissionDate")]
        public string ApplicationSubmissionDate { get; set; }

        /// <summary>
        /// Application payment date/time
        /// Format: DD-MMM-YYYY,hh:mm:ss (24hr)
        /// Empty string if not paid
        /// </summary>
        [JsonProperty("ApplicationPaymentDate")]
        public string ApplicationPaymentDate { get; set; }

        /// <summary>
        /// Next action required details
        /// Empty string if no action required
        /// </summary>
        [JsonProperty("NextActionRequiredDetails")]
        public string NextActionRequiredDetails { get; set; }

        /// <summary>
        /// Final decision: "0" = Approved, "1" = Rejected, "2" = Pending
        /// Empty string if not decided
        /// </summary>
        [JsonProperty("FinalDecision")]
        public string FinalDecision { get; set; }

        /// <summary>
        /// Department redirection URL (optional)
        /// </summary>
        [JsonProperty("DepartmentRedirectionURL")]
        public string DepartmentRedirectionURL { get; set; }

        /// <summary>
        /// Total number of desks in workflow
        /// </summary>
        [JsonProperty("TotalNumberOfDesks")]
        public int TotalNumberOfDesks { get; set; }

        /// <summary>
        /// Current desk number (0 if not assigned)
        /// </summary>
        [JsonProperty("CurrentDeskNumber")]
        public int CurrentDeskNumber { get; set; }

        /// <summary>
        /// Next desk number (0 if final)
        /// </summary>
        [JsonProperty("NextDeskNumber")]
        public int NextDeskNumber { get; set; }

        /// <summary>
        /// Desk review details (in ascending order)
        /// </summary>
        [JsonProperty("DeskDetails")]
        public DeskDetail[] DeskDetails { get; set; }

        #region Helper Properties

        /// <summary>
        /// Check if application is paid
        /// </summary>
        [JsonIgnore]
        public bool IsPaid => !string.IsNullOrEmpty(ApplicationPaymentDate);

        /// <summary>
        /// Check if next action is required
        /// </summary>
        [JsonIgnore]
        public bool IsActionRequired => !string.IsNullOrEmpty(NextActionRequiredDetails);

        /// <summary>
        /// Check if final decision is made
        /// </summary>
        [JsonIgnore]
        public bool IsFinalDecisionMade => !string.IsNullOrEmpty(FinalDecision);

        /// <summary>
        /// Get final decision as enum
        /// </summary>
        [JsonIgnore]
        public FinalDecisionStatus? FinalDecisionStatus
        {
            get
            {
                if (string.IsNullOrEmpty(FinalDecision))
                    return null;

                switch (FinalDecision)
                {
                    case "0": return TrackApplicationAPI.FinalDecisionStatus.Approved;
                    case "1": return TrackApplicationAPI.FinalDecisionStatus.Rejected;
                    case "2": return TrackApplicationAPI.FinalDecisionStatus.Pending;
                    default: return null;
                }
            }
        }

        /// <summary>
        /// Calculate progress percentage
        /// </summary>
        [JsonIgnore]
        public int ProgressPercentage
        {
            get
            {
                if (TotalNumberOfDesks <= 0)
                    return 0;

                int completedDesks = DeskDetails?.Length ?? 0;
                return (int)Math.Round((double)completedDesks / TotalNumberOfDesks * 100);
            }
        }

        #endregion
    }

    /// <summary>
    /// Desk review details
    /// </summary>
    public class DeskDetail
    {
        /// <summary>
        /// Desk number (e.g., "Desk 1")
        /// </summary>
        [JsonProperty("DeskNumber")]
        public string DeskNumber { get; set; }

        /// <summary>
        /// Reviewer name/designation
        /// Empty string if not available
        /// </summary>
        [JsonProperty("ReviewActionBy")]
        public string ReviewActionBy { get; set; }

        /// <summary>
        /// Review date/time
        /// Format: DD-MMM-YYYY,hh:mm:ss (24hr)
        /// Empty string if not reviewed
        /// </summary>
        [JsonProperty("ReviewActionDateTime")]
        public string ReviewActionDateTime { get; set; }

        /// <summary>
        /// Review comments/details
        /// Empty string if no comments
        /// </summary>
        [JsonProperty("ReviewActionDetails")]
        public string ReviewActionDetails { get; set; }
    }

    /// <summary>
    /// Encrypted response wrapper
    /// </summary>
    internal class EncryptedResponse
    {
        [JsonProperty("data")]
        public string data { get; set; }
    }

    #endregion

    #region Enums

    /// <summary>
    /// Response language
    /// </summary>
    public enum Language
    {
        English,
        Marathi
    }

    /// <summary>
    /// Final decision status
    /// </summary>
    public enum FinalDecisionStatus
    {
        Approved = 0,
        Rejected = 1,
        Pending = 2
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        private List<string> _errors = new List<string>();

        public bool IsValid => _errors.Count == 0;
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        internal void AddError(string error)
        {
            _errors.Add(error);
        }
    }

    #endregion

    #region Exceptions

    /// <summary>
    /// Base exception for Track Application API
    /// </summary>
    public class TrackApplicationException : Exception
    {
        public TrackApplicationException(string message) : base(message) { }
        public TrackApplicationException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// Request validation exception
    /// </summary>
    public class ValidationException : TrackApplicationException
    {
        public IReadOnlyList<string> ValidationErrors { get; }

        public ValidationException(string message, IReadOnlyList<string> errors)
            : base(message)
        {
            ValidationErrors = errors;
        }
    }

    /// <summary>
    /// Encryption exception
    /// </summary>
    public class EncryptionException : TrackApplicationException
    {
        public EncryptionException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// Decryption exception
    /// </summary>
    public class DecryptionException : TrackApplicationException
    {
        public DecryptionException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// API error exception
    /// </summary>
    public class ApiException : TrackApplicationException
    {
        public System.Net.HttpStatusCode StatusCode { get; }
        public string ResponseContent { get; }

        public ApiException(string message, System.Net.HttpStatusCode statusCode, string responseContent)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }

    #endregion

    #region Helper Utilities

    /// <summary>
    /// Helper utilities for working with application status
    /// </summary>
    public static class StatusHelper
    {
        /// <summary>
        /// Get user-friendly status text for final decision
        /// </summary>
        public static string GetFinalDecisionText(string finalDecision, Language language = Language.English)
        {
            if (string.IsNullOrEmpty(finalDecision))
                return language == Language.English ? "Pending" : "प्रलंबित";

            switch (finalDecision)
            {
                case "0":
                    return language == Language.English ? "Approved" : "मंजूर";
                case "1":
                    return language == Language.English ? "Rejected" : "नाकारले";
                case "2":
                    return language == Language.English ? "Pending" : "प्रलंबित";
                default:
                    return language == Language.English ? "Unknown" : "अज्ञात";
            }
        }

        /// <summary>
        /// Parse date string to DateTime
        /// </summary>
        public static DateTime? ParseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            // Format: DD-MMM-YYYY,hh:mm:ss
            try
            {
                return DateTime.ParseExact(
                    dateString,
                    "dd-MMM-yyyy,HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture
                );
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Format DateTime to API date string
        /// </summary>
        public static string FormatDate(DateTime dateTime)
        {
            return dateTime.ToString("dd-MMM-yyyy,HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    #endregion
}
