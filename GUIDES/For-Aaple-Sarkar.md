# For Aaple Sarkar Team - Client SDK Guide

**Purpose:** Call department APIs to fetch application status

---

## Quick Start (3 Steps)

### Step 1: Install
```bash
1. Copy CODE/Client/TrackApplicationSDK.cs to your project
2. Install Newtonsoft.Json:
   Install-Package Newtonsoft.Json
```

### Step 2: Configure
```csharp
using MaharashtraGov.TrackApplicationAPI;

// Initialize once per department
var revenueClient = new TrackApplicationClient(
    apiBaseUrl: "https://api.revenue.maharashtra.gov.in",
    encryptionKey: "revenue-dept-key-24char",
    encryptionIV: "rev-iv-8",
    departmentName: "Revenue Department"
);

var municipalClient = new TrackApplicationClient(
    apiBaseUrl: "https://api.municipal.maharashtra.gov.in",
    encryptionKey: "municipal-dept-key-24char",
    encryptionIV: "mun-iv-8",
    departmentName: "Municipal Corporation"
);
```

### Step 3: Use
```csharp
// Get application status
var response = await revenueClient.GetApplicationStatusAsync(
    applicationId: "INC12345678",
    serviceId: "4111",
    language: Language.English
);

// Display to citizen
Console.WriteLine($"Service: {response.ServiceName}");
Console.WriteLine($"Status: {StatusHelper.GetFinalDecisionText(response.FinalDecision)}");
Console.WriteLine($"Progress: {response.ProgressPercentage}%");
```

**That's it!** SDK handles encryption, retries, errors automatically.

---

## Common Usage

### Display on Web Portal
```csharp
public async Task<ActionResult> TrackApplication(string appId, string serviceId)
{
    try
    {
        var response = await _revenueClient.GetApplicationStatusAsync(appId, serviceId);

        return View(new StatusViewModel
        {
            ApplicationId = response.ApplicationID,
            ServiceName = response.ServiceName,
            ApplicantName = response.ApplicantName,
            Status = StatusHelper.GetFinalDecisionText(response.FinalDecision),
            Progress = response.ProgressPercentage,
            IsPaid = response.IsPaid,
            ActionRequired = response.IsActionRequired,
            ActionMessage = response.NextActionRequiredDetails
        });
    }
    catch (Exception ex)
    {
        return View("Error", new { Message = "Unable to retrieve status" });
    }
}
```

### Mobile API
```csharp
[HttpGet]
public async Task<JsonResult> GetStatus(string appId, string serviceId, string dept)
{
    var client = GetClientForDepartment(dept); // Your method to select client

    try
    {
        var response = await client.GetApplicationStatusAsync(appId, serviceId);

        return Json(new {
            success = true,
            data = new {
                applicationId = response.ApplicationID,
                serviceName = response.ServiceName,
                status = StatusHelper.GetFinalDecisionText(response.FinalDecision),
                progress = response.ProgressPercentage,
                isPaid = response.IsPaid,
                actionRequired = response.IsActionRequired
            }
        });
    }
    catch
    {
        return Json(new { success = false, error = "Unable to fetch status" });
    }
}
```

---

## Helper Properties

The SDK provides useful helper properties:

```csharp
var response = await client.GetApplicationStatusAsync(appId, serviceId);

// Check if paid
if (response.IsPaid)
    Console.WriteLine($"Paid on: {response.ApplicationPaymentDate}");

// Check if action required
if (response.IsActionRequired)
    Console.WriteLine($"Action: {response.NextActionRequiredDetails}");

// Check if final decision made
if (response.IsFinalDecisionMade)
    Console.WriteLine($"Decision: {response.FinalDecisionStatus}");

// Calculate progress
Console.WriteLine($"Progress: {response.ProgressPercentage}%");
```

---

## Error Handling

```csharp
try
{
    var response = await client.GetApplicationStatusAsync(appId, serviceId);
    // Use response...
}
catch (ValidationException ex)
{
    // Invalid request (check your parameters)
    Log($"Validation error: {string.Join(", ", ex.ValidationErrors)}");
}
catch (EncryptionException ex)
{
    // Wrong encryption key/IV
    Log($"Encryption error - check credentials");
}
catch (ApiException ex)
{
    // API returned error
    if (ex.StatusCode == HttpStatusCode.NotFound)
        Show("Application not found");
    else
        Show("API error - try again later");
}
catch (TrackApplicationException ex)
{
    // Network or other error
    Log($"Error: {ex.Message}");
    Show("Unable to connect - try again later");
}
```

---

## Configuration (Web.config)

