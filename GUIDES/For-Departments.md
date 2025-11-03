# For Departments - Build Your API

**Purpose:** Build the API that Aaple Sarkar will call to get application status

---

## Quick Start (4 Steps)

### Step 1: Copy Template
```bash
1. Download DepartmentAPI-Template.cs
2. Add to your ASP.NET Web API project
3. Install Newtonsoft.Json:
   Install-Package Newtonsoft.Json
```

### Step 2: Implement ONE Method

Open the template file and find this method:

```csharp
private ApplicationStatusResponse GetApplicationStatusFromDatabase(
    string applicationId,
    string serviceId,
    string departmentName,
    string language)
{
    // TODO: Replace this with YOUR database logic

    // Query YOUR database
    var app = _dbContext.Applications
        .Include(a => a.Reviews)
        .FirstOrDefault(a => a.ApplicationID == applicationId
                          && a.ServiceID == serviceId);

    if (app == null)
        throw new ApplicationNotFoundException();

    // Build response from YOUR data
    return new ApplicationStatusResponse
    {
        ApplicationID = app.ApplicationID,
        ServiceName = GetServiceName(serviceId, language),
        ApplicantName = app.ApplicantName,
        EstimatedDisbursalDays = 7,

        ApplicationSubmissionDate = FormatDate(app.SubmittedDate),
        ApplicationPaymentDate = app.PaidDate.HasValue
            ? FormatDate(app.PaidDate.Value)
            : "",

        NextActionRequiredDetails = app.ActionMessage ?? "",
        FinalDecision = app.Status == "Approved" ? "0"
                      : app.Status == "Rejected" ? "1"
                      : "2",

        DepartmentRedirectionURL = "",

        TotalNumberOfDesks = 3,
        CurrentDeskNumber = app.CurrentDesk,
        NextDeskNumber = app.NextDesk,

        DeskDetails = app.Reviews
            .OrderBy(r => r.DeskNumber)
            .Select(r => new DeskDetail
            {
                DeskNumber = $"Desk {r.DeskNumber}",
                ReviewActionBy = r.ReviewerName ?? "",
                ReviewActionDateTime = FormatDate(r.ReviewedDate),
                ReviewActionDetails = r.Comments ?? ""
            })
            .ToArray()
    };
}
```

**That's it!** Template handles encryption, validation, errors - everything else.

### Step 3: Configure Encryption

Add to Web.config:
```xml
<configuration>
  <appSettings>
    <add key="EncryptionKey" value="your-24-character-key-here" />
    <add key="EncryptionIV" value="your-8ch" />
  </appSettings>
</configuration>
```

Get these from Aaple Sarkar team.

### Step 4: Test with Validator

```bash
1. Deploy to test environment: http://test.yourdept.gov.in
2. Run DepartmentAPI-Validator.exe
3. Configure validator with your test URL and keys
4. Run tests
5. Fix any errors
6. Re-test until all pass
```

---

## What the Template Does for You

✅ **Request Decryption** - Automatically decrypts incoming requests
✅ **Response Encryption** - Automatically encrypts responses
✅ **Validation** - Validates all requests and responses
✅ **Error Handling** - Proper HTTP status codes and error messages
✅ **Specification Compliance** - Matches V3 spec exactly

**You only write database logic!**

---

## Example: Revenue Department

### Their Database
```sql
Table: IncomeApplications
- ApplicationNo (string)
- ServiceID (string)
- ApplicantFirstName (string)
- ApplicantLastName (string)
- SubmittedOn (datetime)
- PaidOn (datetime, nullable)
- Status (string) -- 'Pending', 'Approved', 'Rejected'
- CurrentDesk (int)

Table: ApplicationReviews
- ApplicationNo (string)
- DeskNumber (int)
- ReviewerName (string)
- ReviewedOn (datetime)
- Comments (string)
```

### Their Implementation

