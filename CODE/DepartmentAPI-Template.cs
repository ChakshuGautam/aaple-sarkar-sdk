// ============================================================================
// Department API Template - Server-Side Implementation
//
// This template helps departments quickly build their Track Application API
// that conforms to the V3 specification.
//
// USAGE:
// 1. Copy this file to your ASP.NET Web API project
// 2. Implement the GetApplicationStatusFromDatabase() method
// 3. Configure encryption keys in Web.config
// 4. Deploy
//
// The template handles:
// - Request decryption
// - Response encryption
// - Validation
// - Error handling
// - Specification compliance
// ============================================================================

using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;
using Newtonsoft.Json;

namespace YourDepartment.TrackApplicationAPI
{
    /// <summary>
    /// Track Application Status API Controller
    /// Implements V3 specification for Aaple Sarkar integration
    /// </summary>
    [RoutePrefix("api/SampleAPI")]
    public class TrackApplicationController : ApiController
    {
        // ====================================================================
        // CONFIGURATION - Update these in Web.config
        // ====================================================================
        private readonly string _encryptionKey = System.Configuration.ConfigurationManager.AppSettings["EncryptionKey"];
        private readonly string _encryptionIV = System.Configuration.ConfigurationManager.AppSettings["EncryptionIV"];

        // ====================================================================
        // API ENDPOINT - This is what Aaple Sarkar will call
        // ====================================================================

        /// <summary>
        /// Get application status (encrypted request/response)
        /// POST /api/SampleAPI/sendappstatus_encrypted
        /// </summary>
        [HttpPost]
        [Route("sendappstatus_encrypted")]
        public HttpResponseMessage SendApplicationStatusEncrypted([FromBody] EncryptedRequest encryptedRequest)
        {
            try
            {
                // Step 1: Validate encrypted request
                if (encryptedRequest == null || string.IsNullOrEmpty(encryptedRequest.data))
                {
                    return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request format");
                }

                // Step 2: Decrypt request
                string decryptedJson;
                try
                {
                    decryptedJson = Decrypt(encryptedRequest.data);
                }
                catch (Exception ex)
                {
                    LogError($"Decryption failed: {ex.Message}");
                    return CreateErrorResponse(HttpStatusCode.BadRequest, "Failed to decrypt request");
                }

                // Step 3: Parse request
                ApplicationStatusRequest request;
                try
                {
                    request = JsonConvert.DeserializeObject<ApplicationStatusRequest>(decryptedJson);
                }
                catch (Exception ex)
                {
                    LogError($"JSON parsing failed: {ex.Message}");
                    return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request format");
                }

                // Step 4: Validate request fields
                var validationError = ValidateRequest(request);
                if (validationError != null)
                {
                    return CreateErrorResponse(HttpStatusCode.BadRequest, validationError);
                }

                // Step 5: Get application data from YOUR database
                // ⚠️ THIS IS WHERE YOU IMPLEMENT YOUR LOGIC ⚠️
                ApplicationStatusResponse response;
                try
                {
                    response = GetApplicationStatusFromDatabase(
                        request.AppID,
                        request.ServiceID,
                        request.DeptName,
                        request.Language
                    );
                }
                catch (ApplicationNotFoundException)
                {
                    return CreateErrorResponse(HttpStatusCode.NotFound, "Application not found");
                }
                catch (Exception ex)
                {
                    LogError($"Database error: {ex.Message}");
                    return CreateErrorResponse(HttpStatusCode.InternalServerError, "Internal server error");
                }

                // Step 6: Validate response (ensure it meets specification)
                var responseValidationError = ValidateResponse(response);
                if (responseValidationError != null)
                {
                    LogError($"Response validation failed: {responseValidationError}");
                    return CreateErrorResponse(HttpStatusCode.InternalServerError, "Invalid response data");
                }

                // Step 7: Serialize response to JSON
                string responseJson = JsonConvert.SerializeObject(response, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include
                });

                // Step 8: Encrypt response
                string encryptedResponse;
                try
                {
                    encryptedResponse = Encrypt(responseJson);
                }
                catch (Exception ex)
                {
                    LogError($"Encryption failed: {ex.Message}");
                    return CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to encrypt response");
                }

                // Step 9: Return encrypted response
                var result = new EncryptedResponse { data = encryptedResponse };
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                LogError($"Unexpected error: {ex.Message}\n{ex.StackTrace}");
                return CreateErrorResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred");
            }
        }

