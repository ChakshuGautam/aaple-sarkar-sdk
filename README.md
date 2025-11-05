# Aaple Sarkar SDK

Complete SDK for integrating with Maharashtra Government's Aaple Sarkar Track Application Status API.

## Overview

This repository contains **complete production-ready SDKs** for both sides of the integration:

- **For Aaple Sarkar Team**: Client SDK to call department APIs and retrieve application status
- **For Departments**: Server SDK to receive requests and provide application status

## Quick Links

- **📖 [For Aaple Sarkar Team](GUIDES/For-Aaple-Sarkar.md)** - Client SDK guide (3 steps)
- **🏛️ [For Departments](GUIDES/For-Departments.md)** - Server API guide (4 steps)
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
    ├── TrackApplicationSDK.cs          # Client SDK (870 lines)
    ├── TrackApplicationSDK-Examples.cs # Client usage examples (500 lines)
    ├── DepartmentSDK.cs                # Department Server SDK (900 lines) ⭐ NEW
    ├── DepartmentSDK-Examples.cs       # Server usage examples (1000 lines) ⭐ NEW
    ├── DepartmentAPI-Template.cs       # Server template (600 lines)
    └── DepartmentAPI-Validator.cs      # Testing tool (800 lines)
```

## What's New

### Department SDK ⭐

We've added a **complete server-side SDK** for departments that:

- **Handles all encryption/decryption automatically**
- **Validates requests and responses**
- **Provides clean interface** - Just implement `IDepartmentDataProvider`
- **Includes helper utilities** - Date formatting, status mapping, etc.
- **10 comprehensive examples** - From basic to production-ready
- **Type-safe** - Full IntelliSense support
- **Production-ready** - Error handling, logging, validation

**Before (Template):**
```csharp
// You had to manage encryption, validation, errors manually
// Template provided structure but required more work
```

**Now (SDK):**
```csharp
// Just implement one interface:
public class MyDataProvider : IDepartmentDataProvider
{
    public async Task<ApplicationStatusResponse> GetApplicationStatusAsync(...)
    {
        // Your database logic here
        return response;
    }
}

// SDK handles everything else!
```
