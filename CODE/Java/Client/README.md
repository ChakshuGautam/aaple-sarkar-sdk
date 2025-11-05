# Java Client SDK - For Aaple Sarkar Portal

Complete Java SDK for calling department APIs to retrieve application status.

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

    <!-- OkHttp for HTTP -->
    <dependency>
        <groupId>com.squareup.okhttp3</groupId>
        <artifactId>okhttp</artifactId>
        <version>4.12.0</version>
    </dependency>
</dependencies>
```

### Gradle
```gradle
dependencies {
    implementation 'com.google.code.gson:gson:2.10.1'
    implementation 'com.squareup.okhttp3:okhttp:4.12.0'
}
```

## Quick Start

### 1. Copy SDK to Your Project
```bash
cp TrackApplicationSDK.java src/main/java/gov/maharashtra/trackapplication/
```

### 2. Use in Your Code
```java
import gov.maharashtra.trackapplication.*;

public class Example {
    public static void main(String[] args) {
        // Initialize client
        TrackApplicationClient client = new TrackApplicationClient(
            "https://api.revenue.maharashtra.gov.in",
            "revenue-dept-key-24char",
            "rev-iv-8",
            "Revenue Department"
        );

        try {
            // Get application status
            ApplicationStatusResponse response = client.getApplicationStatus(
                "INC12345678",
                "4111",
                Language.ENGLISH
            );

            // Display results
            System.out.println("Service: " + response.getServiceName());
            System.out.println("Applicant: " + response.getApplicantName());
            System.out.println("Status: " + StatusHelper.getFinalDecisionText(
                response.getFinalDecision(),
                Language.ENGLISH
            ));
            System.out.println("Progress: " + response.getProgressPercentage() + "%");

        } catch (TrackApplicationException e) {
            System.err.println("Error: " + e.getMessage());
        } finally {
            client.close();
        }
    }
}
```

## Features

✅ **Automatic Encryption/Decryption** - TripleDES handled automatically
✅ **Retry Logic** - Configurable retry with exponential backoff
✅ **Type Safe** - Full type safety with proper Java classes
✅ **Helper Methods** - Date parsing, status translation, progress calculation
✅ **Error Handling** - Comprehensive exception hierarchy
✅ **Logging** - Built-in logging support

## Configuration

```java
ClientConfiguration config = new ClientConfiguration(
    "https://api.revenue.maharashtra.gov.in",
    "revenue-dept-key-24char",
    "rev-iv-8",
    "Revenue Department"
);

// Optional settings
config.setTimeout(60000);           // 60 seconds
config.setMaxRetries(5);            // 5 retry attempts
config.setRetryDelay(3000);         // 3 seconds between retries
config.setEnableLogging(true);      // Enable logging
config.setLogLevel(LogLevel.DEBUG); // Debug level logs

TrackApplicationClient client = new TrackApplicationClient(config);
```

## Advanced Usage

### Multi-Language Support
```java
// Get response in Marathi
ApplicationStatusResponse marathiResponse = client.getApplicationStatus(
    "INC12345678",
    "4111",
    Language.MARATHI
);

System.out.println(marathiResponse.getServiceName()); // "उत्पन्नाचा दाखला"
```

### Error Handling
```java
try {
    ApplicationStatusResponse response = client.getApplicationStatus(appId, serviceId);
    // Use response...

} catch (ValidationException e) {
    // Invalid request parameters
    System.err.println("Validation errors:");
    for (String error : e.getValidationErrors()) {
        System.err.println("  - " + error);
    }

} catch (EncryptionException e) {
    // Encryption failed - check keys
    System.err.println("Encryption error: " + e.getMessage());

} catch (ApiException e) {
    // API returned error
    if (e.getStatusCode() == 404) {
        System.err.println("Application not found");
    } else {
        System.err.println("API error: " + e.getMessage());
    }

} catch (TrackApplicationException e) {
    // Network or other error
    System.err.println("Error: " + e.getMessage());
}
```

### Helper Properties
```java
ApplicationStatusResponse response = client.getApplicationStatus(appId, serviceId);

// Check if paid
if (response.isPaid()) {
    System.out.println("Paid on: " + response.getApplicationPaymentDate());
}

// Check if action required
if (response.isActionRequired()) {
    System.out.println("Action: " + response.getNextActionRequiredDetails());
}

// Get progress
System.out.println("Progress: " + response.getProgressPercentage() + "%");

// Get status as enum
FinalDecisionStatus status = response.getFinalDecisionStatus();
if (status == FinalDecisionStatus.APPROVED) {
    System.out.println("Application approved!");
}
```

## Date Handling

```java
// Parse dates
LocalDateTime submittedDate = StatusHelper.parseDate(
    response.getApplicationSubmissionDate()
);

// Format dates
String formatted = StatusHelper.formatDate(LocalDateTime.now());
// Returns: "05-Nov-2025,14:30:00"
```

## Complete Example

See the C# examples for additional patterns that can be applied to Java:
- Web API integration (use Spring Boot)
- Background jobs (use Spring Scheduler or Quartz)
- Batch processing
- Dependency injection (use Spring)

## Files

- **TrackApplicationSDK.java** - Complete SDK (1000+ lines)
- **README.md** - This file

## Support

- Check encryption keys are correct (24 chars key, 8 chars IV)
- Enable logging for debugging: `config.setEnableLogging(true)`
- Verify department API endpoint is accessible
