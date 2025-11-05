// ============================================================================
// Maharashtra Government - Department Track Application API SDK (Server-Side)
// Version: 1.0.0
// Compatible with: API V3 Specification (November 2025)
//
// This SDK helps departments quickly build their Track Application API
// that receives requests from Aaple Sarkar Portal.
//
// Installation:
//   Copy this file to your ASP.NET Web API project
//
// Usage:
//   1. Implement IDepartmentDataProvider interface
//   2. Create DepartmentApiHandler with your data provider
//   3. Call ProcessRequestAsync() from your Web API controller
//
// Example:
//   var handler = new DepartmentApiHandler(config, dataProvider);
//   var response = await handler.ProcessRequestAsync(encryptedRequestBody);
//   return Ok(response);
//
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MaharashtraGov.DepartmentAPI
{
    #region Public API

    /// <summary>
    /// Main handler for Department Track Application API
    /// Processes encrypted requests from Aaple Sarkar and returns encrypted responses
    /// </summary>
    public class DepartmentApiHandler
    {
        private readonly DepartmentConfiguration _config;
        private readonly IDepartmentDataProvider _dataProvider;

        /// <summary>
        /// Initialize Department API handler with simple configuration
        /// </summary>
        /// <param name="encryptionKey">TripleDES encryption key (24 characters)</param>
        /// <param name="encryptionIV">TripleDES encryption IV (8 characters)</param>
        /// <param name="dataProvider">Your implementation of data provider</param>
        public DepartmentApiHandler(
            string encryptionKey,
            string encryptionIV,
            IDepartmentDataProvider dataProvider)
            : this(new DepartmentConfiguration
            {
                EncryptionKey = encryptionKey,
                EncryptionIV = encryptionIV
            }, dataProvider)
        {
        }

        /// <summary>
        /// Initialize Department API handler with full configuration
        /// </summary>
        public DepartmentApiHandler(
            DepartmentConfiguration config,
            IDepartmentDataProvider dataProvider)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (dataProvider == null)
                throw new ArgumentNullException(nameof(dataProvider));

            if (string.IsNullOrEmpty(config.EncryptionKey))
                throw new ArgumentException("EncryptionKey is required", nameof(config));

            if (string.IsNullOrEmpty(config.EncryptionIV))
                throw new ArgumentException("EncryptionIV is required", nameof(config));

            _config = config;
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Process encrypted request from Aaple Sarkar
        /// This is the main entry point - call this from your Web API controller
        /// </summary>
        /// <param name="encryptedRequestBody">The encrypted request body (JSON with "data" property)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Encrypted response ready to return to Aaple Sarkar</returns>
        public async Task<DepartmentApiResponse> ProcessRequestAsync(
            string encryptedRequestBody,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Log("Processing incoming request from Aaple Sarkar");

                // Step 1: Parse encrypted request wrapper
                EncryptedRequest encryptedRequest;
                try
                {
                    encryptedRequest = JsonConvert.DeserializeObject<EncryptedRequest>(encryptedRequestBody);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to parse request JSON: {ex.Message}");
                    return CreateErrorResponse(400, "Invalid request format");
                }

                if (encryptedRequest == null || string.IsNullOrEmpty(encryptedRequest.data))
                {
                    LogError("Request does not contain encrypted data");
                    return CreateErrorResponse(400, "Invalid request format");
                }

                // Step 2: Decrypt request
                string decryptedJson;
                try
                {
                    decryptedJson = Decrypt(encryptedRequest.data);
                    LogDebug($"Request decrypted: {decryptedJson}");
                }
                catch (Exception ex)
                {
                    LogError($"Decryption failed: {ex.Message}");
                    return CreateErrorResponse(400, "Failed to decrypt request");
                }

                // Step 3: Parse decrypted request
                ApplicationStatusRequest request;
                try
                {
                    request = JsonConvert.DeserializeObject<ApplicationStatusRequest>(decryptedJson);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to parse decrypted JSON: {ex.Message}");
                    return CreateErrorResponse(400, "Invalid request format");
                }

                // Step 4: Validate request
                var validation = ValidateRequest(request);
                if (!validation.IsValid)
                {
                    LogError($"Request validation failed: {string.Join(", ", validation.Errors)}");
                    return CreateErrorResponse(400, $"Validation failed: {string.Join(", ", validation.Errors)}");
                }

                Log($"Request validated - AppID: {request.AppID}, ServiceID: {request.ServiceID}");

                // Step 5: Get application data from department's data provider
                ApplicationStatusResponse response;
                try
                {
                    response = await _dataProvider.GetApplicationStatusAsync(
                        request.AppID,
                        request.ServiceID,
                        request.DeptName,
                        request.Language,
                        cancellationToken
                    );
                }
                catch (ApplicationNotFoundException ex)
                {
                    Log($"Application not found: {request.AppID}");
                    return CreateErrorResponse(404, ex.Message ?? "Application not found");
                }
                catch (Exception ex)
                {
                    LogError($"Data provider error: {ex.Message}");
                    return CreateErrorResponse(500, "Internal server error");
                }

                // Step 6: Validate response
                var responseValidation = ValidateResponse(response);
                if (!responseValidation.IsValid)
                {
                    LogError($"Response validation failed: {string.Join(", ", responseValidation.Errors)}");
                    return CreateErrorResponse(500, "Invalid response data");
                }

                // Step 7: Ensure empty strings for null values (API specification requirement)
                NormalizeResponse(response);

                // Step 8: Serialize response to JSON
                string responseJson = JsonConvert.SerializeObject(response, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include
                });

                LogDebug($"Response JSON: {responseJson}");

                // Step 9: Encrypt response
                string encryptedResponse;
                try
                {
                    encryptedResponse = Encrypt(responseJson);
                    LogDebug("Response encrypted successfully");
                }
                catch (Exception ex)
                {
                    LogError($"Encryption failed: {ex.Message}");
                    return CreateErrorResponse(500, "Failed to encrypt response");
                }

                // Step 10: Return encrypted response
                Log($"Request processed successfully for AppID: {request.AppID}");

                return new DepartmentApiResponse
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    EncryptedData = new EncryptedResponse { data = encryptedResponse }
                };
            }
            catch (Exception ex)
            {
                LogError($"Unexpected error: {ex.Message}\n{ex.StackTrace}");
                return CreateErrorResponse(500, "An unexpected error occurred");
            }
        }

        /// <summary>
        /// Validate a request without processing it
        /// Useful for testing
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

        /// <summary>
        /// Validate a response before sending
        /// </summary>
        public ValidationResult ValidateResponse(ApplicationStatusResponse response)
        {
            var result = new ValidationResult();

            if (response == null)
            {
                result.AddError("Response cannot be null");
                return result;
            }

            if (string.IsNullOrWhiteSpace(response.ApplicationID))
                result.AddError("ApplicationID is required");

            if (string.IsNullOrWhiteSpace(response.ServiceName))
                result.AddError("ServiceName is required");

            if (string.IsNullOrWhiteSpace(response.ApplicantName))
                result.AddError("ApplicantName is required");

            // Validate FinalDecision values
            if (!string.IsNullOrEmpty(response.FinalDecision))
            {
                if (response.FinalDecision != "0" &&
                    response.FinalDecision != "1" &&
                    response.FinalDecision != "2")
                {
                    result.AddError("FinalDecision must be '0', '1', '2', or empty string");
                }
            }

            // Validate date formats (if not empty)
            if (!string.IsNullOrEmpty(response.ApplicationSubmissionDate))
            {
                if (!IsValidDateFormat(response.ApplicationSubmissionDate))
                    result.AddError("ApplicationSubmissionDate must be in format DD-MMM-YYYY,HH:mm:ss");
            }

            if (!string.IsNullOrEmpty(response.ApplicationPaymentDate))
            {
                if (!IsValidDateFormat(response.ApplicationPaymentDate))
                    result.AddError("ApplicationPaymentDate must be in format DD-MMM-YYYY,HH:mm:ss");
            }

            // Validate desk details
            if (response.DeskDetails != null)
            {
                foreach (var desk in response.DeskDetails)
                {
                    if (!string.IsNullOrEmpty(desk.ReviewActionDateTime))
                    {
                        if (!IsValidDateFormat(desk.ReviewActionDateTime))
                            result.AddError($"DeskDetail ReviewActionDateTime must be in format DD-MMM-YYYY,HH:mm:ss");
                    }
                }
            }

            return result;
        }

        #region Encryption/Decryption (Matching Aaple Sarkar's implementation)

        private string Encrypt(string plainText)
        {
            try
            {
                byte[] key = Encoding.UTF8.GetBytes(_config.EncryptionKey);
                byte[] iv = Encoding.UTF8.GetBytes(_config.EncryptionIV);
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
                throw new EncryptionException("Failed to encrypt response data", ex);
            }
        }

        private string Decrypt(string cipherText)
        {
            try
            {
                byte[] key = Encoding.UTF8.GetBytes(_config.EncryptionKey);
                byte[] iv = Encoding.UTF8.GetBytes(_config.EncryptionIV);
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
                throw new DecryptionException("Failed to decrypt request data", ex);
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

        #region Helper Methods

        /// <summary>
        /// Normalize response to ensure empty strings instead of nulls
        /// (API specification requirement)
        /// </summary>
        private void NormalizeResponse(ApplicationStatusResponse response)
        {
            response.ApplicationSubmissionDate = response.ApplicationSubmissionDate ?? "";
            response.ApplicationPaymentDate = response.ApplicationPaymentDate ?? "";
            response.NextActionRequiredDetails = response.NextActionRequiredDetails ?? "";
            response.FinalDecision = response.FinalDecision ?? "";
            response.DepartmentRedirectionURL = response.DepartmentRedirectionURL ?? "";

            if (response.DeskDetails != null)
            {
                foreach (var desk in response.DeskDetails)
                {
                    desk.ReviewActionBy = desk.ReviewActionBy ?? "";
                    desk.ReviewActionDateTime = desk.ReviewActionDateTime ?? "";
                    desk.ReviewActionDetails = desk.ReviewActionDetails ?? "";
                }
            }
        }

        private bool IsValidDateFormat(string dateString)
        {
            try
            {
                DateTime.ParseExact(
                    dateString,
                    "dd-MMM-yyyy,HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        private DepartmentApiResponse CreateErrorResponse(int statusCode, string message)
        {
            return new DepartmentApiResponse
            {
                StatusCode = statusCode,
                IsSuccess = false,
                ErrorMessage = message,
                ErrorData = new
                {
                    error = message,
                    timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
                }
            };
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_config.EnableLogging)
            {
                Console.WriteLine($"[DepartmentSDK] {message}");
            }
        }

        private void LogDebug(string message)
        {
            if (_config.EnableLogging && _config.LogLevel == LogLevel.Debug)
            {
                Console.WriteLine($"[DepartmentSDK][DEBUG] {message}");
            }
        }

        private void LogError(string message)
        {
            if (_config.EnableLogging)
            {
                Console.Error.WriteLine($"[DepartmentSDK][ERROR] {message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Interface that departments must implement to provide application data
    /// This is where you connect the SDK to your database/business logic
    /// </summary>
    public interface IDepartmentDataProvider
    {
        /// <summary>
        /// Get application status from your system
        /// </summary>
        /// <param name="applicationId">Application ID from the request</param>
        /// <param name="serviceId">Service ID from the request</param>
        /// <param name="departmentName">Department name from the request</param>
        /// <param name="language">Language code: "EN" or "MR"</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Application status response</returns>
        /// <exception cref="ApplicationNotFoundException">Thrown when application is not found</exception>
        Task<ApplicationStatusResponse> GetApplicationStatusAsync(
            string applicationId,
            string serviceId,
            string departmentName,
            string language,
            CancellationToken cancellationToken = default);
    }

    #endregion

    #region Configuration

    /// <summary>
    /// Configuration for Department API handler
    /// </summary>
    public class DepartmentConfiguration
    {
        /// <summary>
        /// TripleDES encryption key (required, 24 characters)
        /// </summary>
        public string EncryptionKey { get; set; }

        /// <summary>
        /// TripleDES encryption IV (required, 8 characters)
        /// </summary>
        public string EncryptionIV { get; set; }

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

    #region Response Models

    /// <summary>
    /// Response from Department API handler
    /// </summary>
    public class DepartmentApiResponse
    {
        /// <summary>
        /// HTTP status code (200, 400, 404, 500)
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Encrypted response data (for successful requests)
        /// </summary>
        public EncryptedResponse EncryptedData { get; set; }

        /// <summary>
        /// Error message (for failed requests)
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Error data object (for failed requests)
        /// </summary>
        public object ErrorData { get; set; }
    }

    #endregion

    #region Request/Response Models (Matching V3 Specification)

    /// <summary>
    /// Encrypted request wrapper
    /// </summary>
    public class EncryptedRequest
    {
        [JsonProperty("data")]
        public string data { get; set; }
    }

    /// <summary>
    /// Encrypted response wrapper
    /// </summary>
    public class EncryptedResponse
    {
        [JsonProperty("data")]
        public string data { get; set; }
    }

    /// <summary>
    /// Request to get application status (decrypted)
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
    /// Base exception for Department API
    /// </summary>
    public class DepartmentApiException : Exception
    {
        public DepartmentApiException(string message) : base(message) { }
        public DepartmentApiException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when application is not found
    /// </summary>
    public class ApplicationNotFoundException : DepartmentApiException
    {
        public ApplicationNotFoundException() : base("Application not found") { }
        public ApplicationNotFoundException(string message) : base(message) { }
    }

    /// <summary>
    /// Encryption exception
    /// </summary>
    public class EncryptionException : DepartmentApiException
    {
        public EncryptionException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// Decryption exception
    /// </summary>
    public class DecryptionException : DepartmentApiException
    {
        public DecryptionException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    #endregion

    #region Helper Utilities

    /// <summary>
    /// Helper utilities for building responses
    /// </summary>
    public static class ResponseHelper
    {
        /// <summary>
        /// Format DateTime to API date string (DD-MMM-YYYY,HH:mm:ss)
        /// </summary>
        public static string FormatDate(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return "";

            return dateTime.Value.ToString("dd-MMM-yyyy,HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parse API date string to DateTime
        /// </summary>
        public static DateTime? ParseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

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
        /// Convert boolean final decision to API string
        /// </summary>
        public static string FormatFinalDecision(bool? approved)
        {
            if (!approved.HasValue)
                return ""; // Not decided

            return approved.Value ? "0" : "1"; // 0 = Approved, 1 = Rejected
        }

        /// <summary>
        /// Set final decision to pending
        /// </summary>
        public static string FormatPendingDecision()
        {
            return "2"; // 2 = Pending
        }

        /// <summary>
        /// Ensure string is never null (convert to empty string)
        /// </summary>
        public static string EmptyIfNull(string value)
        {
            return value ?? "";
        }
    }

    #endregion
}
