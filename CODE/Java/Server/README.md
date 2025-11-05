# Java Server SDK - For Departments

Complete Java SDK for departments to implement their Track Application Status API that receives requests from Aaple Sarkar Portal.

## Installation

### Maven
```xml
<dependencies>
    <!-- Gson for JSON -->
    <dependency>
        <groupId>com.google.code.gson</groupId>
        <artifactId>gson</artifactId>
        <version>2.10.1</version>
    </dependency>

    <!-- Servlet API (for web applications) -->
    <dependency>
        <groupId>javax.servlet</groupId>
        <artifactId>javax.servlet-api</artifactId>
        <version>4.0.1</version>
        <scope>provided</scope>
    </dependency>
</dependencies>
```

### Gradle
```gradle
dependencies {
    implementation 'com.google.code.gson:gson:2.10.1'
    compileOnly 'javax.servlet:javax.servlet-api:4.0.1'
}
```

## Quick Start (3 Steps)

### Step 1: Copy SDK to Your Project
```bash
cp DepartmentSDK.java src/main/java/gov/maharashtra/departmentapi/
```

### Step 2: Implement Data Provider

```java
import gov.maharashtra.departmentapi.*;

public class MyDepartmentDataProvider implements DepartmentDataProvider {

    @Override
    public ApplicationStatusResponse getApplicationStatus(
            String applicationId,
            String serviceId,
            String departmentName,
            String language) throws ApplicationNotFoundException {

        // Query YOUR database
        Application app = database.findApplication(applicationId, serviceId);

        if (app == null) {
            throw new ApplicationNotFoundException();
        }

        // Build response
        ApplicationStatusResponse response = new ApplicationStatusResponse();
        response.ApplicationID = applicationId;
        response.ServiceName = language.equals("MR") ? "उत्पन्नाचा दाखला" : "Income Certificate";
        response.ApplicantName = app.getApplicantName();
        response.EstimatedDisbursalDays = 7;
        response.ApplicationSubmissionDate = ResponseHelper.formatDate(app.getSubmittedDate());
        response.ApplicationPaymentDate = ResponseHelper.formatDate(app.getPaymentDate());
        response.NextActionRequiredDetails = app.getActionDetails() != null ? app.getActionDetails() : "";
        response.FinalDecision = app.isApproved() ? "0" : "2";
        response.DepartmentRedirectionURL = "";
        response.TotalNumberOfDesks = 3;
        response.CurrentDeskNumber = app.getCurrentDesk();
        response.NextDeskNumber = app.getNextDesk();
        response.DeskDetails = getDeskDetails(app); // Your helper method

        return response;
    }
}
```

### Step 3: Create Servlet/Controller

**Using JAX-RS (Jersey/RESTEasy):**
```java
import javax.ws.rs.*;
import javax.ws.rs.core.Response;
import java.io.BufferedReader;
import java.util.stream.Collectors;

@Path("/api/SampleAPI")
public class TrackApplicationController {

    private static final DepartmentApiHandler handler;

    static {
        DepartmentConfiguration config = new DepartmentConfiguration(
            System.getenv("ENCRYPTION_KEY"),
            System.getenv("ENCRYPTION_IV")
        );
        config.setEnableLogging(true);

        handler = new DepartmentApiHandler(config, new MyDepartmentDataProvider());
    }

    @POST
    @Path("/sendappstatus_encrypted")
    @Consumes("application/json")
    @Produces("application/json")
    public Response sendApplicationStatusEncrypted(String requestBody) {
        DepartmentApiResponse response = handler.processRequest(requestBody);

        if (response.isSuccess()) {
            return Response.ok(response.getEncryptedData()).build();
        } else {
            return Response.status(response.getStatusCode())
                .entity(response.getErrorData())
                .build();
        }
    }
}
```

**Using Spring Boot:**
```java
import org.springframework.web.bind.annotation.*;
import javax.servlet.http.HttpServletRequest;
import java.io.BufferedReader;
import java.util.stream.Collectors;

@RestController
@RequestMapping("/api/SampleAPI")
public class TrackApplicationController {

    private final DepartmentApiHandler handler;

    public TrackApplicationController() {
        DepartmentConfiguration config = new DepartmentConfiguration(
            System.getenv("ENCRYPTION_KEY"),
            System.getenv("ENCRYPTION_IV")
        );
        config.setEnableLogging(true);

        this.handler = new DepartmentApiHandler(config, new MyDepartmentDataProvider());
    }

    @PostMapping("/sendappstatus_encrypted")
    public Object sendApplicationStatusEncrypted(HttpServletRequest request) throws Exception {
        String requestBody = request.getReader().lines().collect(Collectors.joining());
        DepartmentApiResponse response = handler.processRequest(requestBody);

        if (response.isSuccess()) {
            return response.getEncryptedData();
        } else {
            throw new RuntimeException(response.getErrorMessage());
        }
    }
}
```

## That's It!

The SDK handles:
✅ Request decryption
✅ Request validation
✅ Response validation
✅ Response encryption
✅ Error handling
✅ Logging