        // ====================================================================
        // YOUR IMPLEMENTATION - Fill in this method with your database logic
        // ====================================================================

        /// <summary>
        /// Get application status from YOUR database
        /// ⚠️ IMPLEMENT THIS METHOD ⚠️
        /// </summary>
        private ApplicationStatusResponse GetApplicationStatusFromDatabase(
            string applicationId,
            string serviceId,
            string departmentName,
            string language)
        {
            // ============================================================
            // TODO: Replace this with your actual database query
            // ============================================================

            // Example: Query your database
            // var dbRecord = _dbContext.Applications
            //     .Include(a => a.Reviews)
            //     .FirstOrDefault(a => a.ApplicationID == applicationId
            //                       && a.ServiceID == serviceId);
            //
            // if (dbRecord == null)
            //     throw new ApplicationNotFoundException();

            // ============================================================
            // Build response from your database data
            // ============================================================

            var response = new ApplicationStatusResponse
            {
                // Basic application info
                ApplicationID = applicationId,
                ServiceName = GetServiceName(serviceId, language), // Your method to get service name
                ApplicantName = "Get from your database", // dbRecord.ApplicantName
                EstimatedDisbursalDays = 7, // From your database

                // Dates (format: DD-MMM-YYYY,HH:mm:ss or empty string)
                ApplicationSubmissionDate = FormatDate(DateTime.Now), // dbRecord.SubmittedDate
                ApplicationPaymentDate = "", // Empty if not paid, otherwise formatted date

                // Action required
                NextActionRequiredDetails = "", // Empty if no action needed

                // Final decision: "0" = Approved, "1" = Rejected, "2" = Pending, "" = Not decided
                FinalDecision = "2", // From your database

                // Optional department URL
                DepartmentRedirectionURL = "",

                // Workflow tracking
                TotalNumberOfDesks = 3, // From your workflow configuration
                CurrentDeskNumber = 2, // From your database
                NextDeskNumber = 3, // From your workflow

                // Review history (in ascending order)
                DeskDetails = GetDeskDetailsFromDatabase(applicationId) // Your method
            };

            return response;
        }

        /// <summary>
        /// Example: Get desk review details from database
        /// </summary>
        private DeskDetail[] GetDeskDetailsFromDatabase(string applicationId)
        {
            // TODO: Query your database for review history
            // Example:
            // return _dbContext.Reviews
            //     .Where(r => r.ApplicationID == applicationId)
            //     .OrderBy(r => r.DeskNumber)
            //     .Select(r => new DeskDetail
            //     {
            //         DeskNumber = $"Desk {r.DeskNumber}",
            //         ReviewActionBy = r.ReviewerName ?? "",
            //         ReviewActionDateTime = FormatDate(r.ReviewedDate),
            //         ReviewActionDetails = r.Comments ?? ""
            //     })
            //     .ToArray();

            // Placeholder:
            return new DeskDetail[]
            {
                new DeskDetail
                {
                    DeskNumber = "Desk 1",
                    ReviewActionBy = "Officer Name",
                    ReviewActionDateTime = FormatDate(DateTime.Now.AddDays(-2)),
                    ReviewActionDetails = "Documents verified"
                }
            };
        }

        /// <summary>
        /// Example: Get service name based on language
        /// </summary>
        private string GetServiceName(string serviceId, string language)
        {
            // TODO: Query your database or configuration
            if (language == "MR")
                return "उत्पन्नाचा दाखला"; // Marathi
            else
                return "Income Certificate"; // English
        }

        // ====================================================================
        // VALIDATION - Ensures request/response meet specification
        // ====================================================================

        private string ValidateRequest(ApplicationStatusRequest request)
        {
            if (request == null)
                return "Request is null";

            if (string.IsNullOrWhiteSpace(request.AppID))
                return "AppID is required";

            if (string.IsNullOrWhiteSpace(request.ServiceID))
                return "ServiceID is required";

            if (string.IsNullOrWhiteSpace(request.DeptName))
                return "DeptName is required";

            if (string.IsNullOrWhiteSpace(request.Language))
                return "Language is required";

            if (request.Language != "EN" && request.Language != "MR")
                return "Language must be 'EN' or 'MR'";

            return null; // Valid
        }

