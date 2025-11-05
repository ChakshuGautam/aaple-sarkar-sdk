// ============================================================================
// Maharashtra Government - Track Application Status API SDK (Java)
// Version: 1.0.0
// Compatible with: API V3 Specification (November 2025)
//
// Installation:
//   1. Copy this file to your project
//   2. Add dependencies to pom.xml or build.gradle:
//      - com.google.code.gson:gson:2.10.1
//      - com.squareup.okhttp3:okhttp:4.12.0
//
// Usage:
//   TrackApplicationClient client = new TrackApplicationClient(
//       baseUrl, encryptKey, encryptIV, deptName
//   );
//   ApplicationStatusResponse response = client.getApplicationStatus(
//       "INC12345678", "4111"
//   );
//   System.out.println(response.getServiceName());
//
// ============================================================================

package gov.maharashtra.trackapplication;

import com.google.gson.*;
import okhttp3.*;

import javax.crypto.Cipher;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.TimeUnit;

/**
 * Main client for Track Application Status API
 *
 * This SDK handles:
 * - TripleDES encryption/decryption
 * - HTTP communication with retry logic
 * - Request/response validation
 * - Error handling
 *
 * Example usage:
 * <pre>
 * TrackApplicationClient client = new TrackApplicationClient(
 *     "https://api.revenue.maharashtra.gov.in",
 *     "revenue-dept-key-24char",
 *     "rev-iv-8",
 *     "Revenue Department"
 * );
 *
 * ApplicationStatusResponse response = client.getApplicationStatus(
 *     "INC12345678",
 *     "4111",
 *     Language.ENGLISH
 * );
 *
 * System.out.println("Status: " + response.getServiceName());
 * </pre>
 */
public class TrackApplicationClient implements AutoCloseable {

    private final OkHttpClient httpClient;
    private final String encryptionKey;
    private final String encryptionIV;
    private final String departmentName;
    private final ClientConfiguration config;
    private final Gson gson;

    /**
     * Initialize Track Application API client
     *
     * @param apiBaseUrl Base URL of the API (e.g., https://api.maharashtra.gov.in)
     * @param encryptionKey TripleDES encryption key (24 characters)
     * @param encryptionIV TripleDES encryption IV (8 characters)
     * @param departmentName Department name (e.g., "Revenue Department")
     */
    public TrackApplicationClient(
            String apiBaseUrl,
            String encryptionKey,
            String encryptionIV,
            String departmentName) {
        this(new ClientConfiguration(apiBaseUrl, encryptionKey, encryptionIV, departmentName));
    }

    /**
     * Initialize Track Application API client with configuration
     */
    public TrackApplicationClient(ClientConfiguration config) {
        if (config == null) {
            throw new IllegalArgumentException("config cannot be null");
        }

        if (config.getApiBaseUrl() == null || config.getApiBaseUrl().isEmpty()) {
            throw new IllegalArgumentException("ApiBaseUrl is required");
        }

        if (config.getEncryptionKey() == null || config.getEncryptionKey().isEmpty()) {
            throw new IllegalArgumentException("EncryptionKey is required");
        }

        if (config.getEncryptionIV() == null || config.getEncryptionIV().isEmpty()) {
            throw new IllegalArgumentException("EncryptionIV is required");
        }

        if (config.getDepartmentName() == null || config.getDepartmentName().isEmpty()) {
            throw new IllegalArgumentException("DepartmentName is required");
        }

        this.config = config;
        this.encryptionKey = config.getEncryptionKey();
        this.encryptionIV = config.getEncryptionIV();
        this.departmentName = config.getDepartmentName();

        this.httpClient = new OkHttpClient.Builder()
            .connectTimeout(config.getTimeout(), TimeUnit.MILLISECONDS)
            .readTimeout(config.getTimeout(), TimeUnit.MILLISECONDS)
            .writeTimeout(config.getTimeout(), TimeUnit.MILLISECONDS)
            .addInterceptor(chain -> {
                Request request = chain.request().newBuilder()
                    .addHeader("User-Agent", "TrackApplicationSDK-Java/1.0")
                    .build();
                return chain.proceed(request);
            })
            .build();

        this.gson = new GsonBuilder()
            .serializeNulls()
            .create();
    }