You just write your database logic!

## Features

- **Automatic Encryption/Decryption** - No crypto code needed
- **Request & Response Validation** - Ensures spec compliance
- **Error Handling** - Proper HTTP status codes
- **Helper Utilities** - Date formatting, status mapping
- **Type Safe** - Full type safety
- **Logging Support** - Configurable logging

## Configuration

```java
DepartmentConfiguration config = new DepartmentConfiguration(
    "encryption-key-24chars",
    "iv-8char"
);

config.setEnableLogging(true);      // Enable logging
config.setLogLevel(LogLevel.DEBUG); // Debug level

DepartmentApiHandler handler = new DepartmentApiHandler(
    config,
    new MyDepartmentDataProvider()
);
```

## Helper Utilities

### Date Formatting
```java
// Format LocalDateTime to API format
String formatted = ResponseHelper.formatDate(LocalDateTime.now());
// Returns: "05-Nov-2025,14:30:00"

// Parse API date string
LocalDateTime parsed = ResponseHelper.parseDate("05-Nov-2025,14:30:00");

// Handle nulls safely
String safe = ResponseHelper.emptyIfNull(possiblyNullString);
```

## Complete Example (Spring Boot)

```java
import org.springframework.stereotype.Service;
import org.springframework.web.bind.annotation.*;
import javax.annotation.PostConstruct;

@Service
class ApplicationService {
    private DepartmentApiHandler handler;

    @PostConstruct
    public void init() {
        DepartmentConfiguration config = new DepartmentConfiguration(
            System.getenv("ENCRYPTION_KEY"),
            System.getenv("ENCRYPTION_IV")
        );
        config.setEnableLogging(true);

        handler = new DepartmentApiHandler(config, new MyDepartmentDataProvider());
    }

    public DepartmentApiResponse processRequest(String requestBody) {
        return handler.processRequest(requestBody);
    }
}

@RestController
@RequestMapping("/api/SampleAPI")
class TrackApplicationController {

    private final ApplicationService service;

    public TrackApplicationController(ApplicationService service) {
        this.service = service;
    }

    @PostMapping("/sendappstatus_encrypted")
    public Object sendApplicationStatusEncrypted(@RequestBody String requestBody) {
        DepartmentApiResponse response = service.processRequest(requestBody);

        if (response.isSuccess()) {
            return response.getEncryptedData();
        } else {
            return ResponseEntity.status(response.getStatusCode())
                .body(response.getErrorData());
        }
    }
}
```

## Field Specifications

### Required Response Fields

| Field | Type | Example | Notes |
|-------|------|---------|-------|
| ApplicationID | String | "INC12345678" | From your database |
| ServiceName | String | "Income Certificate" | Localized based on language |
| ApplicantName | String | "Ramesh Kumar" | From your database |
| EstimatedDisbursalDays | int | 7 | Your business logic |
| ApplicationSubmissionDate | String | "05-Nov-2025,14:30:00" or "" | Use ResponseHelper.formatDate() |
| ApplicationPaymentDate | String | "06-Nov-2025,10:00:00" or "" | Use ResponseHelper.formatDate() |
| NextActionRequiredDetails | String | "Upload documents" or "" | From your database |
| FinalDecision | String | "0", "1", "2", or "" | Map your status |
| DepartmentRedirectionURL | String | "https://..." or "" | Optional URL |
| TotalNumberOfDesks | int | 3 | Your workflow config |
| CurrentDeskNumber | int | 2 | From your database |
| NextDeskNumber | int | 3 | Your business logic |
| DeskDetails | Array | [...] | Review history |

### FinalDecision Mapping

```java
// Your status → API value
if (app.getStatus().equals("APPROVED")) {
    response.FinalDecision = "0";
} else if (app.getStatus().equals("REJECTED")) {
    response.FinalDecision = "1";
} else {
    response.FinalDecision = "2"; // Pending
}
```

## Error Handling

The SDK automatically handles:
- 400 - Bad Request (invalid/malformed requests)
- 404 - Not Found (throw ApplicationNotFoundException)
- 500 - Internal Server Error (any other exception)

```java
@Override
public ApplicationStatusResponse getApplicationStatus(...)
        throws ApplicationNotFoundException {

    Application app = database.findApplication(applicationId);

    if (app == null) {
        throw new ApplicationNotFoundException(); // SDK returns 404
    }

    // Build and return response
    return response;
}
```

## Environment Variables

```bash
# .env or system environment
ENCRYPTION_KEY=your-24-character-key-here
ENCRYPTION_IV=your-8iv
```

## Files

- **DepartmentSDK.java** - Complete SDK (800+ lines)
- **README.md** - This file

## Support

- Check encryption keys are correct (24 chars key, 8 chars IV)
- Enable logging for debugging: `config.setEnableLogging(true)`
- Validate response format matches specification
- Use `ResponseHelper` for date formatting

## See Also

- C# Server SDK for comparison
- OpenAPI specification for complete API details
- Technical Integration documentation
