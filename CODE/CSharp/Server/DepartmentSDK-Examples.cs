// ============================================================================
// Department SDK - Usage Examples
//
// This file contains 10 comprehensive examples showing how to use the
// Department SDK to implement Track Application API in your department.
//
// Examples:
// 1. Basic Web API Controller
// 2. Simple Data Provider with In-Memory Data
// 3. Database-Connected Data Provider (Entity Framework)
// 4. Multi-Language Support
// 5. Complex Workflow with Desk Tracking
// 6. Error Handling and Logging
// 7. ASP.NET Core Web API Integration
// 8. Configuration from Web.config/appsettings.json
// 9. Unit Testing
// 10. Performance Monitoring
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using MaharashtraGov.DepartmentAPI;

namespace DepartmentSDK.Examples
{
    // ========================================================================
    // EXAMPLE 1: Basic Web API Controller
    // ========================================================================

    /// <summary>
    /// Example 1: Simple Web API controller implementation
    /// This is the easiest way to get started - just 3 steps!
    /// </summary>
    [RoutePrefix("api/SampleAPI")]
    public class Example1_BasicController : ApiController
    {
        // Step 1: Create your data provider
        private static readonly IDataProvider _dataProvider = new SimpleDataProvider();

        // Step 2: Configure the handler
        private static readonly DepartmentApiHandler _handler = new DepartmentApiHandler(
            encryptionKey: "YOUR_24_CHARACTER_KEY_HERE",
            encryptionIV: "YOUR_8IV",
            dataProvider: _dataProvider
        );

        // Step 3: Create the API endpoint
        [HttpPost]
        [Route("sendappstatus_encrypted")]
        public async Task<IHttpActionResult> SendApplicationStatusEncrypted()
        {
            // Read the request body
            string requestBody = await Request.Content.ReadAsStringAsync();

            // Process with SDK
            var result = await _handler.ProcessRequestAsync(requestBody);

            // Return appropriate response
            if (result.IsSuccess)
                return Content(HttpStatusCode.OK, result.EncryptedData);
            else
                return Content((HttpStatusCode)result.StatusCode, result.ErrorData);
        }

        // Simple data provider (you'll implement this with your database)
        private class SimpleDataProvider : IDepartmentDataProvider
        {
            public Task<ApplicationStatusResponse> GetApplicationStatusAsync(
                string applicationId, string serviceId, string departmentName,
                string language, CancellationToken cancellationToken = default)
            {
                // Check if application exists
                if (applicationId != "INC12345678")
                    throw new ApplicationNotFoundException();

                // Return application data
                var response = new ApplicationStatusResponse
                {
                    ApplicationID = applicationId,
                    ServiceName = language == "MR" ? "उत्पन्नाचा दाखला" : "Income Certificate",
                    ApplicantName = "Ramesh Kumar",
                    EstimatedDisbursalDays = 7,
                    ApplicationSubmissionDate = ResponseHelper.FormatDate(DateTime.Now.AddDays(-5)),
                    ApplicationPaymentDate = ResponseHelper.FormatDate(DateTime.Now.AddDays(-4)),
                    NextActionRequiredDetails = "",
                    FinalDecision = "2", // Pending
                    DepartmentRedirectionURL = "",
                    TotalNumberOfDesks = 3,
                    CurrentDeskNumber = 2,
                    NextDeskNumber = 3,
                    DeskDetails = new DeskDetail[]
                    {
                        new DeskDetail
                        {
                            DeskNumber = "Desk 1",
                            ReviewActionBy = "Clerk - Mr. Patil",
                            ReviewActionDateTime = ResponseHelper.FormatDate(DateTime.Now.AddDays(-3)),
                            ReviewActionDetails = "Documents verified and forwarded"
                        }
                    }
                };

                return Task.FromResult(response);
            }
        }
    }

    // ========================================================================
    // EXAMPLE 2: In-Memory Data Provider (for testing)
    // ========================================================================

    /// <summary>
    /// Example 2: In-memory data provider for testing and development
    /// </summary>
    public class Example2_InMemoryDataProvider : IDepartmentDataProvider
    {
        private readonly Dictionary<string, ApplicationData> _applications;

