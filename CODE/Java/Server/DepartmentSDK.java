// ============================================================================
// Maharashtra Government - Department Track Application API SDK (Java)
// Version: 1.0.0
// Compatible with: API V3 Specification (November 2025)
//
// This SDK helps departments quickly build their Track Application API
// that receives requests from Aaple Sarkar Portal.
//
// Installation:
//   1. Copy this file to your project
//   2. Add dependencies: Gson, javax.servlet (for web apps)
//   3. Implement DepartmentDataProvider interface
//   4. Use DepartmentApiHandler in your servlet/controller
//
// Usage:
//   DepartmentApiHandler handler = new DepartmentApiHandler(config, dataProvider);
//   DepartmentApiResponse response = handler.processRequest(requestBody);
//   // Return response to client
//
// ============================================================================

package gov.maharashtra.departmentapi;

import com.google.gson.*;

import javax.crypto.Cipher;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;
import java.nio.charset.StandardCharsets;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.CompletableFuture;

/**
 * Main handler for Department Track Application API
 *
 * Processes encrypted requests from Aaple Sarkar and returns encrypted responses.
 * Handles all encryption, validation, and error handling automatically.
 *
 * Example usage:
 * <pre>
 * // 1. Implement data provider
 * class MyDataProvider implements DepartmentDataProvider {
 *     public ApplicationStatusResponse getApplicationStatus(...) {
 *         // Your database logic here
 *         return response;
 *     }
 * }
 *
 * // 2. Create handler
 * DepartmentConfiguration config = new DepartmentConfiguration(
 *     "encryption-key-24chars",
 *     "iv-8char"
 * );
 * DepartmentApiHandler handler = new DepartmentApiHandler(config, new MyDataProvider());
 *
 * // 3. Process request in your servlet/controller
 * String requestBody = request.getReader().lines().collect(Collectors.joining());
 * DepartmentApiResponse response = handler.processRequest(requestBody);
 *
 * // 4. Return response
 * if (response.isSuccess()) {
 *     return Response.ok(response.getEncryptedData()).build();
 * } else {
 *     return Response.status(response.getStatusCode()).entity(response.getErrorData()).build();
 * }
 * </pre>
 */
public class DepartmentApiHandler {

    private final DepartmentConfiguration config;
    private final DepartmentDataProvider dataProvider;
    private final Gson gson;

    /**
     * Initialize Department API handler
     *
     * @param encryptionKey TripleDES encryption key (24 characters)
     * @param encryptionIV TripleDES encryption IV (8 characters)
     * @param dataProvider Your implementation of data provider
     */
    public DepartmentApiHandler(
            String encryptionKey,
            String encryptionIV,
            DepartmentDataProvider dataProvider) {
        this(new DepartmentConfiguration(encryptionKey, encryptionIV), dataProvider);
    }

    /**
     * Initialize Department API handler with full configuration
     */
    public DepartmentApiHandler(
            DepartmentConfiguration config,
            DepartmentDataProvider dataProvider) {

        if (config == null) {
            throw new IllegalArgumentException("config cannot be null");
        }

        if (dataProvider == null) {
            throw new IllegalArgumentException("dataProvider cannot be null");
        }

        if (config.getEncryptionKey() == null || config.getEncryptionKey().isEmpty()) {
            throw new IllegalArgumentException("EncryptionKey is required");
        }

        if (config.getEncryptionIV() == null || config.getEncryptionIV().isEmpty()) {
            throw new IllegalArgumentException("EncryptionIV is required");
        }

        this.config = config;
        this.dataProvider = dataProvider;
        this.gson = new GsonBuilder().serializeNulls().create();
    }