```csharp
// Their DbContext
private RevenueDbContext _db = new RevenueDbContext();

private ApplicationStatusResponse GetApplicationStatusFromDatabase(
    string applicationId,
    string serviceId,
    string departmentName,
    string language)
{
    // Query their database
    var app = _db.IncomeApplications
        .FirstOrDefault(a => a.ApplicationNo == applicationId);

    if (app == null)
        throw new ApplicationNotFoundException();

    var reviews = _db.ApplicationReviews
        .Where(r => r.ApplicationNo == applicationId)
        .OrderBy(r => r.DeskNumber)
        .ToList();

    // Map to response
    return new ApplicationStatusResponse
    {
        ApplicationID = app.ApplicationNo,
        ServiceName = language == "MR"
            ? "उत्पन्नाचा दाखला"
            : "Income Certificate",
        ApplicantName = $"{app.ApplicantFirstName} {app.ApplicantLastName}",
        EstimatedDisbursalDays = 7,

        ApplicationSubmissionDate = FormatDate(app.SubmittedOn),
        ApplicationPaymentDate = app.PaidOn.HasValue
            ? FormatDate(app.PaidOn.Value)
            : "",

        NextActionRequiredDetails = "",
        FinalDecision = app.Status == "Approved" ? "0"
                      : app.Status == "Rejected" ? "1"
                      : "2",

        DepartmentRedirectionURL = "",

        TotalNumberOfDesks = 3,
        CurrentDeskNumber = app.CurrentDesk,
        NextDeskNumber = app.CurrentDesk < 3 ? app.CurrentDesk + 1 : 0,

        DeskDetails = reviews.Select(r => new DeskDetail
        {
            DeskNumber = $"Desk {r.DeskNumber}",
            ReviewActionBy = r.ReviewerName ?? "",
            ReviewActionDateTime = FormatDate(r.ReviewedOn),
            ReviewActionDetails = r.Comments ?? ""
        }).ToArray()
    };
}
```

---

## Validation Tool

### Run Validator

```bash
# Configure in Program.cs
static async Task Main(string[] args)
{
    string apiBaseUrl = "http://test.revenue.maharashtra.gov.in";
    string encryptionKey = "revenue-dept-key-24char";
    string encryptionIV = "rev-iv-8";

    var validator = new APIValidator(apiBaseUrl, encryptionKey, encryptionIV);
    var report = await validator.ValidateAPIAsync();

    Environment.Exit(report.Failed == 0 ? 0 : 1);
}
```

### Sample Output

```
==================================================================
Department API Validator - V3 Specification
==================================================================
API URL: http://test.revenue.maharashtra.gov.in
Started: 2025-11-03 14:30:00

TEST: Basic Connectivity
----------------------------------------------------------------------
  ✓ API is reachable (Status: OK)

TEST: Valid Request - All Fields
----------------------------------------------------------------------
  ✓ ApplicationID matches: INC12345678
  ✓ ServiceName present: Income Certificate
  ✓ ApplicantName present: John Doe
  ✓ Valid request processed successfully

TEST: Response Format Validation
----------------------------------------------------------------------
  ✓ All required fields present

TEST: Empty String Convention
----------------------------------------------------------------------
  ✓ ApplicationPaymentDate uses empty string
  ✓ Empty string convention followed

TEST: Date Format Validation
----------------------------------------------------------------------
  ✓ ApplicationSubmissionDate format correct: 18-Sep-2025,17:30:00
  ✓ Desk 1 date format correct

TEST: FinalDecision Values
----------------------------------------------------------------------
  ✓ FinalDecision value correct: '2'

==================================================================
VALIDATION SUMMARY
==================================================================
Total Checks: 42
Passed:       42
Failed:       0
Warnings:     0

✓ API VALIDATION PASSED - Ready for integration with Aaple Sarkar!
==================================================================
```

---

## Field Specifications

### Required Fields

| Field | Type | Description | Your Value |
|-------|------|-------------|------------|
| `ApplicationID` | string | Application ID | From your DB |
| `ServiceName` | string | Service name (localized) | Based on ServiceID + Language |
| `ApplicantName` | string | Applicant full name | From your DB |
| `EstimatedDisbursalDays` | int | Estimated days | Your business logic |
| `ApplicationSubmissionDate` | string | Submission date (DD-MMM-YYYY,HH:mm:ss) | `FormatDate(yourDate)` |
| `ApplicationPaymentDate` | string | Payment date or `""` | `FormatDate(date)` or `""` |
| `NextActionRequiredDetails` | string | Action message or `""` | From your DB or `""` |
| `FinalDecision` | string | `"0"`, `"1"`, `"2"`, or `""` | Map your status |
| `DepartmentRedirectionURL` | string | Optional URL or `""` | Your URL or `""` |
| `TotalNumberOfDesks` | int | Total workflow desks | Your workflow config |
| `CurrentDeskNumber` | int | Current desk (0 if not assigned) | From your DB |
| `NextDeskNumber` | int | Next desk (0 if final) | Your business logic |
| `DeskDetails` | array | Review history | From your DB |