    /**
     * Get application status by ID
     *
     * @param applicationId Application ID (e.g., "INC12345678")
     * @param serviceId Service ID (e.g., "4111")
     * @return Application status details
     * @throws TrackApplicationException if request fails
     */
    public ApplicationStatusResponse getApplicationStatus(
            String applicationId,
            String serviceId) throws TrackApplicationException {
        return getApplicationStatus(applicationId, serviceId, Language.ENGLISH);
    }

    /**
     * Get application status by ID with language preference
     *
     * @param applicationId Application ID (e.g., "INC12345678")
     * @param serviceId Service ID (e.g., "4111")
     * @param language Response language (EN or MR)
     * @return Application status details
     * @throws TrackApplicationException if request fails
     */
    public ApplicationStatusResponse getApplicationStatus(
            String applicationId,
            String serviceId,
            Language language) throws TrackApplicationException {

        ApplicationStatusRequest request = new ApplicationStatusRequest();
        request.setAppID(applicationId);
        request.setServiceID(serviceId);
        request.setDeptName(departmentName);
        request.setLanguage(language == Language.ENGLISH ? "EN" : "MR");

        return getApplicationStatus(request);
    }

    /**
     * Get application status with full request object
     */
    public ApplicationStatusResponse getApplicationStatus(
            ApplicationStatusRequest request) throws TrackApplicationException {

        if (request == null) {
            throw new IllegalArgumentException("request cannot be null");
        }

        // Validate request
        ValidationResult validation = validateRequest(request);
        if (!validation.isValid()) {
            throw new ValidationException(
                "Request validation failed",
                validation.getErrors()
            );
        }

        log("Requesting status for application: " + request.getAppID());

        // Retry logic
        Exception lastException = null;

        for (int attempt = 0; attempt <= config.getMaxRetries(); attempt++) {
            try {
                // Serialize request to JSON
                String requestJson = gson.toJson(request);
                logDebug("Request JSON: " + requestJson);

                // Encrypt request
                String encryptedRequest = encrypt(requestJson);
                logDebug("Request encrypted successfully");

                // Create encrypted payload
                JsonObject payload = new JsonObject();
                payload.addProperty("data", encryptedRequest);
                String payloadJson = gson.toJson(payload);

                // Build URL
                String url = config.getApiBaseUrl();
                if (!url.endsWith("/")) {
                    url += "/";
                }
                url += config.getApiEndpoint();

                // Send HTTP request
                RequestBody body = RequestBody.create(
                    payloadJson,
                    MediaType.parse("application/json; charset=utf-8")
                );

                Request httpRequest = new Request.Builder()
                    .url(url)
                    .post(body)
                    .build();

                Response httpResponse = httpClient.newCall(httpRequest).execute();
                logDebug("HTTP response: " + httpResponse.code());

                String responseContent = httpResponse.body().string();

                // Check for HTTP errors
                if (!httpResponse.isSuccessful()) {
                    throw new ApiException(
                        "API returned error status " + httpResponse.code(),
                        httpResponse.code(),
                        responseContent
                    );
                }

                // Parse encrypted response
                JsonObject encryptedResponse = gson.fromJson(responseContent, JsonObject.class);

                if (encryptedResponse == null || !encryptedResponse.has("data")) {
                    throw new ApiException(
                        "Response does not contain encrypted data",
                        httpResponse.code(),
                        responseContent
                    );
                }

                // Decrypt response
                String decryptedJson = decrypt(encryptedResponse.get("data").getAsString());
                logDebug("Response decrypted: " + decryptedJson);

                // Parse response
                ApplicationStatusResponse response = gson.fromJson(
                    decryptedJson,
                    ApplicationStatusResponse.class
                );

                if (response == null) {
                    throw new ApiException(
                        "Failed to parse API response",
                        httpResponse.code(),
                        decryptedJson
                    );
                }

                log("Status retrieved successfully for " + request.getAppID());
                httpResponse.close();
                return response;

            } catch (EncryptionException | DecryptionException | ValidationException e) {
                // Don't retry these errors
                throw e;
            } catch (IOException e) {
                lastException = e;
                logError("HTTP request failed (attempt " + (attempt + 1) + "): " + e.getMessage());
            } catch (Exception e) {
                lastException = e;
                logError("Unexpected error (attempt " + (attempt + 1) + "): " + e.getMessage());
            }

            // Wait before retry
            if (attempt < config.getMaxRetries()) {
                long delayMs = config.getRetryDelay() * (attempt + 1);
                log("Retrying in " + (delayMs / 1000.0) + " seconds...");
                try {
                    Thread.sleep(delayMs);
                } catch (InterruptedException e) {
                    Thread.currentThread().interrupt();
                    throw new TrackApplicationException("Request interrupted", e);
                }
            }
        }

        // All retries failed
        throw new TrackApplicationException(
            "Request failed after " + (config.getMaxRetries() + 1) + " attempts",
            lastException
        );
    }

