// ============================================================================
// Track Application Status API SDK - Usage Examples
//
// This file contains practical examples showing how to use the SDK
// in different scenarios commonly needed by government departments.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc; // For ASP.NET MVC examples
using MaharashtraGov.TrackApplicationAPI;

namespace TrackApplicationSDK.Examples
{
    // ========================================================================
    // EXAMPLE 1: Basic Usage - Simple Console Application
    // ========================================================================

    public class Example1_BasicUsage
    {
        public static async Task Main()
        {
            // Step 1: Initialize the client (one time, reuse across requests)
            var client = new TrackApplicationClient(
                apiBaseUrl: "https://api.maharashtra.gov.in",
                encryptionKey: "your-24-character-key-here",
                encryptionIV: "your-8-char-iv",
                departmentName: "Revenue Department"
            );

            // Step 2: Get application status
            try
            {
                var response = await client.GetApplicationStatusAsync(
                    applicationId: "INC12345678",
                    serviceId: "4111",
                    language: Language.English
                );

                // Step 3: Display results
                Console.WriteLine($"Application ID: {response.ApplicationID}");
                Console.WriteLine($"Service: {response.ServiceName}");
                Console.WriteLine($"Applicant: {response.ApplicantName}");
                Console.WriteLine($"Status: {StatusHelper.GetFinalDecisionText(response.FinalDecision)}");
                Console.WriteLine($"Progress: {response.ProgressPercentage}%");

                // Check if paid
                if (response.IsPaid)
                {
                    Console.WriteLine($"Paid on: {response.ApplicationPaymentDate}");
                }

                // Check if action required
                if (response.IsActionRequired)
                {
                    Console.WriteLine($"Action Required: {response.NextActionRequiredDetails}");
                }

                // Show review history
                Console.WriteLine("\nReview History:");
                foreach (var desk in response.DeskDetails ?? Array.Empty<DeskDetail>())
                {
                    Console.WriteLine($"  {desk.DeskNumber}: {desk.ReviewActionDetails}");
                    Console.WriteLine($"    Reviewed by: {desk.ReviewActionBy}");
                    Console.WriteLine($"    Date: {desk.ReviewActionDateTime}");
                }
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"Validation Error: {string.Join(", ", ex.ValidationErrors)}");
            }
            catch (EncryptionException ex)
            {
                Console.WriteLine($"Encryption Error: {ex.Message}");
                Console.WriteLine("Please check your encryption key and IV");
            }
            catch (ApiException ex)
            {
                Console.WriteLine($"API Error ({ex.StatusCode}): {ex.Message}");
            }
            catch (TrackApplicationException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    // ========================================================================
    // EXAMPLE 2: ASP.NET MVC Web Portal - Display Application Status
    // ========================================================================

    public class ApplicationStatusController : Controller
    {
        private readonly TrackApplicationClient _apiClient;

        // Initialize client in constructor (dependency injection recommended)
        public ApplicationStatusController()
        {
            _apiClient = new TrackApplicationClient(
                apiBaseUrl: System.Configuration.ConfigurationManager.AppSettings["ApiBaseUrl"],
                encryptionKey: System.Configuration.ConfigurationManager.AppSettings["EncryptionKey"],
                encryptionIV: System.Configuration.ConfigurationManager.AppSettings["EncryptionIV"],
                departmentName: "Revenue Department"
            );
        }

        // GET: /ApplicationStatus/Track?applicationId=INC12345678&serviceId=4111
        public async Task<ActionResult> Track(string applicationId, string serviceId)
        {
            if (string.IsNullOrEmpty(applicationId))
                return View("Error", new { Message = "Application ID is required" });

            if (string.IsNullOrEmpty(serviceId))
                return View("Error", new { Message = "Service ID is required" });

            try
            {
                var response = await _apiClient.GetApplicationStatusAsync(
                    applicationId,
                    serviceId,
                    Language.English
                );

                var viewModel = new ApplicationStatusViewModel
                {
                    ApplicationId = response.ApplicationID,
                    ServiceName = response.ServiceName,
                    ApplicantName = response.ApplicantName,
                    SubmittedDate = response.ApplicationSubmissionDate,
                    PaymentDate = response.ApplicationPaymentDate,
                    IsPaid = response.IsPaid,
                    EstimatedDays = response.EstimatedDisbursalDays,
                    FinalDecisionText = StatusHelper.GetFinalDecisionText(response.FinalDecision),
                    IsFinal = response.IsFinalDecisionMade,
                    ActionRequired = response.NextActionRequiredDetails,
                    DepartmentUrl = response.DepartmentRedirectionURL,
                    ProgressPercentage = response.ProgressPercentage,
                    TotalDesks = response.TotalNumberOfDesks,
                    CurrentDesk = response.CurrentDeskNumber,
                    ReviewHistory = (response.DeskDetails ?? Array.Empty<DeskDetail>())
                        .Select(d => new ReviewViewModel
                        {
                            DeskName = d.DeskNumber,
                            ReviewedBy = d.ReviewActionBy,
                            ReviewedDate = d.ReviewActionDateTime,
                            Comments = d.ReviewActionDetails
                        })
                        .ToList()
                };

                return View(viewModel);
            }
            catch (ValidationException ex)
            {
                return View("Error", new { Message = $"Invalid request: {string.Join(", ", ex.ValidationErrors)}" });
            }
            catch (ApiException ex)
            {
                return View("Error", new { Message = $"Unable to retrieve status. Please try again later." });
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Trace.TraceError($"Error retrieving application status: {ex}");
                return View("Error", new { Message = "An error occurred. Please try again." });
            }
        }

        // View Model
        public class ApplicationStatusViewModel
        {
            public string ApplicationId { get; set; }
            public string ServiceName { get; set; }
            public string ApplicantName { get; set; }
            public string SubmittedDate { get; set; }
            public string PaymentDate { get; set; }
            public bool IsPaid { get; set; }
            public int EstimatedDays { get; set; }
            public string FinalDecisionText { get; set; }
            public bool IsFinal { get; set; }
            public string ActionRequired { get; set; }
            public string DepartmentUrl { get; set; }
            public int ProgressPercentage { get; set; }
            public int TotalDesks { get; set; }
            public int CurrentDesk { get; set; }
            public List<ReviewViewModel> ReviewHistory { get; set; }
        }

        public class ReviewViewModel
        {
            public string DeskName { get; set; }
            public string ReviewedBy { get; set; }
            public string ReviewedDate { get; set; }
            public string Comments { get; set; }
        }
    }

    // ========================================================================
    // EXAMPLE 3: Background Job - Check Applications and Send Notifications
    // ========================================================================

    public class ApplicationMonitoringService
    {
        private readonly TrackApplicationClient _apiClient;
        private readonly INotificationService _notificationService;

        public ApplicationMonitoringService(
            string apiBaseUrl,
            string encryptionKey,
            string encryptionIV,
            INotificationService notificationService)
        {
            _apiClient = new TrackApplicationClient(
                apiBaseUrl,
                encryptionKey,
                encryptionIV,
                "Revenue Department"
            );
            _notificationService = notificationService;
        }

        /// <summary>
        /// Check all pending applications and send notifications for status changes
        /// </summary>
        public async Task CheckPendingApplicationsAsync()
        {
            // Get list of applications to check from database
            var pendingApplications = GetPendingApplicationsFromDatabase();

            foreach (var app in pendingApplications)
            {
                try
                {
                    var response = await _apiClient.GetApplicationStatusAsync(
                        app.ApplicationId,
                        app.ServiceId
                    );

                    // Check if status changed
                    if (HasStatusChanged(app, response))
                    {
                        // Update database
                        UpdateApplicationStatus(app.ApplicationId, response);

                        // Send notification to citizen
                        if (response.IsFinalDecisionMade)
                        {
                            var decision = StatusHelper.GetFinalDecisionText(response.FinalDecision);
                            await _notificationService.SendEmailAsync(
                                app.CitizenEmail,
                                "Application Status Update",
                                $"Your application {app.ApplicationId} has been {decision}."
                            );

                            await _notificationService.SendSMSAsync(
                                app.CitizenMobile,
                                $"Your application {app.ApplicationId} has been {decision}."
                            );
                        }
                        else if (response.IsActionRequired)
                        {
                            await _notificationService.SendEmailAsync(
                                app.CitizenEmail,
                                "Action Required on Your Application",
                                response.NextActionRequiredDetails
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error and continue with next application
                    LogError($"Failed to check application {app.ApplicationId}: {ex.Message}");
                }
            }
        }

        // Placeholder methods
        private List<PendingApplication> GetPendingApplicationsFromDatabase()
        {
            // Your database logic here
            return new List<PendingApplication>();
        }

        private bool HasStatusChanged(PendingApplication app, ApplicationStatusResponse response)
        {
            // Compare with stored status
            return false;
        }

        private void UpdateApplicationStatus(string applicationId, ApplicationStatusResponse response)
        {
            // Update database
        }

        private void LogError(string message)
        {
            System.Diagnostics.Trace.TraceError(message);
        }
    }

    public class PendingApplication
    {
        public string ApplicationId { get; set; }
        public string ServiceId { get; set; }
        public string CitizenEmail { get; set; }
        public string CitizenMobile { get; set; }
    }

    public interface INotificationService
    {
        Task SendEmailAsync(string email, string subject, string body);
        Task SendSMSAsync(string mobile, string message);
    }

    // ========================================================================
    // EXAMPLE 4: Batch Processing - Check Multiple Applications
    // ========================================================================

    public class BatchApplicationProcessor
    {
        private readonly TrackApplicationClient _apiClient;

        public BatchApplicationProcessor(string apiBaseUrl, string encryptionKey, string encryptionIV)
        {
            _apiClient = new TrackApplicationClient(
                apiBaseUrl,
                encryptionKey,
                encryptionIV,
                "Revenue Department"
            );
        }

        /// <summary>
        /// Check status of multiple applications in batch
        /// </summary>
        public async Task<List<BatchResult>> CheckMultipleApplicationsAsync(
            List<string> applicationIds,
            string serviceId)
        {
            var results = new List<BatchResult>();

            foreach (var appId in applicationIds)
            {
                try
                {
                    var response = await _apiClient.GetApplicationStatusAsync(appId, serviceId);

                    results.Add(new BatchResult
                    {
                        ApplicationId = appId,
                        Success = true,
                        ServiceName = response.ServiceName,
                        ApplicantName = response.ApplicantName,
                        Status = StatusHelper.GetFinalDecisionText(response.FinalDecision),
                        Progress = response.ProgressPercentage
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new BatchResult
                    {
                        ApplicationId = appId,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return results;
        }

        public class BatchResult
        {
            public string ApplicationId { get; set; }
            public bool Success { get; set; }
            public string ServiceName { get; set; }
            public string ApplicantName { get; set; }
            public string Status { get; set; }
            public int Progress { get; set; }
            public string ErrorMessage { get; set; }
        }
    }

    // ========================================================================
    // EXAMPLE 5: Configuration with Advanced Options
    // ========================================================================

    public class Example5_AdvancedConfiguration
    {
        public static TrackApplicationClient CreateClientWithAdvancedOptions()
        {
            var config = new ClientConfiguration
            {
                ApiBaseUrl = "https://api.maharashtra.gov.in",
                ApiEndpoint = "/api/SampleAPI/sendappstatus_encrypted",
                EncryptionKey = "your-24-character-key-here",
                EncryptionIV = "your-8-char-iv",
                DepartmentName = "Revenue Department",

                // Advanced options
                Timeout = TimeSpan.FromSeconds(60),        // Increase timeout
                MaxRetries = 5,                            // More retries
                RetryDelay = TimeSpan.FromSeconds(3),      // Longer delay between retries
                EnableLogging = true,                      // Enable logging
                LogLevel = LogLevel.Debug                  // Detailed logs
            };

            return new TrackApplicationClient(config);
        }
    }

    // ========================================================================
    // EXAMPLE 6: Error Handling - Handle Different Error Scenarios
    // ========================================================================

    public class Example6_ErrorHandling
    {
        public static async Task DemonstrateErrorHandlingAsync(TrackApplicationClient client)
        {
            try
            {
                var response = await client.GetApplicationStatusAsync("INC12345678", "4111");

                // Success - use response
                Console.WriteLine($"Status: {response.ServiceName}");
            }
            catch (ValidationException ex)
            {
                // Request validation failed (e.g., missing required field)
                Console.WriteLine("Invalid request:");
                foreach (var error in ex.ValidationErrors)
                {
                    Console.WriteLine($"  - {error}");
                }

                // Fix: Check your input parameters
            }
            catch (EncryptionException ex)
            {
                // Encryption failed (e.g., wrong key/IV)
                Console.WriteLine($"Encryption error: {ex.Message}");

                // Fix: Check your encryption key and IV are correct
            }
            catch (DecryptionException ex)
            {
                // Decryption failed (e.g., API returned malformed data)
                Console.WriteLine($"Decryption error: {ex.Message}");

                // Fix: Contact API provider - response format may have changed
            }
            catch (ApiException ex)
            {
                // API returned error status code
                Console.WriteLine($"API error ({ex.StatusCode}): {ex.Message}");
                Console.WriteLine($"Response: {ex.ResponseContent}");

                // Fix depends on status code:
                // - 400: Check request format
                // - 401/403: Check authentication
                // - 404: Application not found
                // - 500: API server error (try again later)
            }
            catch (TrackApplicationException ex)
            {
                // General SDK error (e.g., network issues, retries exhausted)
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }

                // Fix: Check network connection, API availability
            }
        }
    }

    // ========================================================================
    // EXAMPLE 7: Mobile API Backend - REST API for Mobile Apps
    // ========================================================================

    public class MobileApiController : Controller
    {
        private readonly TrackApplicationClient _apiClient;

        public MobileApiController()
        {
            _apiClient = new TrackApplicationClient(
                System.Configuration.ConfigurationManager.AppSettings["ApiBaseUrl"],
                System.Configuration.ConfigurationManager.AppSettings["EncryptionKey"],
                System.Configuration.ConfigurationManager.AppSettings["EncryptionIV"],
                "Revenue Department"
            );
        }

        // GET: /api/mobile/application-status?appId=INC12345678&serviceId=4111&lang=en
        [HttpGet]
        public async Task<JsonResult> GetApplicationStatus(
            string appId,
            string serviceId,
            string lang = "en")
        {
            try
            {
                var language = lang.ToLower() == "mr" ? Language.Marathi : Language.English;
                var response = await _apiClient.GetApplicationStatusAsync(appId, serviceId, language);

                // Return mobile-friendly response
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        applicationId = response.ApplicationID,
                        serviceName = response.ServiceName,
                        applicantName = response.ApplicantName,
                        status = new
                        {
                            text = StatusHelper.GetFinalDecisionText(response.FinalDecision, language),
                            isFinal = response.IsFinalDecisionMade,
                            actionRequired = response.IsActionRequired,
                            actionMessage = response.NextActionRequiredDetails
                        },
                        progress = new
                        {
                            percentage = response.ProgressPercentage,
                            currentDesk = response.CurrentDeskNumber,
                            totalDesks = response.TotalNumberOfDesks
                        },
                        timeline = (response.DeskDetails ?? Array.Empty<DeskDetail>())
                            .Select(d => new
                            {
                                desk = d.DeskNumber,
                                reviewedBy = d.ReviewActionBy,
                                date = d.ReviewActionDateTime,
                                comments = d.ReviewActionDetails
                            })
                            .ToList()
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }

    // ========================================================================
    // EXAMPLE 8: Using Helper Utilities
    // ========================================================================

    public class Example8_HelperUtilities
    {
        public static void DemonstrateHelpers(ApplicationStatusResponse response)
        {
            // Get user-friendly status text
            string statusText = StatusHelper.GetFinalDecisionText(
                response.FinalDecision,
                Language.English
            );
            Console.WriteLine($"Status: {statusText}");

            // Get status in Marathi
            string marathiStatus = StatusHelper.GetFinalDecisionText(
                response.FinalDecision,
                Language.Marathi
            );
            Console.WriteLine($"स्थिती: {marathiStatus}");

            // Parse date string to DateTime
            DateTime? submittedDate = StatusHelper.ParseDate(response.ApplicationSubmissionDate);
            if (submittedDate.HasValue)
            {
                Console.WriteLine($"Submitted: {submittedDate.Value:yyyy-MM-dd}");
            }

            // Format DateTime to API format
            string formattedDate = StatusHelper.FormatDate(DateTime.Now);
            Console.WriteLine($"Current date in API format: {formattedDate}");

            // Check progress
            Console.WriteLine($"Application is {response.ProgressPercentage}% complete");

            // Use helper properties
            if (response.IsPaid)
                Console.WriteLine($"✓ Payment completed on {response.ApplicationPaymentDate}");

            if (response.IsActionRequired)
                Console.WriteLine($"⚠ Action needed: {response.NextActionRequiredDetails}");

            if (response.IsFinalDecisionMade)
                Console.WriteLine($"✓ Final decision: {response.FinalDecisionStatus}");
        }
    }

    // ========================================================================
    // EXAMPLE 9: Web.config Configuration
    // ========================================================================

    /*
    Add this to your Web.config:

    <configuration>
      <appSettings>
        <add key="ApiBaseUrl" value="https://api.maharashtra.gov.in" />
        <add key="EncryptionKey" value="your-24-character-key-here" />
        <add key="EncryptionIV" value="your-8-char-iv" />
        <add key="DepartmentName" value="Revenue Department" />
      </appSettings>
    </configuration>

    Then use:
    var client = new TrackApplicationClient(
        ConfigurationManager.AppSettings["ApiBaseUrl"],
        ConfigurationManager.AppSettings["EncryptionKey"],
        ConfigurationManager.AppSettings["EncryptionIV"],
        ConfigurationManager.AppSettings["DepartmentName"]
    );
    */

    // ========================================================================
    // EXAMPLE 10: Dependency Injection (for modern ASP.NET)
    // ========================================================================

    /*
    In Startup.cs or Program.cs:

    services.AddSingleton<TrackApplicationClient>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        return new TrackApplicationClient(
            config["TrackApplicationAPI:BaseUrl"],
            config["TrackApplicationAPI:EncryptionKey"],
            config["TrackApplicationAPI:EncryptionIV"],
            config["TrackApplicationAPI:DepartmentName"]
        );
    });

    Then inject in controller:

    public class ApplicationController : Controller
    {
        private readonly TrackApplicationClient _apiClient;

        public ApplicationController(TrackApplicationClient apiClient)
        {
            _apiClient = apiClient;
        }

        // Use _apiClient...
    }
    */
}
