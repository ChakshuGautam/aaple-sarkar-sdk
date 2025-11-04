# Aaple Sarkar SDK

Complete SDK for integrating with Maharashtra Government's Aaple Sarkar Track Application Status API.

## Overview

This repository contains production-ready code and documentation for both sides of the integration:

- **For Aaple Sarkar Team**: Client SDK to call department APIs
- **For Departments**: Server template to build APIs quickly

## Quick Links

- **📖 [For Aaple Sarkar Team](GUIDES/For-Aaple-Sarkar.md)** - Client SDK guide (3 steps)
- **🏛️ [For Departments](GUIDES/For-Departments.md)** - Server API guide (4 steps)
- **📄 [OpenAPI Specification](openapi.yaml)** - API specification
- **📘 [Technical Integration V3.3](TECHNICAL_INTEGRATION_V3.3.md)** - Complete technical documentation
- **📊 [Dashboard Requirements](DASHBOARD_REQUIREMENTS.md)** - RTS timeline bifurcation requirements

## Structure

```
.
├── README.md                           # This file
├── openapi.yaml                        # OpenAPI 3.0 specification
├── TECHNICAL_INTEGRATION_V3.3.md      # Complete technical documentation
├── DASHBOARD_REQUIREMENTS.md           # Dashboard bifurcation requirements
│
├── GUIDES/
│   ├── For-Aaple-Sarkar.md            # Client integration guide
│   └── For-Departments.md             # Server implementation guide
│
└── CODE/
    ├── TrackApplicationSDK.cs          # Client SDK (800 lines)
    ├── TrackApplicationSDK-Examples.cs # Usage examples (500 lines)
    ├── DepartmentAPI-Template.cs       # Server template (600 lines)
    └── DepartmentAPI-Validator.cs      # Testing tool (800 lines)
```