    /**
     * Validate request without sending
     */
    public ValidationResult validateRequest(ApplicationStatusRequest request) {
        ValidationResult result = new ValidationResult();

        if (request == null) {
            result.addError("Request cannot be null");
            return result;
        }

        if (request.getAppID() == null || request.getAppID().trim().isEmpty()) {
            result.addError("AppID is required");
        }

        if (request.getServiceID() == null || request.getServiceID().trim().isEmpty()) {
            result.addError("ServiceID is required");
        }

        if (request.getDeptName() == null || request.getDeptName().trim().isEmpty()) {
            result.addError("DeptName is required");
        }

        if (request.getLanguage() == null || request.getLanguage().trim().isEmpty()) {
            result.addError("Language is required");
        } else if (!request.getLanguage().equals("EN") && !request.getLanguage().equals("MR")) {
            result.addError("Language must be 'EN' or 'MR'");
        }

        return result;
    }

    // ========================================================================
    // Encryption/Decryption (Matching API specification)
    // ========================================================================

    private String encrypt(String plainText) throws EncryptionException {
        try {
            byte[] keyBytes = encryptionKey.getBytes(StandardCharsets.UTF_8);
            byte[] ivBytes = encryptionIV.getBytes(StandardCharsets.UTF_8);
            byte[] dataBytes = plainText.getBytes(StandardCharsets.UTF_8);

            SecretKeySpec keySpec = new SecretKeySpec(keyBytes, "DESede");
            IvParameterSpec ivSpec = new IvParameterSpec(ivBytes);

            Cipher cipher = Cipher.getInstance("DESede/CBC/NoPadding");
            cipher.init(Cipher.ENCRYPT_MODE, keySpec, ivSpec);

            // Pad data to block size
            int blockSize = cipher.getBlockSize();
            int paddedLength = ((dataBytes.length + blockSize - 1) / blockSize) * blockSize;
            byte[] paddedData = new byte[paddedLength];
            System.arraycopy(dataBytes, 0, paddedData, 0, dataBytes.length);

            byte[] encrypted = cipher.doFinal(paddedData);
            return bytesToHex(encrypted);

        } catch (Exception e) {
            throw new EncryptionException("Failed to encrypt request data", e);
        }
    }

    private String decrypt(String cipherText) throws DecryptionException {
        try {
            byte[] keyBytes = encryptionKey.getBytes(StandardCharsets.UTF_8);
            byte[] ivBytes = encryptionIV.getBytes(StandardCharsets.UTF_8);
            byte[] dataBytes = hexToBytes(cipherText);

            SecretKeySpec keySpec = new SecretKeySpec(keyBytes, "DESede");
            IvParameterSpec ivSpec = new IvParameterSpec(ivBytes);

            Cipher cipher = Cipher.getInstance("DESede/CBC/NoPadding");
            cipher.init(Cipher.DECRYPT_MODE, keySpec, ivSpec);

            byte[] decrypted = cipher.doFinal(dataBytes);
            String result = new String(decrypted, StandardCharsets.UTF_8);

            // Remove null padding
            return result.replaceAll("\\u0000+$", "");

        } catch (Exception e) {
            throw new DecryptionException("Failed to decrypt response data", e);
        }
    }