```xml
<configuration>
  <appSettings>
    <!-- Revenue Department -->
    <add key="Revenue.ApiUrl" value="https://api.revenue.maharashtra.gov.in" />
    <add key="Revenue.EncryptionKey" value="revenue-dept-key-24char" />
    <add key="Revenue.EncryptionIV" value="rev-iv-8" />

    <!-- Municipal Department -->
    <add key="Municipal.ApiUrl" value="https://api.municipal.maharashtra.gov.in" />
    <add key="Municipal.EncryptionKey" value="municipal-dept-key-24char" />
    <add key="Municipal.EncryptionIV" value="mun-iv-8" />

    <!-- Add all 50 departments... -->
  </appSettings>
</configuration>
```

```csharp
// Load from config
var revenueClient = new TrackApplicationClient(
    ConfigurationManager.AppSettings["Revenue.ApiUrl"],
    ConfigurationManager.AppSettings["Revenue.EncryptionKey"],
    ConfigurationManager.AppSettings["Revenue.EncryptionIV"],
    "Revenue Department"
);
```

---

## Multi-Language Support

```csharp
// Get response in Marathi
var marathiResponse = await client.GetApplicationStatusAsync(
    appId,
    serviceId,
    Language.Marathi
);

Console.WriteLine(marathiResponse.ServiceName); // "उत्पन्नाचा दाखला"

// Get status text in Marathi
string marathiStatus = StatusHelper.GetFinalDecisionText(
    response.FinalDecision,
    Language.Marathi
);
Console.WriteLine(marathiStatus); // "मंजूर" / "नाकारले" / "प्रलंबित"
```

---

## Advanced Configuration

```csharp
var config = new ClientConfiguration
{
    ApiBaseUrl = "https://api.revenue.maharashtra.gov.in",
    EncryptionKey = "revenue-dept-key-24char",
    EncryptionIV = "rev-iv-8",
    DepartmentName = "Revenue Department",

    // Optional settings
    Timeout = TimeSpan.FromSeconds(60),     // Increase timeout
    MaxRetries = 5,                          // More retries
    RetryDelay = TimeSpan.FromSeconds(3),    // Longer delay
    EnableLogging = true,                    // Enable logs
    LogLevel = LogLevel.Debug                // Detailed logs
};

var client = new TrackApplicationClient(config);
```

---

## Understanding Empty Strings

⚠️ **Important:** The API uses empty strings (`""`) for null values, not JSON `null`.

```csharp
// Check if paid
if (string.IsNullOrEmpty(response.ApplicationPaymentDate))
    Console.WriteLine("Not paid yet");
else
    Console.WriteLine($"Paid on: {response.ApplicationPaymentDate}");

// Or use helper property (better)
if (response.IsPaid)
    Console.WriteLine($"Paid on: {response.ApplicationPaymentDate}");
```

---

## Date Format

All dates use: `DD-MMM-YYYY,HH:mm:ss` (24-hour, no timezone)

Examples:
- `18-Sep-2025,17:30:00`
- `03-Nov-2025,14:25:30`

Parse dates:
```csharp
DateTime? submittedDate = StatusHelper.ParseDate(response.ApplicationSubmissionDate);
if (submittedDate.HasValue)
    Console.WriteLine($"Submitted: {submittedDate.Value:yyyy-MM-dd}");
```

---

## Final Decision Values

| Value | Meaning | English | Marathi |
|-------|---------|---------|---------|
| `"0"` | Approved | "Approved" | "मंजूर" |
| `"1"` | Rejected | "Rejected" | "नाकारले" |
| `"2"` | Pending | "Pending" | "प्रलंबित" |
| `""` | Not decided | "Pending" | "प्रलंबित" |

---

## Files You Need

✅ **CODE/Client/TrackApplicationSDK.cs** - The SDK (copy to your project)
✅ **CODE/Client/TrackApplicationSDK-Examples.cs** - More examples if needed
✅ **Newtonsoft.Json** - Install via NuGet

---

## Support

**Issues with SDK:**
- Check parameters are correct
- Verify encryption keys
- Enable logging to see details

**Issues with department API:**
- Contact the specific department
- Check their endpoint URL
- Verify they're deployed

---

## Summary

**3 Steps to Integration:**
1. Copy SDK file → Install Newtonsoft.Json
2. Configure with department credentials
3. Call with 3 lines of code

**SDK handles:**
- ✅ Encryption/decryption
- ✅ HTTP communication
- ✅ Retry logic
- ✅ Error handling
- ✅ Type safety

**You get:**
- Clean, simple code (3 lines)
- Consistent integration across 50 departments
- Professional error handling
