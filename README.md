# Aaple Sarkar SDK

Complete SDK for integrating with Maharashtra Government's Aaple Sarkar Track Application Status API.

## Overview

This repository contains **complete production-ready SDKs in both C# and Java** for both sides of the integration:

- **For Aaple Sarkar Team**: Client SDK to call department APIs and retrieve application status
- **For Departments**: Server SDK to receive requests and provide application status
- **Languages**: Full implementation in C# and Java

## Quick Links

- **📖 [For Aaple Sarkar Team](GUIDES/For-Aaple-Sarkar.md)** - Client SDK guide (3 steps)
- **🏛️ [For Departments](GUIDES/For-Departments.md)** - Server SDK guide (3 steps)
- **📄 [OpenAPI Specification](openapi.yaml)** - API specification
- **📘 [Technical Integration V3.3](TECHNICAL_INTEGRATION_V3.3.md)** - Complete technical documentation

## Structure

```
.
├── README.md                           # This file
├── openapi.yaml                        # OpenAPI 3.0 specification
├── TECHNICAL_INTEGRATION_V3.3.md      # Complete technical documentation
│
├── GUIDES/
│   ├── For-Aaple-Sarkar.md            # Client SDK integration guide
│   └── For-Departments.md             # Server SDK implementation guide
│
└── CODE/
    │
    ├── CSharp/                         # C# Implementation
    │   ├── Client/                     # For Aaple Sarkar Portal
    │   │   ├── TrackApplicationSDK.cs              # Client SDK (870 lines)
    │   │   └── TrackApplicationSDK-Examples.cs     # Usage examples (500 lines)
    │   │
    │   └── Server/                     # For Departments
    │       ├── DepartmentSDK.cs                    # Server SDK (900 lines)
    │       ├── DepartmentSDK-Examples.cs           # Usage examples (1000 lines)
    │       ├── DepartmentAPI-Template.cs           # Server template (600 lines)
    │       └── DepartmentAPI-Validator.cs          # Testing tool (800 lines)
    │
    └── Java/                           # Java Implementation ⭐ NEW
        ├── Client/                     # For Aaple Sarkar Portal
        │   ├── TrackApplicationSDK.java            # Client SDK (1000+ lines)
        │   └── README.md                           # Java client guide
        │
        └── Server/                     # For Departments
            ├── DepartmentSDK.java                  # Server SDK (800+ lines)
            └── README.md                           # Java server guide
```

## What's New

### Complete Java SDKs ⭐ NEW

We've added **full Java implementations** of both Client and Server SDKs:

**Java Client SDK** (for Aaple Sarkar Portal):
- Complete HTTP client using OkHttp
- TripleDES encryption/decryption
- Retry logic with exponential backoff
- Comprehensive error handling
- Helper utilities and validation

**Java Server SDK** (for Departments):
- Server-side request handler
- Clean `DepartmentDataProvider` interface
- Automatic encryption/decryption
- Request/response validation
- Works with Spring Boot, JAX-RS, or any Java web framework

### Department Server SDK ⭐

Both C# and Java now have **complete server-side SDKs** for departments:

**Before (Template):**
```csharp
// You had to manage encryption, validation, errors manually
```

**Now (C# SDK):**
```csharp
public class MyDataProvider : IDepartmentDataProvider
{
    public async Task<ApplicationStatusResponse> GetApplicationStatusAsync(...)
    {
        // Your database logic here
        return response;
    }
}
```

**Now (Java SDK):**
```java
public class MyDataProvider implements DepartmentDataProvider {
    public ApplicationStatusResponse getApplicationStatus(...) {
        // Your database logic here
        return response;
    }
}
```

**SDK handles everything else!** Encryption, validation, error handling, logging - all automatic.

## Language Support

| Component | C# | Java |
|-----------|-----|------|
| **Client SDK** (Aaple Sarkar) | ✅ Complete | ✅ Complete |
| **Server SDK** (Departments) | ✅ Complete | ✅ Complete |
| **Examples** | ✅ 10+ examples | ✅ READMEs with examples |
| **Templates** | ✅ Available | - |
| **Validator** | ✅ Available | - |

Choose the language that matches your tech stack!