    private String bytesToHex(byte[] bytes) {
        StringBuilder hex = new StringBuilder();
        for (byte b : bytes) {
            hex.append(String.format("%02X", b));
        }
        return hex.toString();
    }

    private byte[] hexToBytes(String hex) {
        int len = hex.length();
        byte[] data = new byte[len / 2];
        for (int i = 0; i < len; i += 2) {
            data[i / 2] = (byte) ((Character.digit(hex.charAt(i), 16) << 4)
                                + Character.digit(hex.charAt(i + 1), 16));
        }
        return data;
    }

    // ========================================================================
    // Logging
    // ========================================================================

    private void log(String message) {
        if (config.isEnableLogging()) {
            System.out.println("[TrackApplicationSDK] " + message);
        }
    }

    private void logDebug(String message) {
        if (config.isEnableLogging() && config.getLogLevel() == LogLevel.DEBUG) {
            System.out.println("[TrackApplicationSDK][DEBUG] " + message);
        }
    }

    private void logError(String message) {
        if (config.isEnableLogging()) {
            System.err.println("[TrackApplicationSDK][ERROR] " + message);
        }
    }

    @Override
    public void close() {
        // OkHttp client resources are managed internally
        httpClient.dispatcher().executorService().shutdown();
        httpClient.connectionPool().evictAll();
    }
}

// ============================================================================
// Configuration
// ============================================================================

/**
 * Configuration for Track Application API client
 */
class ClientConfiguration {
    private String apiBaseUrl;
    private String apiEndpoint = "api/SampleAPI/sendappstatus_encrypted";
    private String encryptionKey;
    private String encryptionIV;
    private String departmentName;
    private long timeout = 30000; // 30 seconds
    private int maxRetries = 3;
    private long retryDelay = 2000; // 2 seconds
    private boolean enableLogging = false;
    private LogLevel logLevel = LogLevel.INFO;

    public ClientConfiguration(
            String apiBaseUrl,
            String encryptionKey,
            String encryptionIV,
            String departmentName) {
        this.apiBaseUrl = apiBaseUrl;
        this.encryptionKey = encryptionKey;
        this.encryptionIV = encryptionIV;
        this.departmentName = departmentName;
    }

    // Getters and setters
    public String getApiBaseUrl() { return apiBaseUrl; }
    public void setApiBaseUrl(String apiBaseUrl) { this.apiBaseUrl = apiBaseUrl; }

    public String getApiEndpoint() { return apiEndpoint; }
    public void setApiEndpoint(String apiEndpoint) { this.apiEndpoint = apiEndpoint; }

    public String getEncryptionKey() { return encryptionKey; }
    public void setEncryptionKey(String encryptionKey) { this.encryptionKey = encryptionKey; }

    public String getEncryptionIV() { return encryptionIV; }
    public void setEncryptionIV(String encryptionIV) { this.encryptionIV = encryptionIV; }

    public String getDepartmentName() { return departmentName; }
    public void setDepartmentName(String departmentName) { this.departmentName = departmentName; }

    public long getTimeout() { return timeout; }
    public void setTimeout(long timeout) { this.timeout = timeout; }

    public int getMaxRetries() { return maxRetries; }
    public void setMaxRetries(int maxRetries) { this.maxRetries = maxRetries; }

    public long getRetryDelay() { return retryDelay; }
    public void setRetryDelay(long retryDelay) { this.retryDelay = retryDelay; }

    public boolean isEnableLogging() { return enableLogging; }
    public void setEnableLogging(boolean enableLogging) { this.enableLogging = enableLogging; }

    public LogLevel getLogLevel() { return logLevel; }
    public void setLogLevel(LogLevel logLevel) { this.logLevel = logLevel; }
}

/**
 * Log level enumeration
 */
enum LogLevel {
    INFO,
    DEBUG
}