        private string ValidateResponse(ApplicationStatusResponse response)
        {
            if (response == null)
                return "Response is null";

            if (string.IsNullOrWhiteSpace(response.ApplicationID))
                return "ApplicationID is required";

            if (string.IsNullOrWhiteSpace(response.ServiceName))
                return "ServiceName is required";

            if (string.IsNullOrWhiteSpace(response.ApplicantName))
                return "ApplicantName is required";

            // Validate FinalDecision values
            if (!string.IsNullOrEmpty(response.FinalDecision))
            {
                if (response.FinalDecision != "0" &&
                    response.FinalDecision != "1" &&
                    response.FinalDecision != "2")
                {
                    return "FinalDecision must be '0', '1', '2', or empty string";
                }
            }

            // All fields use empty string for null (not null itself)
            // This is validated during serialization

            return null; // Valid
        }

        // ====================================================================
        // ENCRYPTION/DECRYPTION - Matches Aaple Sarkar's implementation
        // ====================================================================

        private string Encrypt(string plainText)
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

        private string Decrypt(string cipherText)
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

        // ====================================================================
        // UTILITIES
        // ====================================================================

        /// <summary>
        /// Format DateTime to API specification: DD-MMM-YYYY,HH:mm:ss
        /// </summary>
        private string FormatDate(DateTime? date)
        {
            if (!date.HasValue)
                return ""; // Empty string for null dates

            return date.Value.ToString("dd-MMM-yyyy,HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture);
        }

        private HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string message)
        {
            return Request.CreateResponse(statusCode, new
            {
                error = message,
                timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
            });
        }

        private void LogError(string message)
        {
            // TODO: Implement your logging (database, file, etc.)
            System.Diagnostics.Trace.TraceError($"[TrackApplicationAPI] {message}");
        }
    }

    // ========================================================================
    // MODELS - Match V3 Specification Exactly
    // ========================================================================

    /// <summary>
    /// Encrypted request wrapper
    /// </summary>
    public class EncryptedRequest
    {
        public string data { get; set; }
    }

    /// <summary>
    /// Encrypted response wrapper
    /// </summary>
    public class EncryptedResponse
    {
        public string data { get; set; }
    }

    /// <summary>
    /// Application status request (decrypted)
    /// </summary>
    public class ApplicationStatusRequest
    {
        public string AppID { get; set; }
        public string ServiceID { get; set; }
        public string DeptName { get; set; }
        public string Language { get; set; } // "EN" or "MR"
    }

    /// <summary>
    /// Application status response (to be encrypted)
    /// Matches V3 specification exactly
    /// </summary>
    public class ApplicationStatusResponse
    {
        public string ApplicationID { get; set; }
        public string ServiceName { get; set; }
        public string ApplicantName { get; set; }
        public int EstimatedDisbursalDays { get; set; }

        // Format: DD-MMM-YYYY,HH:mm:ss (24hr) or empty string
        public string ApplicationSubmissionDate { get; set; }
        public string ApplicationPaymentDate { get; set; }

        // Empty string when no action required
        public string NextActionRequiredDetails { get; set; }

        // "0" = Approved, "1" = Rejected, "2" = Pending, "" = Not decided
        public string FinalDecision { get; set; }

        // Optional department URL
        public string DepartmentRedirectionURL { get; set; }

        // Workflow tracking
        public int TotalNumberOfDesks { get; set; }
        public int CurrentDeskNumber { get; set; }
        public int NextDeskNumber { get; set; }

        // Review history (in ascending order)
        public DeskDetail[] DeskDetails { get; set; }
    }

    /// <summary>
    /// Desk review detail
    /// </summary>
    public class DeskDetail
    {
        public string DeskNumber { get; set; } // e.g., "Desk 1"
        public string ReviewActionBy { get; set; } // Empty string if not assigned
        public string ReviewActionDateTime { get; set; } // DD-MMM-YYYY,HH:mm:ss or empty
        public string ReviewActionDetails { get; set; } // Empty string if no comments
    }

    /// <summary>
    /// Custom exception for application not found
    /// </summary>
    public class ApplicationNotFoundException : Exception
    {
        public ApplicationNotFoundException() : base("Application not found") { }
    }
}
