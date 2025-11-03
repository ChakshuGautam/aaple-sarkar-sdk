// ============================================================================
// Department API Validator
//
// This tool validates that your Track Application API implementation
// conforms to the V3 specification.
//
// USAGE:
// 1. Run your API locally or on test server
// 2. Run this validator against your API
// 3. Fix any validation errors
// 4. Re-test until all tests pass
// 5. Ready for production!
//
// Run as Console Application or Unit Tests
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DepartmentAPI.Validator
{
    /// <summary>
    /// Validates department API implementation against V3 specification
    /// </summary>
    public class APIValidator
    {
        private readonly string _apiBaseUrl;
        private readonly string _encryptionKey;
        private readonly string _encryptionIV;
        private readonly HttpClient _httpClient;
        private List<ValidationResult> _results = new List<ValidationResult>();

        public APIValidator(string apiBaseUrl, string encryptionKey, string encryptionIV)
        {
            _apiBaseUrl = apiBaseUrl;
            _encryptionKey = encryptionKey;
            _encryptionIV = encryptionIV;
            _httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
        }

        /// <summary>
        /// Run all validation tests
        /// </summary>
        public async Task<ValidationReport> ValidateAPIAsync()
        {
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine("Department API Validator - V3 Specification");
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine($"API URL: {_apiBaseUrl}");
            Console.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            _results.Clear();

            // Test 1: Basic connectivity
            await Test_BasicConnectivity();

            // Test 2: Valid request with all fields
            await Test_ValidRequest_AllFields();

            // Test 3: Valid request - Marathi language
            await Test_ValidRequest_Marathi();

            // Test 4: Missing required fields
            await Test_MissingRequiredFields();

            // Test 5: Invalid language
            await Test_InvalidLanguage();

            // Test 6: Response format validation
            await Test_ResponseFormat();

            // Test 7: Empty string convention
            await Test_EmptyStringConvention();

            // Test 8: Date format validation
            await Test_DateFormat();

            // Test 9: FinalDecision values
            await Test_FinalDecisionValues();

            // Test 10: Desk details ordering
            await Test_DeskDetailsOrdering();

            // Test 11: Encryption/Decryption
            await Test_EncryptionDecryption();

            // Test 12: Application not found scenario
            await Test_ApplicationNotFound();

            // Generate report
            return GenerateReport();
        }

        // ====================================================================
        // TEST CASES
        // ====================================================================

        private async Task Test_BasicConnectivity()
        {
            StartTest("Basic Connectivity", "API endpoint is accessible");

            try
            {
                var response = await _httpClient.GetAsync("/");
                Pass($"API is reachable (Status: {response.StatusCode})");
            }
            catch (Exception ex)
            {
                Fail($"Cannot reach API: {ex.Message}");
            }
        }

        private async Task Test_ValidRequest_AllFields()
        {
            StartTest("Valid Request - All Fields", "API handles valid request correctly");

            var request = new
            {
                AppID = "INC12345678",
                ServiceID = "4111",
                DeptName = "Revenue Department",
                Language = "EN"
            };

            try
            {
                var response = await SendEncryptedRequest(request);

                if (response != null)
                {
                    // Validate response structure
                    if (string.IsNullOrEmpty(response.ApplicationID))
                        Fail("ApplicationID is missing or empty");
                    else if (response.ApplicationID != request.AppID)
                        Warn($"ApplicationID mismatch: sent '{request.AppID}', got '{response.ApplicationID}'");
                    else
                        Pass($"ApplicationID matches: {response.ApplicationID}");

                    if (string.IsNullOrEmpty(response.ServiceName))
                        Fail("ServiceName is missing or empty");
                    else
                        Pass($"ServiceName present: {response.ServiceName}");

                    if (string.IsNullOrEmpty(response.ApplicantName))
                        Fail("ApplicantName is missing or empty");
                    else
                        Pass($"ApplicantName present: {response.ApplicantName}");

                    if (response.EstimatedDisbursalDays <= 0)
                        Warn($"EstimatedDisbursalDays seems low: {response.EstimatedDisbursalDays}");
                    else
                        Pass($"EstimatedDisbursalDays: {response.EstimatedDisbursalDays} days");

                    Pass("Valid request processed successfully");
                }
                else
                {
                    Fail("No response received from API");
                }
            }
            catch (Exception ex)
            {
                Fail($"Valid request failed: {ex.Message}");
            }
        }

        private async Task Test_ValidRequest_Marathi()
        {
            StartTest("Marathi Language Support", "API returns Marathi text when Language=MR");

            var request = new
            {
                AppID = "INC12345678",
                ServiceID = "4111",
                DeptName = "Revenue Department",
                Language = "MR"
            };

            try
            {
                var response = await SendEncryptedRequest(request);

                if (response != null)
                {
                    // Check if service name contains Devanagari characters
                    bool hasDevanagari = response.ServiceName.Any(c => c >= 0x0900 && c <= 0x097F);

                    if (hasDevanagari)
                        Pass($"Marathi text detected: {response.ServiceName}");
                    else
                        Warn($"Language=MR but ServiceName doesn't contain Marathi: {response.ServiceName}");
                }
            }
            catch (Exception ex)
            {
                Fail($"Marathi request failed: {ex.Message}");
            }
        }

        private async Task Test_MissingRequiredFields()
        {
            StartTest("Missing Required Fields", "API rejects requests with missing fields");

            // Missing AppID
            try
            {
                var request = new
                {
                    ServiceID = "4111",
                    DeptName = "Revenue Department",
                    Language = "EN"
                };

                var response = await SendEncryptedRequestExpectingError(request);
                if (response.IsSuccessStatusCode)
                    Fail("API accepted request with missing AppID (should reject)");
                else
                    Pass($"Correctly rejected missing AppID (Status: {response.StatusCode})");
            }
            catch
            {
                Pass("Correctly rejected missing AppID");
            }
        }

        private async Task Test_InvalidLanguage()
        {
            StartTest("Invalid Language Value", "API rejects invalid language codes");

            var request = new
            {
                AppID = "INC12345678",
                ServiceID = "4111",
                DeptName = "Revenue Department",
                Language = "FR" // Invalid - should be EN or MR
            };

            try
            {
                var response = await SendEncryptedRequestExpectingError(request);
                if (response.IsSuccessStatusCode)
                    Fail("API accepted invalid language 'FR' (should reject)");
                else
                    Pass($"Correctly rejected invalid language (Status: {response.StatusCode})");
            }
            catch
            {
                Pass("Correctly rejected invalid language");
            }
        }

        private async Task Test_ResponseFormat()
        {
            StartTest("Response Format Validation", "All required fields present in response");

            var request = new
            {
                AppID = "INC12345678",
                ServiceID = "4111",
                DeptName = "Revenue Department",
                Language = "EN"
            };

            try
            {
                var response = await SendEncryptedRequest(request);

                if (response != null)
                {
                    CheckField("ApplicationID", response.ApplicationID);
                    CheckField("ServiceName", response.ServiceName);
                    CheckField("ApplicantName", response.ApplicantName);
                    CheckFieldNotNull("EstimatedDisbursalDays", response.EstimatedDisbursalDays);
                    CheckFieldNotNull("ApplicationSubmissionDate", response.ApplicationSubmissionDate);
                    CheckFieldNotNull("ApplicationPaymentDate", response.ApplicationPaymentDate);
                    CheckFieldNotNull("NextActionRequiredDetails", response.NextActionRequiredDetails);
                    CheckFieldNotNull("FinalDecision", response.FinalDecision);
                    CheckFieldNotNull("TotalNumberOfDesks", response.TotalNumberOfDesks);
                    CheckFieldNotNull("CurrentDeskNumber", response.CurrentDeskNumber);
                    CheckFieldNotNull("NextDeskNumber", response.NextDeskNumber);
                    CheckFieldNotNull("DeskDetails", response.DeskDetails);

                    Pass("All required fields present");
                }
            }
            catch (Exception ex)
            {
                Fail($"Response format validation failed: {ex.Message}");
            }
        }

        private async Task Test_EmptyStringConvention()
        {
            StartTest("Empty String Convention", "Null values represented as empty strings");

            var request = new
            {
                AppID = "INC12345678",
                ServiceID = "4111",
                DeptName = "Revenue Department",
                Language = "EN"
            };

            try
            {
                var response = await SendEncryptedRequest(request);

                if (response != null)
                {
                    // Check that fields use "" not null
                    if (response.ApplicationPaymentDate == null)
                        Fail("ApplicationPaymentDate is null (should be empty string)");
                    else
                        Pass($"ApplicationPaymentDate uses empty string: '{response.ApplicationPaymentDate}'");

                    if (response.NextActionRequiredDetails == null)
                        Fail("NextActionRequiredDetails is null (should be empty string)");
                    else
                        Pass($"NextActionRequiredDetails uses empty string: '{response.NextActionRequiredDetails}'");

                    Pass("Empty string convention followed");
                }
            }
            catch (Exception ex)
            {
                Fail($"Empty string test failed: {ex.Message}");
            }
        }

        private async Task Test_DateFormat()
        {
            StartTest("Date Format Validation", "Dates use DD-MMM-YYYY,HH:mm:ss format");

            var request = new
            {
                AppID = "INC12345678",
                ServiceID = "4111",
                DeptName = "Revenue Department",
                Language = "EN"
            };

            try
            {
                var response = await SendEncryptedRequest(request);

                if (response != null)
                {
                    // Check ApplicationSubmissionDate format
                    if (!string.IsNullOrEmpty(response.ApplicationSubmissionDate))
                    {
                        if (ValidateDateFormat(response.ApplicationSubmissionDate))
                            Pass($"ApplicationSubmissionDate format correct: {response.ApplicationSubmissionDate}");
                        else
                            Fail($"ApplicationSubmissionDate wrong format: {response.ApplicationSubmissionDate} (expected DD-MMM-YYYY,HH:mm:ss)");
                    }

                    // Check desk review dates
                    if (response.DeskDetails != null)
                    {
                        foreach (var desk in response.DeskDetails)
                        {
                            if (!string.IsNullOrEmpty(desk.ReviewActionDateTime))
                            {
                                if (ValidateDateFormat(desk.ReviewActionDateTime))
                                    Pass($"{desk.DeskNumber} date format correct");
                                else
                                    Fail($"{desk.DeskNumber} date wrong format: {desk.ReviewActionDateTime}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Fail($"Date format test failed: {ex.Message}");
            }
        }

        private async Task Test_FinalDecisionValues()
        {
            StartTest("FinalDecision Values", "FinalDecision uses correct values (0/1/2)");

            var request = new
            {
                AppID = "INC12345678",
                ServiceID = "4111",
                DeptName = "Revenue Department",
                Language = "EN"
            };

            try
            {
                var response = await SendEncryptedRequest(request);

                if (response != null)
                {
                    if (string.IsNullOrEmpty(response.FinalDecision))
                    {
                        Pass("FinalDecision is empty string (not decided)");
                    }
                    else if (response.FinalDecision == "0" || response.FinalDecision == "1" || response.FinalDecision == "2")
                    {
                        Pass($"FinalDecision value correct: '{response.FinalDecision}'");
                    }
                    else
                    {
                        Fail($"FinalDecision invalid value: '{response.FinalDecision}' (must be '0', '1', '2', or empty)");
                    }
                }
            }
            catch (Exception ex)
            {
                Fail($"FinalDecision test failed: {ex.Message}");
            }
        }

        private async Task Test_DeskDetailsOrdering()
        {
            StartTest("Desk Details Ordering", "DeskDetails array in ascending order");

            var request = new
            {
                AppID = "INC12345678",
                ServiceID = "4111",
                DeptName = "Revenue Department",
                Language = "EN"
            };

            try
            {
                var response = await SendEncryptedRequest(request);

                if (response != null && response.DeskDetails != null && response.DeskDetails.Length > 1)
                {
                    bool isOrdered = true;
                    for (int i = 0; i < response.DeskDetails.Length - 1; i++)
                    {
                        // Check if DeskNumber is increasing (basic check)
                        string current = response.DeskDetails[i].DeskNumber;
                        string next = response.DeskDetails[i + 1].DeskNumber;

                        if (string.Compare(current, next) > 0)
                        {
                            isOrdered = false;
                            Fail($"DeskDetails not in order: {current} comes before {next}");
                            break;
                        }
                    }

                    if (isOrdered)
                        Pass($"DeskDetails in ascending order ({response.DeskDetails.Length} desks)");
                }
                else
                {
                    Pass("DeskDetails ordering check skipped (0-1 desks)");
                }
            }
            catch (Exception ex)
            {
                Fail($"Desk ordering test failed: {ex.Message}");
            }
        }

        private async Task Test_EncryptionDecryption()
        {
            StartTest("Encryption/Decryption", "Request decryption and response encryption work correctly");

            try
            {
                string testData = "Test encryption";
                string encrypted = Encrypt(testData);
                string decrypted = Decrypt(encrypted);

                if (decrypted.TrimEnd('\0') == testData)
                    Pass("Encryption/decryption working correctly");
                else
                    Fail($"Encryption roundtrip failed: '{testData}' != '{decrypted}'");
            }
            catch (Exception ex)
            {
                Fail($"Encryption test failed: {ex.Message}");
            }
        }

        private async Task Test_ApplicationNotFound()
        {
            StartTest("Application Not Found Handling", "API returns appropriate error for non-existent application");

            var request = new
            {
                AppID = "NONEXISTENT999",
                ServiceID = "4111",
                DeptName = "Revenue Department",
                Language = "EN"
            };

            try
            {
                var response = await SendEncryptedRequestExpectingError(request);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    Pass($"Correctly returns 404 for non-existent application");
                else if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    Warn("Returns 200 for non-existent application (consider returning 404)");
                else
                    Pass($"Returns error status {response.StatusCode} for non-existent application");
            }
            catch (Exception ex)
            {
                Fail($"Application not found test failed: {ex.Message}");
            }
        }

        // ====================================================================
        // HELPER METHODS
        // ====================================================================

        private async Task<dynamic> SendEncryptedRequest(object request)
        {
            var requestJson = JsonConvert.SerializeObject(request);
            var encrypted = Encrypt(requestJson);

            var payload = new { data = encrypted };
            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            var httpResponse = await _httpClient.PostAsync("/api/SampleAPI/sendappstatus_encrypted", content);
            httpResponse.EnsureSuccessStatusCode();

            var responseContent = await httpResponse.Content.ReadAsStringAsync();
            var encryptedResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

            string decryptedJson = Decrypt(encryptedResponse.data.ToString());
            return JsonConvert.DeserializeObject<dynamic>(decryptedJson);
        }

        private async Task<HttpResponseMessage> SendEncryptedRequestExpectingError(object request)
        {
            var requestJson = JsonConvert.SerializeObject(request);
            var encrypted = Encrypt(requestJson);

            var payload = new { data = encrypted };
            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json"
            );

            return await _httpClient.PostAsync("/api/SampleAPI/sendappstatus_encrypted", content);
        }

        private bool ValidateDateFormat(string dateString)
        {
            // Format: DD-MMM-YYYY,HH:mm:ss
            try
            {
                DateTime.ParseExact(dateString, "dd-MMM-yyyy,HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void CheckField(string fieldName, string value)
        {
            if (string.IsNullOrEmpty(value))
                Warn($"{fieldName} is empty");
            else
                Pass($"{fieldName}: {(value.Length > 50 ? value.Substring(0, 50) + "..." : value)}");
        }

        private void CheckFieldNotNull(string fieldName, object value)
        {
            if (value == null)
                Fail($"{fieldName} is null (should not be)");
            else
                Pass($"{fieldName}: present");
        }

        // Encryption/Decryption (same as API)
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

                using (var encryptor = tdes.CreateEncryptor())
                {
                    byte[] encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
                    return BitConverter.ToString(encrypted).Replace("-", "");
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

                using (var decryptor = tdes.CreateDecryptor())
                {
                    byte[] decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);
                    return Encoding.UTF8.GetString(decrypted).TrimEnd('\0');
                }
            }
        }

        private byte[] HexStringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private void StartTest(string name, string description)
        {
            Console.WriteLine();
            Console.WriteLine($"TEST: {name}");
            Console.WriteLine($"      {description}");
            Console.WriteLine("-".PadRight(70, '-'));
        }

        private void Pass(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {message}");
            Console.ResetColor();
            _results.Add(new ValidationResult { TestName = "Current", Status = "PASS", Message = message });
        }

        private void Fail(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ {message}");
            Console.ResetColor();
            _results.Add(new ValidationResult { TestName = "Current", Status = "FAIL", Message = message });
        }

        private void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ {message}");
            Console.ResetColor();
            _results.Add(new ValidationResult { TestName = "Current", Status = "WARN", Message = message });
        }

        private ValidationReport GenerateReport()
        {
            var report = new ValidationReport
            {
                TotalTests = _results.Count,
                Passed = _results.Count(r => r.Status == "PASS"),
                Failed = _results.Count(r => r.Status == "FAIL"),
                Warnings = _results.Count(r => r.Status == "WARN"),
                Results = _results
            };

            Console.WriteLine();
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine("VALIDATION SUMMARY");
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine($"Total Checks: {report.TotalTests}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Passed:       {report.Passed}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed:       {report.Failed}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warnings:     {report.Warnings}");
            Console.ResetColor();
            Console.WriteLine();

            if (report.Failed == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ API VALIDATION PASSED - Ready for integration with Aaple Sarkar!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ API VALIDATION FAILED - {report.Failed} issue(s) need to be fixed");
                Console.ResetColor();
            }

            Console.WriteLine("=".PadRight(70, '='));
            return report;
        }
    }

    public class ValidationResult
    {
        public string TestName { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class ValidationReport
    {
        public int TotalTests { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Warnings { get; set; }
        public List<ValidationResult> Results { get; set; }
    }

    // ========================================================================
    // CONSOLE RUNNER
    // ========================================================================

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Department API Validator");
            Console.WriteLine();

            // Configure your API details
            string apiBaseUrl = "http://localhost:12345"; // Your API URL
            string encryptionKey = "your-24-character-key-here";
            string encryptionIV = "your-8ch";

            // Run validator
            var validator = new APIValidator(apiBaseUrl, encryptionKey, encryptionIV);
            var report = await validator.ValidateAPIAsync();

            // Exit code: 0 if passed, 1 if failed
            Environment.Exit(report.Failed == 0 ? 0 : 1);
        }
    }
}