    /**
     * Process encrypted request from Aaple Sarkar
     * This is the main entry point - call this from your servlet/controller
     *
     * @param encryptedRequestBody The encrypted request body (JSON with "data" property)
     * @return Encrypted response ready to return to Aaple Sarkar
     */
    public DepartmentApiResponse processRequest(String encryptedRequestBody) {
        try {
            log("Processing incoming request from Aaple Sarkar");

            // Step 1: Parse encrypted request wrapper
            EncryptedRequest encryptedRequest;
            try {
                encryptedRequest = gson.fromJson(encryptedRequestBody, EncryptedRequest.class);
            } catch (Exception e) {
                logError("Failed to parse request JSON: " + e.getMessage());
                return createErrorResponse(400, "Invalid request format");
            }

            if (encryptedRequest == null || encryptedRequest.data == null || encryptedRequest.data.isEmpty()) {
                logError("Request does not contain encrypted data");
                return createErrorResponse(400, "Invalid request format");
            }

            // Step 2: Decrypt request
            String decryptedJson;
            try {
                decryptedJson = decrypt(encryptedRequest.data);
                logDebug("Request decrypted: " + decryptedJson);
            } catch (Exception e) {
                logError("Decryption failed: " + e.getMessage());
                return createErrorResponse(400, "Failed to decrypt request");
            }

            // Step 3: Parse decrypted request
            ApplicationStatusRequest request;
            try {
                request = gson.fromJson(decryptedJson, ApplicationStatusRequest.class);
            } catch (Exception e) {
                logError("Failed to parse decrypted JSON: " + e.getMessage());
                return createErrorResponse(400, "Invalid request format");
            }

            // Step 4: Validate request
            ValidationResult validation = validateRequest(request);
            if (!validation.isValid()) {
                logError("Request validation failed: " + String.join(", ", validation.getErrors()));
                return createErrorResponse(400, "Validation failed: " + String.join(", ", validation.getErrors()));
            }

            log("Request validated - AppID: " + request.AppID + ", ServiceID: " + request.ServiceID);

            // Step 5: Get application data from department's data provider
            ApplicationStatusResponse response;
            try {
                response = dataProvider.getApplicationStatus(
                    request.AppID,
                    request.ServiceID,
                    request.DeptName,
                    request.Language
                );
            } catch (ApplicationNotFoundException e) {
                log("Application not found: " + request.AppID);
                return createErrorResponse(404, e.getMessage() != null ? e.getMessage() : "Application not found");
            } catch (Exception e) {
                logError("Data provider error: " + e.getMessage());
                return createErrorResponse(500, "Internal server error");
            }

            // Step 6: Validate response
            ValidationResult responseValidation = validateResponse(response);
            if (!responseValidation.isValid()) {
                logError("Response validation failed: " + String.join(", ", responseValidation.getErrors()));
                return createErrorResponse(500, "Invalid response data");
            }

            // Step 7: Ensure empty strings for null values (API specification requirement)
            normalizeResponse(response);

            // Step 8: Serialize response to JSON
            String responseJson = gson.toJson(response);
            logDebug("Response JSON: " + responseJson);

            // Step 9: Encrypt response
            String encryptedResponse;
            try {
                encryptedResponse = encrypt(responseJson);
                logDebug("Response encrypted successfully");
            } catch (Exception e) {
                logError("Encryption failed: " + e.getMessage());
                return createErrorResponse(500, "Failed to encrypt response");
            }

            // Step 10: Return encrypted response
            log("Request processed successfully for AppID: " + request.AppID);

            EncryptedResponse encryptedResp = new EncryptedResponse();
            encryptedResp.data = encryptedResponse;

            DepartmentApiResponse apiResponse = new DepartmentApiResponse();
            apiResponse.statusCode = 200;
            apiResponse.success = true;
            apiResponse.encryptedData = encryptedResp;

            return apiResponse;

        } catch (Exception e) {
            logError("Unexpected error: " + e.getMessage());
            e.printStackTrace();
            return createErrorResponse(500, "An unexpected error occurred");
        }
    }

    /**
     * Validate a request without processing it
     */
    public ValidationResult validateRequest(ApplicationStatusRequest request) {
        ValidationResult result = new ValidationResult();

        if (request == null) {
            result.addError("Request cannot be null");
            return result;
        }

        if (request.AppID == null || request.AppID.trim().isEmpty()) {
            result.addError("AppID is required");
        }

        if (request.ServiceID == null || request.ServiceID.trim().isEmpty()) {
            result.addError("ServiceID is required");
        }

        if (request.DeptName == null || request.DeptName.trim().isEmpty()) {
            result.addError("DeptName is required");
        }

        if (request.Language == null || request.Language.trim().isEmpty()) {
            result.addError("Language is required");
        } else if (!request.Language.equals("EN") && !request.Language.equals("MR")) {
            result.addError("Language must be 'EN' or 'MR'");
        }

        return result;
    }

    /**
     * Validate a response before sending
     */
    public ValidationResult validateResponse(ApplicationStatusResponse response) {
        ValidationResult result = new ValidationResult();

        if (response == null) {
            result.addError("Response cannot be null");
            return result;
        }

        if (response.ApplicationID == null || response.ApplicationID.trim().isEmpty()) {
            result.addError("ApplicationID is required");
        }

        if (response.ServiceName == null || response.ServiceName.trim().isEmpty()) {
            result.addError("ServiceName is required");
        }

        if (response.ApplicantName == null || response.ApplicantName.trim().isEmpty()) {
            result.addError("ApplicantName is required");
        }

        // Validate FinalDecision values
        if (response.FinalDecision != null && !response.FinalDecision.isEmpty()) {
            if (!response.FinalDecision.equals("0") &&
                !response.FinalDecision.equals("1") &&
                !response.FinalDecision.equals("2")) {
                result.addError("FinalDecision must be '0', '1', '2', or empty string");
            }
        }

        // Validate date formats (if not empty)
        if (response.ApplicationSubmissionDate != null && !response.ApplicationSubmissionDate.isEmpty()) {
            if (!isValidDateFormat(response.ApplicationSubmissionDate)) {
                result.addError("ApplicationSubmissionDate must be in format DD-MMM-YYYY,HH:mm:ss");
            }
        }

        if (response.ApplicationPaymentDate != null && !response.ApplicationPaymentDate.isEmpty()) {
            if (!isValidDateFormat(response.ApplicationPaymentDate)) {
                result.addError("ApplicationPaymentDate must be in format DD-MMM-YYYY,HH:mm:ss");
            }
        }

        return result;
    }