// ============================================================================
// Request/Response Models (Matching V3 Specification)
// ============================================================================

/**
 * Request to get application status
 */
class ApplicationStatusRequest {
    @SerializedName("AppID")
    private String AppID;

    @SerializedName("ServiceID")
    private String ServiceID;

    @SerializedName("DeptName")
    private String DeptName;

    @SerializedName("Language")
    private String Language;

    // Getters and setters
    public String getAppID() { return AppID; }
    public void setAppID(String appID) { AppID = appID; }

    public String getServiceID() { return ServiceID; }
    public void setServiceID(String serviceID) { ServiceID = serviceID; }

    public String getDeptName() { return DeptName; }
    public void setDeptName(String deptName) { DeptName = deptName; }

    public String getLanguage() { return Language; }
    public void setLanguage(String language) { Language = language; }
}

/**
 * Application status response matching V3 specification
 */
class ApplicationStatusResponse {
    @SerializedName("ApplicationID")
    private String ApplicationID;

    @SerializedName("ServiceName")
    private String ServiceName;

    @SerializedName("ApplicantName")
    private String ApplicantName;

    @SerializedName("EstimatedDisbursalDays")
    private int EstimatedDisbursalDays;

    @SerializedName("ApplicationSubmissionDate")
    private String ApplicationSubmissionDate;

    @SerializedName("ApplicationPaymentDate")
    private String ApplicationPaymentDate;

    @SerializedName("NextActionRequiredDetails")
    private String NextActionRequiredDetails;

    @SerializedName("FinalDecision")
    private String FinalDecision;

    @SerializedName("DepartmentRedirectionURL")
    private String DepartmentRedirectionURL;

    @SerializedName("TotalNumberOfDesks")
    private int TotalNumberOfDesks;

    @SerializedName("CurrentDeskNumber")
    private int CurrentDeskNumber;

    @SerializedName("NextDeskNumber")
    private int NextDeskNumber;

    @SerializedName("DeskDetails")
    private DeskDetail[] DeskDetails;

    // Getters
    public String getApplicationID() { return ApplicationID; }
    public String getServiceName() { return ServiceName; }
    public String getApplicantName() { return ApplicantName; }
    public int getEstimatedDisbursalDays() { return EstimatedDisbursalDays; }
    public String getApplicationSubmissionDate() { return ApplicationSubmissionDate; }
    public String getApplicationPaymentDate() { return ApplicationPaymentDate; }
    public String getNextActionRequiredDetails() { return NextActionRequiredDetails; }
    public String getFinalDecision() { return FinalDecision; }
    public String getDepartmentRedirectionURL() { return DepartmentRedirectionURL; }
    public int getTotalNumberOfDesks() { return TotalNumberOfDesks; }
    public int getCurrentDeskNumber() { return CurrentDeskNumber; }
    public int getNextDeskNumber() { return NextDeskNumber; }
    public DeskDetail[] getDeskDetails() { return DeskDetails; }

    // Helper methods
    public boolean isPaid() {
        return ApplicationPaymentDate != null && !ApplicationPaymentDate.isEmpty();
    }

    public boolean isActionRequired() {
        return NextActionRequiredDetails != null && !NextActionRequiredDetails.isEmpty();
    }

    public boolean isFinalDecisionMade() {
        return FinalDecision != null && !FinalDecision.isEmpty();
    }

    public FinalDecisionStatus getFinalDecisionStatus() {
        if (FinalDecision == null || FinalDecision.isEmpty()) {
            return null;
        }
        switch (FinalDecision) {
            case "0": return FinalDecisionStatus.APPROVED;
            case "1": return FinalDecisionStatus.REJECTED;
            case "2": return FinalDecisionStatus.PENDING;
            default: return null;
        }
    }

    public int getProgressPercentage() {
        if (TotalNumberOfDesks <= 0) {
            return 0;
        }
        int completedDesks = DeskDetails != null ? DeskDetails.length : 0;
        return (int) Math.round((double) completedDesks / TotalNumberOfDesks * 100);
    }
}

/**
 * Desk review details
 */