        public Example2_InMemoryDataProvider()
        {
            // Sample data
            _applications = new Dictionary<string, ApplicationData>
            {
                ["INC12345678"] = new ApplicationData
                {
                    ApplicationId = "INC12345678",
                    ServiceId = "4111",
                    ServiceNameEN = "Income Certificate",
                    ServiceNameMR = "उत्पन्नाचा दाखला",
                    ApplicantName = "Ramesh Kumar",
                    SubmittedDate = DateTime.Now.AddDays(-5),
                    PaidDate = DateTime.Now.AddDays(-4),
                    Status = ApplicationStatus.InProgress,
                    CurrentDesk = 2
                },
                ["DOM87654321"] = new ApplicationData
                {
                    ApplicationId = "DOM87654321",
                    ServiceId = "4112",
                    ServiceNameEN = "Domicile Certificate",
                    ServiceNameMR = "अधिवास प्रमाणपत्र",
                    ApplicantName = "Priya Sharma",
                    SubmittedDate = DateTime.Now.AddDays(-10),
                    PaidDate = DateTime.Now.AddDays(-9),
                    Status = ApplicationStatus.Approved,
                    CurrentDesk = 3
                }
            };
        }

        public Task<ApplicationStatusResponse> GetApplicationStatusAsync(
            string applicationId, string serviceId, string departmentName,
            string language, CancellationToken cancellationToken = default)
        {
            // Find application
            if (!_applications.TryGetValue(applicationId, out var app))
                throw new ApplicationNotFoundException($"Application {applicationId} not found");

            // Build response
            var response = new ApplicationStatusResponse
            {
                ApplicationID = app.ApplicationId,
                ServiceName = language == "MR" ? app.ServiceNameMR : app.ServiceNameEN,
                ApplicantName = app.ApplicantName,
                EstimatedDisbursalDays = 7,
                ApplicationSubmissionDate = ResponseHelper.FormatDate(app.SubmittedDate),
                ApplicationPaymentDate = ResponseHelper.FormatDate(app.PaidDate),
                NextActionRequiredDetails = app.Status == ApplicationStatus.ActionRequired
                    ? "Additional documents required" : "",
                FinalDecision = GetFinalDecisionCode(app.Status),
                DepartmentRedirectionURL = "",
                TotalNumberOfDesks = 3,
                CurrentDeskNumber = app.CurrentDesk,
                NextDeskNumber = app.CurrentDesk < 3 ? app.CurrentDesk + 1 : 0,
                DeskDetails = GetDeskDetails(app)
            };

            return Task.FromResult(response);
        }

        private string GetFinalDecisionCode(ApplicationStatus status)
        {
            switch (status)
            {
                case ApplicationStatus.Approved: return "0";
                case ApplicationStatus.Rejected: return "1";
                case ApplicationStatus.InProgress: return "2";
                default: return "";
            }
        }

        private DeskDetail[] GetDeskDetails(ApplicationData app)
        {
            var details = new List<DeskDetail>();
            for (int i = 1; i < app.CurrentDesk; i++)
            {
                details.Add(new DeskDetail
                {
                    DeskNumber = $"Desk {i}",
                    ReviewActionBy = $"Officer {i}",
                    ReviewActionDateTime = ResponseHelper.FormatDate(app.SubmittedDate.AddDays(i)),
                    ReviewActionDetails = "Reviewed and forwarded"
                });
            }
            return details.ToArray();
        }

        // Helper classes
        private class ApplicationData
        {
            public string ApplicationId { get; set; }
            public string ServiceId { get; set; }
            public string ServiceNameEN { get; set; }
            public string ServiceNameMR { get; set; }
            public string ApplicantName { get; set; }
            public DateTime SubmittedDate { get; set; }
            public DateTime? PaidDate { get; set; }
            public ApplicationStatus Status { get; set; }
            public int CurrentDesk { get; set; }
        }

        private enum ApplicationStatus
        {
            InProgress,
            Approved,
            Rejected,
            ActionRequired
        }
    }

    // ========================================================================
    // EXAMPLE 3: Entity Framework Database Provider
    // ========================================================================