    // ========================================================================
    // Encryption/Decryption (Matching Aaple Sarkar's implementation)
    // ========================================================================

    private String encrypt(String plainText) throws Exception {
        byte[] keyBytes = config.getEncryptionKey().getBytes(StandardCharsets.UTF_8);
        byte[] ivBytes = config.getEncryptionIV().getBytes(StandardCharsets.UTF_8);
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
    }

    private String decrypt(String cipherText) throws Exception {
        byte[] keyBytes = config.getEncryptionKey().getBytes(StandardCharsets.UTF_8);
        byte[] ivBytes = config.getEncryptionIV().getBytes(StandardCharsets.UTF_8);
        byte[] dataBytes = hexToBytes(cipherText);

        SecretKeySpec keySpec = new SecretKeySpec(keyBytes, "DESede");
        IvParameterSpec ivSpec = new IvParameterSpec(ivBytes);

        Cipher cipher = Cipher.getInstance("DESede/CBC/NoPadding");
        cipher.init(Cipher.DECRYPT_MODE, keySpec, ivSpec);

        byte[] decrypted = cipher.doFinal(dataBytes);
        String result = new String(decrypted, StandardCharsets.UTF_8);

        // Remove null padding
        return result.replaceAll("\\u0000+$", "");
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
    // Helper Methods
    // ========================================================================

    private void normalizeResponse(ApplicationStatusResponse response) {
        if (response.ApplicationSubmissionDate == null) response.ApplicationSubmissionDate = "";
        if (response.ApplicationPaymentDate == null) response.ApplicationPaymentDate = "";
        if (response.NextActionRequiredDetails == null) response.NextActionRequiredDetails = "";
        if (response.FinalDecision == null) response.FinalDecision = "";
        if (response.DepartmentRedirectionURL == null) response.DepartmentRedirectionURL = "";

        if (response.DeskDetails != null) {
            for (DeskDetail desk : response.DeskDetails) {
                if (desk.ReviewActionBy == null) desk.ReviewActionBy = "";
                if (desk.ReviewActionDateTime == null) desk.ReviewActionDateTime = "";
                if (desk.ReviewActionDetails == null) desk.ReviewActionDetails = "";
            }
        }
    }

    private boolean isValidDateFormat(String dateString) {
        try {
            LocalDateTime.parse(dateString, ResponseHelper.DATE_FORMAT);
            return true;
        } catch (Exception e) {
            return false;
        }
    }

    private DepartmentApiResponse createErrorResponse(int statusCode, String message) {
        DepartmentApiResponse response = new DepartmentApiResponse();
        response.statusCode = statusCode;
        response.success = false;
        response.errorMessage = message;

        JsonObject errorData = new JsonObject();
        errorData.addProperty("error", message);
        errorData.addProperty("timestamp", LocalDateTime.now().toString());
        response.errorData = errorData;

        return response;
    }

    // ========================================================================
    // Logging
    // ========================================================================

    private void log(String message) {
        if (config.isEnableLogging()) {
            System.out.println("[DepartmentSDK] " + message);
        }
    }

    private void logDebug(String message) {
        if (config.isEnableLogging() && config.getLogLevel() == LogLevel.DEBUG) {
            System.out.println("[DepartmentSDK][DEBUG] " + message);
        }
    }

    private void logError(String message) {
        if (config.isEnableLogging()) {
            System.err.println("[DepartmentSDK][ERROR] " + message);
        }
    }
}

// ============================================================================
// Data Provider Interface
// ============================================================================

/**
 * Interface that departments must implement to provide application data
 */
interface DepartmentDataProvider {
    /**
     * Get application status from your system
     *
     * @param applicationId Application ID from the request
     * @param serviceId Service ID from the request
     * @param departmentName Department name from the request
     * @param language Language code: "EN" or "MR"
     * @return Application status response
     * @throws ApplicationNotFoundException when application is not found
     */
    ApplicationStatusResponse getApplicationStatus(
        String applicationId,
        String serviceId,
        String departmentName,
        String language
    ) throws ApplicationNotFoundException;
}

// ============================================================================
// Configuration
// ============================================================================

/**
 * Configuration for Department API handler
 */
class DepartmentConfiguration {
    private String encryptionKey;
    private String encryptionIV;
    private boolean enableLogging = false;
    private LogLevel logLevel = LogLevel.INFO;