### FinalDecision Mapping

Map your status to standard values:

```csharp
// Your DB status → API value
"Approved"  → "0"
"Rejected"  → "1"
"Pending"   → "2"
"In Review" → "2"
null        → ""
```

### Date Format

Use the provided `FormatDate()` helper:

```csharp
// Format a DateTime
string formattedDate = FormatDate(myDateTime);
// Returns: "18-Sep-2025,17:30:00"

// Handle nullable DateTime
string paymentDate = myPaymentDate.HasValue
    ? FormatDate(myPaymentDate.Value)
    : "";
```

---

## Empty String Convention

⚠️ **Important:** Use empty strings (`""`) for null values, NOT `null`.

```csharp
// ✅ Correct
ApplicationPaymentDate = app.PaidDate.HasValue
    ? FormatDate(app.PaidDate.Value)
    : ""

// ❌ Wrong
ApplicationPaymentDate = app.PaidDate.HasValue
    ? FormatDate(app.PaidDate.Value)
    : null
```

---

## Multi-Language Support

```csharp
private string GetServiceName(string serviceId, string language)
{
    // Map serviceId to service name
    if (serviceId == "4111")
    {
        return language == "MR"
            ? "उत्पन्नाचा दाखला"
            : "Income Certificate";
    }
    else if (serviceId == "4112")
    {
        return language == "MR"
            ? "जातीचा दाखला"
            : "Caste Certificate";
    }

    // Default
    return "Unknown Service";
}
```

---

## Deployment Checklist

### Development
- [ ] Copy template to project
- [ ] Implement `GetApplicationStatusFromDatabase()`
- [ ] Configure encryption keys
- [ ] Test locally

### Testing
- [ ] Deploy to test environment
- [ ] Run validator tool
- [ ] Fix all validation errors
- [ ] Re-validate until pass
- [ ] Test with Aaple Sarkar team

### Production
- [ ] Deploy to production
- [ ] Run validator against production
- [ ] Share endpoint URL with Aaple Sarkar
- [ ] Monitor for errors

---

## Endpoint URL

Your API will be available at:
```
https://api.yourdept.maharashtra.gov.in/api/SampleAPI/sendappstatus_encrypted
```

Share this URL with Aaple Sarkar team along with your encryption keys.

---

## Common Issues

### Issue: "Decryption failed"
**Solution:** Check encryption key and IV are exactly 24 and 8 characters

### Issue: "Date format incorrect"
**Solution:** Use `FormatDate()` helper for all dates

### Issue: "FinalDecision invalid value"
**Solution:** Must be `"0"`, `"1"`, `"2"`, or `""` (empty string)

### Issue: "DeskDetails ordering wrong"
**Solution:** Use `.OrderBy(r => r.DeskNumber)` when querying

### Issue: "Application not found returns 200"
**Solution:** Template handles this - throw `ApplicationNotFoundException`

---

## Files You Need

✅ **DepartmentAPI-Template.cs** - API template (copy to project)
✅ **DepartmentAPI-Validator.cs** - Testing tool
✅ **Newtonsoft.Json** - Install via NuGet

---

## Timeline

**Day 1 Morning (2-3 hours):**
- Copy template
- Implement database method
- Configure encryption

**Day 1 Afternoon (1 hour):**
- Deploy to test
- Run validator
- Fix issues

**Day 2 (2-3 hours):**
- Re-validate
- Final testing
- Deploy to production

**Total: 2-3 days**

---

## Support

**Template Issues:**
- Check you've implemented the database method
- Ensure encryption keys are configured
- Review validator output for specific errors

**Aaple Sarkar Integration:**
- Share your endpoint URL
- Provide encryption keys
- Test together

---

## Summary

**4 Steps:**
1. Copy template → Install NuGet
2. Implement 1 database method (1-2 hours)
3. Test with validator (10 min)
4. Deploy (30 min)

**Template handles:**
- ✅ Encryption/decryption
- ✅ Validation
- ✅ Error handling
- ✅ Specification compliance

**You only write:**
- Database query logic (30 min - 2 hours)
- That's it!

**Ready in 2-3 days instead of 2-3 weeks! 🚀**