class DeskDetail {
    @SerializedName("DeskNumber")
    private String DeskNumber;

    @SerializedName("ReviewActionBy")
    private String ReviewActionBy;

    @SerializedName("ReviewActionDateTime")
    private String ReviewActionDateTime;

    @SerializedName("ReviewActionDetails")
    private String ReviewActionDetails;

    public String getDeskNumber() { return DeskNumber; }
    public String getReviewActionBy() { return ReviewActionBy; }
    public String getReviewActionDateTime() { return ReviewActionDateTime; }
    public String getReviewActionDetails() { return ReviewActionDetails; }
}

// ============================================================================
// Enums
// ============================================================================

/**
 * Response language
 */
enum Language {
    ENGLISH,
    MARATHI
}

/**
 * Final decision status
 */
enum FinalDecisionStatus {
    APPROVED,
    REJECTED,
    PENDING
}

// ============================================================================
// Validation
// ============================================================================

/**
 * Validation result
 */
class ValidationResult {
    private List<String> errors = new ArrayList<>();

    public boolean isValid() {
        return errors.isEmpty();
    }

    public List<String> getErrors() {
        return errors;
    }

    void addError(String error) {
        errors.add(error);
    }
}

// ============================================================================
// Exceptions
// ============================================================================

/**
 * Base exception for Track Application API
 */
class TrackApplicationException extends Exception {
    public TrackApplicationException(String message) {
        super(message);
    }

    public TrackApplicationException(String message, Throwable cause) {
        super(message, cause);
    }
}

/**
 * Request validation exception
 */
class ValidationException extends TrackApplicationException {
    private final List<String> validationErrors;

    public ValidationException(String message, List<String> errors) {
        super(message);
        this.validationErrors = errors;
    }

    public List<String> getValidationErrors() {
        return validationErrors;
    }
}

/**
 * Encryption exception
 */
class EncryptionException extends TrackApplicationException {
    public EncryptionException(String message, Throwable cause) {
        super(message, cause);
    }
}

/**
 * Decryption exception
 */
class DecryptionException extends TrackApplicationException {
    public DecryptionException(String message, Throwable cause) {
        super(message, cause);
    }
}

/**
 * API error exception
 */
class ApiException extends TrackApplicationException {
    private final int statusCode;
    private final String responseContent;

    public ApiException(String message, int statusCode, String responseContent) {
        super(message);
        this.statusCode = statusCode;
        this.responseContent = responseContent;
    }

    public int getStatusCode() {
        return statusCode;
    }

    public String getResponseContent() {
        return responseContent;
    }
}

// ============================================================================
// Helper Utilities
// ============================================================================

/**
 * Helper utilities for working with application status
 */
class StatusHelper {
    private static final DateTimeFormatter DATE_FORMAT =
        DateTimeFormatter.ofPattern("dd-MMM-yyyy,HH:mm:ss");

    /**
     * Get user-friendly status text for final decision
     */
    public static String getFinalDecisionText(String finalDecision, Language language) {
        if (finalDecision == null || finalDecision.isEmpty()) {
            return language == Language.ENGLISH ? "Pending" : "प्रलंबित";
        }

        switch (finalDecision) {
            case "0":
                return language == Language.ENGLISH ? "Approved" : "मंजूर";
            case "1":
                return language == Language.ENGLISH ? "Rejected" : "नाकारले";
            case "2":
                return language == Language.ENGLISH ? "Pending" : "प्रलंबित";
            default:
                return language == Language.ENGLISH ? "Unknown" : "अज्ञात";
        }
    }

    /**
     * Parse date string to LocalDateTime
     */
    public static LocalDateTime parseDate(String dateString) {
        if (dateString == null || dateString.trim().isEmpty()) {
            return null;
        }

        try {
            return LocalDateTime.parse(dateString, DATE_FORMAT);
        } catch (Exception e) {
            return null;
        }
    }

    /**
     * Format LocalDateTime to API date string
     */
    public static String formatDate(LocalDateTime dateTime) {
        if (dateTime == null) {
            return "";
        }
        return dateTime.format(DATE_FORMAT);
    }
}