    public DepartmentConfiguration(String encryptionKey, String encryptionIV) {
        this.encryptionKey = encryptionKey;
        this.encryptionIV = encryptionIV;
    }

    public String getEncryptionKey() { return encryptionKey; }
    public void setEncryptionKey(String encryptionKey) { this.encryptionKey = encryptionKey; }

    public String getEncryptionIV() { return encryptionIV; }
    public void setEncryptionIV(String encryptionIV) { this.encryptionIV = encryptionIV; }

    public boolean isEnableLogging() { return enableLogging; }
    public void setEnableLogging(boolean enableLogging) { this.enableLogging = enableLogging; }

    public LogLevel getLogLevel() { return logLevel; }
    public void setLogLevel(LogLevel logLevel) { this.logLevel = logLevel; }
}

enum LogLevel {
    INFO,
    DEBUG
}

// ============================================================================
// Response Models
// ============================================================================

/**
 * Response from Department API handler
 */
class DepartmentApiResponse {
    int statusCode;
    boolean success;
    EncryptedResponse encryptedData;
    String errorMessage;
    JsonObject errorData;

    public int getStatusCode() { return statusCode; }
    public boolean isSuccess() { return success; }
    public EncryptedResponse getEncryptedData() { return encryptedData; }
    public String getErrorMessage() { return errorMessage; }
    public JsonObject getErrorData() { return errorData; }
}

// ============================================================================
// Request/Response Models (Matching V3 Specification)
// ============================================================================

class EncryptedRequest {
    @SerializedName("data")
    String data;
}

class EncryptedResponse {
    @SerializedName("data")
    String data;
}

class ApplicationStatusRequest {
    @SerializedName("AppID")
    String AppID;

    @SerializedName("ServiceID")
    String ServiceID;

    @SerializedName("DeptName")
    String DeptName;

    @SerializedName("Language")
    String Language;
}

class ApplicationStatusResponse {
    @SerializedName("ApplicationID")
    String ApplicationID;

    @SerializedName("ServiceName")
    String ServiceName;

    @SerializedName("ApplicantName")
    String ApplicantName;

    @SerializedName("EstimatedDisbursalDays")
    int EstimatedDisbursalDays;

    @SerializedName("ApplicationSubmissionDate")
    String ApplicationSubmissionDate;

    @SerializedName("ApplicationPaymentDate")
    String ApplicationPaymentDate;

    @SerializedName("NextActionRequiredDetails")
    String NextActionRequiredDetails;

    @SerializedName("FinalDecision")
    String FinalDecision;

    @SerializedName("DepartmentRedirectionURL")
    String DepartmentRedirectionURL;

    @SerializedName("TotalNumberOfDesks")
    int TotalNumberOfDesks;

    @SerializedName("CurrentDeskNumber")
    int CurrentDeskNumber;

    @SerializedName("NextDeskNumber")
    int NextDeskNumber;

    @SerializedName("DeskDetails")
    DeskDetail[] DeskDetails;
}

class DeskDetail {
    @SerializedName("DeskNumber")
    String DeskNumber;

    @SerializedName("ReviewActionBy")
    String ReviewActionBy;

    @SerializedName("ReviewActionDateTime")
    String ReviewActionDateTime;

    @SerializedName("ReviewActionDetails")
    String ReviewActionDetails;
}

// ============================================================================
// Validation
// ============================================================================

class ValidationResult {
    private List<String> errors = new ArrayList<>();

    boolean isValid() {
        return errors.isEmpty();
    }

    List<String> getErrors() {
        return errors;
    }

    void addError(String error) {
        errors.add(error);
    }
}

// ============================================================================
// Exceptions
// ============================================================================

class ApplicationNotFoundException extends Exception {
    public ApplicationNotFoundException() {
        super("Application not found");
    }

    public ApplicationNotFoundException(String message) {
        super(message);
    }
}

// ============================================================================
// Helper Utilities
// ============================================================================

class ResponseHelper {
    static final DateTimeFormatter DATE_FORMAT =
        DateTimeFormatter.ofPattern("dd-MMM-yyyy,HH:mm:ss");

    /**
     * Format LocalDateTime to API date string (DD-MMM-YYYY,HH:mm:ss)
     */
    public static String formatDate(LocalDateTime dateTime) {
        if (dateTime == null) {
            return "";
        }
        return dateTime.format(DATE_FORMAT);
    }

    /**
     * Parse API date string to LocalDateTime
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
     * Ensure string is never null (convert to empty string)
     */
    public static String emptyIfNull(String value) {
        return value != null ? value : "";
    }
}
