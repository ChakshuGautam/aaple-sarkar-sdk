# Aaple Sarkar Dashboard Requirements

## Overview

This document outlines the dashboard requirements for review and monitoring of applications under the Right to Services (RTS) framework.

## Dashboard URL

**Proposed Dashboard:** https://mahagprdashboard.lovable.app/dashboard

## Key Requirement: RTS Timeline Bifurcation

### Current Situation

The existing dashboard data pulled from departments does **not** include bifurcation of pending applications based on RTS timelines.

### Required Bifurcation

All pending applications need to be categorized as:

1. **Pending Within RTS Timelines**
   - Applications that are still within the stipulated time limit for service delivery
   - These applications are within the legal timeframe

2. **Pending Outside RTS Timelines**
   - Applications that have exceeded the stipulated time limit
   - These applications are overdue and require immediate attention

### Challenge

The development team has indicated difficulty implementing this bifurcation because:
- Current department APIs do not provide timeline status
- The distinction requires calculating time elapsed against the `EstimatedDisbursalDays` field
- Need to determine if this calculation should happen at:
  - Department level (in their API responses)
  - Aaple Sarkar platform level (during aggregation)

## API Reference

The existing API for pulling dashboard data is documented in the Technical Integration Document (TECHNICAL_INTEGRATION_V3.3.md).

### Relevant Fields from Application Status API

From the current API response structure:

```
ApplicationStatusResponse:
  - ApplicationSubmissionDate (DD-MMM-YYYY,HH:mm:ss)
  - EstimatedDisbursalDays (Integer)
  - FinalDecision ("0"=Approved, "1"=Rejected, "2"=Pending, ""=Not decided)
  - CurrentDeskNumber
  - NextDeskNumber
  - TotalNumberOfDesks
```

## Proposed Solution Approach

### Option 1: Calculate at Department Level

**Departments modify their API to include:**

```json
{
  "ApplicationID": "INC12345678",
  "EstimatedDisbursalDays": 7,
  "ApplicationSubmissionDate": "18-Sep-2025,17:30:00",
  "DaysElapsed": 5,
  "WithinRTSTimeline": true,  // NEW FIELD
  "DaysOverdue": 0,            // NEW FIELD (0 if within timeline, positive if overdue)
  "FinalDecision": "2"
}
```

**Pros:**
- Department has authoritative data
- Calculation happens once at source
- Reduces processing load on central platform

**Cons:**
- Requires all 50+ departments to update their APIs
- More complex deployment across departments

### Option 2: Calculate at Aaple Sarkar Platform Level

**Central platform calculates based on existing fields:**

```javascript
// Pseudocode
const submissionDate = parseDate(response.ApplicationSubmissionDate);
const currentDate = new Date();
const daysElapsed = calculateDaysDifference(submissionDate, currentDate);
const estimatedDays = response.EstimatedDisbursalDays;

const withinRTSTimeline = daysElapsed <= estimatedDays;
const daysOverdue = daysElapsed > estimatedDays ? (daysElapsed - estimatedDays) : 0;
```

**Pros:**
- No changes required to department APIs
- Faster implementation
- Centralized calculation logic

**Cons:**
- Calculation happens repeatedly during aggregation
- Assumes all departments provide accurate dates

### Option 3: Hybrid Approach

**Add optional fields to API specification:**
- Departments that can calculate provide the fields
- Aaple Sarkar platform calculates for departments that don't
- Gradually migrate all departments to provide the fields

## Recommended Approach

**Option 2: Calculate at Aaple Sarkar Platform Level**

**Rationale:**
1. Minimizes changes to 50+ department systems
2. Can be implemented immediately
3. Based on existing, required fields in V3 specification
4. Calculation logic is straightforward

### Implementation Details

**For pending applications (FinalDecision = "2" or ""):**

```javascript
function calculateRTSStatus(application) {
  // Parse submission date
  const submissionDate = parseDDMMMYYYY(application.ApplicationSubmissionDate);

  // Get current date
  const currentDate = new Date();

  // Calculate days elapsed (excluding weekends/holidays if required)
  const daysElapsed = calculateBusinessDays(submissionDate, currentDate);

  // Determine RTS status
  const withinTimeline = daysElapsed <= application.EstimatedDisbursalDays;
  const daysOverdue = Math.max(0, daysElapsed - application.EstimatedDisbursalDays);

  return {
    withinRTSTimeline: withinTimeline,
    daysElapsed: daysElapsed,
    daysOverdue: daysOverdue,
    percentageComplete: (daysElapsed / application.EstimatedDisbursalDays) * 100
  };
}
```

### Dashboard Display

**Pending Applications Summary:**

```
┌─────────────────────────────────────────┐
│  Pending Applications: 1,234            │
├─────────────────────────────────────────┤
│  ✓ Within RTS Timelines:    987 (80%)  │
│  ⚠ Outside RTS Timelines:   247 (20%)  │
└─────────────────────────────────────────┘
```

**Color Coding:**
- Green: Within RTS timeline
- Yellow: Approaching deadline (>80% of estimated days)
- Red: Outside RTS timeline (overdue)

## Next Steps

1. **Review and approve** calculation approach
2. **Implement** calculation logic in Aaple Sarkar backend
3. **Update dashboard** to display bifurcated data
4. **Add filters** for drilling down into each category
5. **Create alerts** for applications approaching/exceeding RTS timelines

## Related Documents

- [TECHNICAL_INTEGRATION_V3.3.md](TECHNICAL_INTEGRATION_V3.3.md) - Full API specification
- [For-Aaple-Sarkar.md](GUIDES/For-Aaple-Sarkar.md) - Client SDK guide
- [For-Departments.md](GUIDES/For-Departments.md) - Server template guide

## Contact

For questions regarding dashboard requirements:
- **Email:** madhurima@samagragovernance.in
- **Team:** Samagra Governance

---

**Status:** Proposed Solution
**Priority:** High
**Impact:** All 50+ departments