    /// <summary>
    /// Example 3: Real database integration using Entity Framework
    /// </summary>
    public class Example3_DatabaseDataProvider : IDepartmentDataProvider
    {
        private readonly ApplicationDbContext _dbContext;

        public Example3_DatabaseDataProvider(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ApplicationStatusResponse> GetApplicationStatusAsync(
            string applicationId, string serviceId, string departmentName,
            string language, CancellationToken cancellationToken = default)
        {
            // Query database
            var application = await _dbContext.Applications
                .Include(a => a.Service)
                .Include(a => a.Reviews)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId
                                       && a.ServiceID == serviceId,
                                     cancellationToken);

            if (application == null)
                throw new ApplicationNotFoundException();

            // Build response from database entities
            var response = new ApplicationStatusResponse
            {
                ApplicationID = application.ApplicationID,
                ServiceName = language == "MR"
                    ? application.Service.NameMarathi
                    : application.Service.NameEnglish,
                ApplicantName = application.ApplicantName,
                EstimatedDisbursalDays = application.Service.EstimatedDays,
                ApplicationSubmissionDate = ResponseHelper.FormatDate(application.SubmittedDate),
                ApplicationPaymentDate = ResponseHelper.FormatDate(application.PaymentDate),
                NextActionRequiredDetails = application.ActionRequiredDetails ?? "",
                FinalDecision = MapStatusToCode(application.Status),
                DepartmentRedirectionURL = application.RedirectionURL ?? "",
                TotalNumberOfDesks = application.Service.TotalDesks,
                CurrentDeskNumber = application.CurrentDeskNumber,
                NextDeskNumber = application.NextDeskNumber,
                DeskDetails = application.Reviews
                    .OrderBy(r => r.DeskNumber)
                    .Select(r => new DeskDetail
                    {
                        DeskNumber = $"Desk {r.DeskNumber}",
                        ReviewActionBy = r.ReviewerName ?? "",
                        ReviewActionDateTime = ResponseHelper.FormatDate(r.ReviewDate),
                        ReviewActionDetails = r.Comments ?? ""
                    })
                    .ToArray()
            };

            return response;
        }

        private string MapStatusToCode(string status)
        {
            switch (status?.ToUpper())
            {
                case "APPROVED": return "0";
                case "REJECTED": return "1";
                case "PENDING": return "2";
                default: return "";
            }
        }
    }

    // Mock DbContext for example
    public class ApplicationDbContext
    {
        // Your Entity Framework DbSets
        public DbSet<Application> Applications { get; set; }
    }

    public class Application
    {
        public string ApplicationID { get; set; }
        public string ServiceID { get; set; }
        public Service Service { get; set; }
        public string ApplicantName { get; set; }
        public DateTime SubmittedDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Status { get; set; }
        public string ActionRequiredDetails { get; set; }
        public string RedirectionURL { get; set; }
        public int CurrentDeskNumber { get; set; }
        public int NextDeskNumber { get; set; }
        public List<Review> Reviews { get; set; }
    }

    public class Service
    {
        public string ServiceID { get; set; }
        public string NameEnglish { get; set; }
        public string NameMarathi { get; set; }
        public int EstimatedDays { get; set; }
        public int TotalDesks { get; set; }
    }

    public class Review
    {
        public int DeskNumber { get; set; }
        public string ReviewerName { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string Comments { get; set; }
    }

    // ========================================================================
    // EXAMPLE 4: ASP.NET Core Web API Integration
    // ========================================================================

    /// <summary>
    /// Example 4: Modern ASP.NET Core integration with dependency injection
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class Example4_AspNetCoreController : ControllerBase
    {
        private readonly DepartmentApiHandler _handler;
        private readonly ILogger<Example4_AspNetCoreController> _logger;

        public Example4_AspNetCoreController(
            DepartmentApiHandler handler,
            ILogger<Example4_AspNetCoreController> logger)
        {
            _handler = handler;
            _logger = logger;
        }

        [HttpPost("sendappstatus_encrypted")]
        public async Task<IActionResult> SendApplicationStatusEncrypted()
        {
            _logger.LogInformation("Received application status request");

            // Read request body
            using var reader = new StreamReader(Request.Body);
            string requestBody = await reader.ReadToEndAsync();

            // Process with SDK
            var result = await _handler.ProcessRequestAsync(requestBody);

            // Return response
            if (result.IsSuccess)
                return StatusCode(200, result.EncryptedData);
            else
                return StatusCode(result.StatusCode, result.ErrorData);
        }
    }

    // Startup.cs configuration
    public class Example4_Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Register data provider
            services.AddScoped<IDepartmentDataProvider, YourDataProvider>();

            // Register Department SDK handler
            services.AddScoped<DepartmentApiHandler>(sp =>
            {
                var config = new DepartmentConfiguration
                {
                    EncryptionKey = Configuration["AapleSarkar:EncryptionKey"],
                    EncryptionIV = Configuration["AapleSarkar:EncryptionIV"],
                    EnableLogging = true,
                    LogLevel = LogLevel.Debug
                };

                var dataProvider = sp.GetRequiredService<IDepartmentDataProvider>();
                return new DepartmentApiHandler(config, dataProvider);
            });

            services.AddControllers();
        }

        public IConfiguration Configuration { get; }
    }

    // ========================================================================
    // EXAMPLE 5: Multi-Language Support
    // ========================================================================

    /// <summary>
    /// Example 5: Implementing proper multi-language support
    /// </summary>
    public class Example5_MultiLanguageProvider : IDepartmentDataProvider
    {
        private readonly IServiceNameProvider _serviceNames;

        public Example5_MultiLanguageProvider(IServiceNameProvider serviceNames)
        {
            _serviceNames = serviceNames;
        }

        public async Task<ApplicationStatusResponse> GetApplicationStatusAsync(
            string applicationId, string serviceId, string departmentName,
            string language, CancellationToken cancellationToken = default)
        {
            // Your database query here...
            var app = await GetFromDatabase(applicationId);

            if (app == null)
                throw new ApplicationNotFoundException();

            // Get service name in requested language
            var serviceName = await _serviceNames.GetServiceNameAsync(serviceId, language);

            var response = new ApplicationStatusResponse
            {
                ApplicationID = applicationId,
                ServiceName = serviceName,
                ApplicantName = app.ApplicantName,
                // ... rest of the fields
            };

            return response;
        }

        private Task<ApplicationData> GetFromDatabase(string applicationId)
        {
            // Your implementation
            throw new NotImplementedException();
        }
    }

    public interface IServiceNameProvider
    {
        Task<string> GetServiceNameAsync(string serviceId, string language);
    }

    public class ServiceNameProvider : IServiceNameProvider
    {
        private readonly Dictionary<(string, string), string> _names;

        public ServiceNameProvider()
        {
            _names = new Dictionary<(string, string), string>
            {
                [("4111", "EN")] = "Income Certificate",
                [("4111", "MR")] = "उत्पन्नाचा दाखला",
                [("4112", "EN")] = "Domicile Certificate",
                [("4112", "MR")] = "अधिवास प्रमाणपत्र",
                [("4113", "EN")] = "Caste Certificate",
                [("4113", "MR")] = "जात प्रमाणपत्र"
            };
        }

        public Task<string> GetServiceNameAsync(string serviceId, string language)
        {
            if (_names.TryGetValue((serviceId, language), out var name))
                return Task.FromResult(name);

            return Task.FromResult("Unknown Service");
        }
    }

    // ========================================================================
    // EXAMPLE 6: Error Handling and Logging
    // ========================================================================

    /// <summary>
    /// Example 6: Comprehensive error handling and logging
    /// </summary>
    public class Example6_ErrorHandlingController : ApiController
    {
        private readonly DepartmentApiHandler _handler;
        private readonly ILogger _logger;

        public Example6_ErrorHandlingController(
            DepartmentApiHandler handler,
            ILogger logger)
        {
            _handler = handler;
            _logger = logger;
        }

        [HttpPost]
        [Route("api/SampleAPI/sendappstatus_encrypted")]
        public async Task<IHttpActionResult> SendApplicationStatusEncrypted()
        {
            try
            {
                _logger.Info("Processing application status request");

                // Read request
                string requestBody = await Request.Content.ReadAsStringAsync();

                // Validate request body
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.Warn("Empty request body received");
                    return Content(HttpStatusCode.BadRequest, new { error = "Empty request" });
                }

                // Process with SDK
                var result = await _handler.ProcessRequestAsync(requestBody);

                // Log result
                if (result.IsSuccess)
                {
                    _logger.Info($"Request processed successfully (status {result.StatusCode})");
                    return Content(HttpStatusCode.OK, result.EncryptedData);
                }
                else
                {
                    _logger.Warn($"Request failed: {result.ErrorMessage} (status {result.StatusCode})");
                    return Content((HttpStatusCode)result.StatusCode, result.ErrorData);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error: {ex.Message}", ex);
                return InternalServerError(ex);
            }
        }
    }

    // Simple logger interface
    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception ex);
    }

    // ========================================================================
    // EXAMPLE 7: Configuration from Web.config/appsettings.json
    // ========================================================================

    /// <summary>
    /// Example 7: Loading configuration from app settings
    /// </summary>
    public class Example7_ConfigurationExample
    {
        // For .NET Framework (Web.config)
        public static DepartmentApiHandler CreateFromWebConfig()
        {
            var config = new DepartmentConfiguration
            {
                EncryptionKey = System.Configuration.ConfigurationManager.AppSettings["AapleSarkar:EncryptionKey"],
                EncryptionIV = System.Configuration.ConfigurationManager.AppSettings["AapleSarkar:EncryptionIV"],
                EnableLogging = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["AapleSarkar:EnableLogging"] ?? "false"),
                LogLevel = Enum.Parse<LogLevel>(System.Configuration.ConfigurationManager.AppSettings["AapleSarkar:LogLevel"] ?? "Info")
            };

            var dataProvider = new YourDataProvider();
            return new DepartmentApiHandler(config, dataProvider);
        }

        // For .NET Core (appsettings.json)
        public static DepartmentApiHandler CreateFromAppSettings(IConfiguration configuration)
        {
            var config = new DepartmentConfiguration
            {
                EncryptionKey = configuration["AapleSarkar:EncryptionKey"],
                EncryptionIV = configuration["AapleSarkar:EncryptionIV"],
                EnableLogging = configuration.GetValue<bool>("AapleSarkar:EnableLogging"),
                LogLevel = configuration.GetValue<LogLevel>("AapleSarkar:LogLevel")
            };

            var dataProvider = new YourDataProvider();
            return new DepartmentApiHandler(config, dataProvider);
        }
    }

    /* Web.config example:
    <appSettings>
      <add key="AapleSarkar:EncryptionKey" value="YOUR_24_CHARACTER_KEY_HERE" />
      <add key="AapleSarkar:EncryptionIV" value="YOUR_8IV" />
      <add key="AapleSarkar:EnableLogging" value="true" />
      <add key="AapleSarkar:LogLevel" value="Debug" />
    </appSettings>
    */

    /* appsettings.json example:
    {
      "AapleSarkar": {
        "EncryptionKey": "YOUR_24_CHARACTER_KEY_HERE",
        "EncryptionIV": "YOUR_8IV",
        "EnableLogging": true,
        "LogLevel": "Debug"
      }
    }
    */

    // ========================================================================
    // EXAMPLE 8: Unit Testing
    // ========================================================================

    /// <summary>
    /// Example 8: Unit testing your data provider
    /// </summary>
    [TestClass]
    public class Example8_UnitTests
    {
        [TestMethod]
        public async Task GetApplicationStatus_ValidApplication_ReturnsResponse()
        {
            // Arrange
            var dataProvider = new MockDataProvider();
            var config = new DepartmentConfiguration
            {
                EncryptionKey = "123456789012345678901234",
                EncryptionIV = "12345678"
            };
            var handler = new DepartmentApiHandler(config, dataProvider);

            // Create encrypted request
            var request = new { data = "encrypted_test_data" };
            var requestJson = JsonConvert.SerializeObject(request);

            // Act
            var response = await handler.ProcessRequestAsync(requestJson);

            // Assert
            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(200, response.StatusCode);
            Assert.IsNotNull(response.EncryptedData);
        }

        [TestMethod]
        public async Task GetApplicationStatus_ApplicationNotFound_Returns404()
        {
            // Arrange
            var dataProvider = new MockDataProvider(shouldThrowNotFound: true);
            var config = new DepartmentConfiguration
            {
                EncryptionKey = "123456789012345678901234",
                EncryptionIV = "12345678"
            };
            var handler = new DepartmentApiHandler(config, dataProvider);

            // Act
            var response = await handler.ProcessRequestAsync(/* encrypted request */);

            // Assert
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(404, response.StatusCode);
        }

        private class MockDataProvider : IDepartmentDataProvider
        {
            private readonly bool _shouldThrowNotFound;

            public MockDataProvider(bool shouldThrowNotFound = false)
            {
                _shouldThrowNotFound = shouldThrowNotFound;
            }

            public Task<ApplicationStatusResponse> GetApplicationStatusAsync(
                string applicationId, string serviceId, string departmentName,
                string language, CancellationToken cancellationToken = default)
            {
                if (_shouldThrowNotFound)
                    throw new ApplicationNotFoundException();

                return Task.FromResult(new ApplicationStatusResponse
                {
                    ApplicationID = applicationId,
                    ServiceName = "Test Service",
                    ApplicantName = "Test User",
                    EstimatedDisbursalDays = 7,
                    ApplicationSubmissionDate = ResponseHelper.FormatDate(DateTime.Now),
                    ApplicationPaymentDate = "",
                    NextActionRequiredDetails = "",
                    FinalDecision = "2",
                    DepartmentRedirectionURL = "",
                    TotalNumberOfDesks = 3,
                    CurrentDeskNumber = 1,
                    NextDeskNumber = 2,
                    DeskDetails = new DeskDetail[0]
                });
            }
        }
    }

    // ========================================================================
    // EXAMPLE 9: Performance Monitoring
    // ========================================================================

    /// <summary>
    /// Example 9: Adding performance monitoring and metrics
    /// </summary>
    public class Example9_PerformanceMonitoringController : ApiController
    {
        private readonly DepartmentApiHandler _handler;
        private readonly IMetricsCollector _metrics;

        public Example9_PerformanceMonitoringController(
            DepartmentApiHandler handler,
            IMetricsCollector metrics)
        {
            _handler = handler;
            _metrics = metrics;
        }

        [HttpPost]
        [Route("api/SampleAPI/sendappstatus_encrypted")]
        public async Task<IHttpActionResult> SendApplicationStatusEncrypted()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _metrics.IncrementCounter("api_requests_total");

                // Read request
                string requestBody = await Request.Content.ReadAsStringAsync();

                // Process
                var result = await _handler.ProcessRequestAsync(requestBody);

                stopwatch.Stop();
                _metrics.RecordTiming("api_request_duration_ms", stopwatch.ElapsedMilliseconds);

                if (result.IsSuccess)
                {
                    _metrics.IncrementCounter("api_requests_success");
                    return Content(HttpStatusCode.OK, result.EncryptedData);
                }
                else
                {
                    _metrics.IncrementCounter($"api_requests_error_{result.StatusCode}");
                    return Content((HttpStatusCode)result.StatusCode, result.ErrorData);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metrics.IncrementCounter("api_requests_exception");
                _metrics.RecordTiming("api_request_duration_ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    public interface IMetricsCollector
    {
        void IncrementCounter(string metric);
        void RecordTiming(string metric, long milliseconds);
    }

    // ========================================================================
    // EXAMPLE 10: Complete Production-Ready Implementation
    // ========================================================================

    /// <summary>
    /// Example 10: Complete production-ready implementation with all features
    /// </summary>
    [RoutePrefix("api/SampleAPI")]
    public class Example10_ProductionController : ApiController
    {
        private readonly DepartmentApiHandler _handler;
        private readonly ILogger _logger;
        private readonly IMetricsCollector _metrics;
        private readonly ICacheService _cache;

        public Example10_ProductionController(
            DepartmentApiHandler handler,
            ILogger logger,
            IMetricsCollector metrics,
            ICacheService cache)
        {
            _handler = handler;
            _logger = logger;
            _metrics = metrics;
            _cache = cache;
        }

        [HttpPost]
        [Route("sendappstatus_encrypted")]
        public async Task<IHttpActionResult> SendApplicationStatusEncrypted()
        {
            var requestId = Guid.NewGuid().ToString();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.Info($"[{requestId}] Received application status request");
                _metrics.IncrementCounter("api_requests_total");

                // Read request
                string requestBody = await Request.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.Warn($"[{requestId}] Empty request body");
                    return BadRequest("Empty request");
                }

                // Process with SDK
                var result = await _handler.ProcessRequestAsync(requestBody);

                stopwatch.Stop();
                _metrics.RecordTiming("api_request_duration_ms", stopwatch.ElapsedMilliseconds);

                // Log and return
                if (result.IsSuccess)
                {
                    _logger.Info($"[{requestId}] Request successful in {stopwatch.ElapsedMilliseconds}ms");
                    _metrics.IncrementCounter("api_requests_success");
                    return Content(HttpStatusCode.OK, result.EncryptedData);
                }
                else
                {
                    _logger.Warn($"[{requestId}] Request failed: {result.ErrorMessage} (HTTP {result.StatusCode})");
                    _metrics.IncrementCounter($"api_requests_error_{result.StatusCode}");
                    return Content((HttpStatusCode)result.StatusCode, result.ErrorData);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error($"[{requestId}] Unexpected error after {stopwatch.ElapsedMilliseconds}ms", ex);
                _metrics.IncrementCounter("api_requests_exception");
                return InternalServerError(ex);
            }
        }

        // Health check endpoint
        [HttpGet]
        [Route("health")]
        public IHttpActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                version = "1.0.0"
            });
        }
    }

    public interface ICacheService
    {
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
    }

    // ========================================================================
    // Placeholder implementations for examples
    // ========================================================================

    public class YourDataProvider : IDepartmentDataProvider
    {
        public Task<ApplicationStatusResponse> GetApplicationStatusAsync(
            string applicationId, string serviceId, string departmentName,
            string language, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Implement with your database logic");
        }
    }

    // Mock classes for Entity Framework example
    public class DbSet<T> where T : class { }
    public static class EntityFrameworkExtensions
    {
        public static Task<T> FirstOrDefaultAsync<T>(this DbSet<T> set,
            System.Linq.Expressions.Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken) where T : class
        {
            return Task.FromResult<T>(null);
        }
    }

    // Mock classes for ASP.NET Core example
    public class ControllerBase { protected IActionResult StatusCode(int code, object value) => null; }
    public interface IActionResult { }
    public class ApiControllerAttribute : Attribute { }
    public class RouteAttribute : Attribute { public RouteAttribute(string template) { } }
    public class HttpPostAttribute : Attribute { public HttpPostAttribute(string template = null) { } }
    public interface IServiceCollection { }
    public interface IServiceProvider { }
    public static class ServiceCollectionExtensions
    {
        public static void AddScoped<T>(this IServiceCollection services) where T : class { }
        public static void AddScoped<T>(this IServiceCollection services, Func<IServiceProvider, T> factory) where T : class { }
        public static void AddControllers(this IServiceCollection services) { }
    }
    public static class ServiceProviderExtensions
    {
        public static T GetRequiredService<T>(this IServiceProvider provider) { return default(T); }
    }
    public interface IConfiguration
    {
        string this[string key] { get; }
        T GetValue<T>(string key);
    }
    public class StreamReader : IDisposable
    {
        public StreamReader(System.IO.Stream stream) { }
        public Task<string> ReadToEndAsync() => Task.FromResult("");
        public void Dispose() { }
    }
    public class HttpRequest
    {
        public System.IO.Stream Body { get; set; }
    }

    // Mock unit test attributes
    public class TestClassAttribute : Attribute { }
    public class TestMethodAttribute : Attribute { }
    public static class Assert
    {
        public static void IsTrue(bool condition) { }
        public static void IsFalse(bool condition) { }
        public static void AreEqual(object expected, object actual) { }
        public static void IsNotNull(object value) { }
    }
}
