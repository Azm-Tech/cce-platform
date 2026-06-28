# HLD Additions — CCE Knowledge Centre Platform
> Document: MOEnergy HLD File v1.1 — Supplementary Sections  
> Based on: BRD V4.0 & Confirmed Enquiry Responses  
> Date: May 18, 2026

---

## Section 1 — Platform Domain & URL

The confirmed public URL for the CCE Knowledge Centre platform is:

> ### 🌐 www.test.com

The following HLD components must reference this domain:

| Component | Required Action |
|-----------|----------------|
| DNS Configuration | Create A/CNAME record pointing to the load balancer or WAF ingress IP |
| TLS Certificate | Issue a valid SSL/TLS certificate for `www.test.com` with auto-renewal configured |
| WAF Policy | Register and protect `www.test.com` under the WAF configuration |
| CORS & CSP Headers | All application-level security headers must explicitly reference this domain |
| Email Notifications | All system-generated emails (MSG001–MSG005) must use this domain in links and sender identity |

---

## Section 2 — Communication Matrix

All platform communications are conducted exclusively over encrypted channels. The table below documents all communication flows between system components, users, and external services.

> **Policy:** No plaintext (HTTP / port 80) communication is permitted on any channel. All HTTP requests must be redirected to HTTPS at the WAF or load balancer level.

| # | Source | Destination | Port | Protocol | Direction | Encrypted |
|---|--------|-------------|------|----------|-----------|-----------|
| 1 | Public Users / Visitors | External Web Portal | 443 | HTTPS | Inbound | ✅ TLS |
| 2 | Registered Users | External Web Portal | 443 | HTTPS | Inbound | ✅ TLS |
| 3 | State Representatives | External Web Portal | 443 | HTTPS | Inbound | ✅ TLS |
| 4 | Admins / Content Managers | Internal CMS Portal | 443 | HTTPS | Inbound | ✅ TLS |
| 5 | Super Admin | Internal CMS Portal | 443 | HTTPS | Inbound | ✅ TLS |
| 6 | External Web Server | Application Server | 8443 | HTTPS | Internal | ✅ TLS |
| 7 | Application Server | Database Server | 1433 | TDS over TLS | Internal | ✅ TLS |
| 8 | Application Server | KAPSARC API | 443 | HTTPS | Outbound | ✅ TLS |
| 9 | Application Server | Email Service (SMTP) | 587 | SMTP/STARTTLS | Outbound | ✅ TLS |
| 10 | Admin Browser | Active Directory (AD) | 636 | LDAPS | Internal | ✅ TLS |

---

## Section 3 — KAPSARC Integration

### 3.1 Overview

The platform integrates with **KAPSARC** (King Abdullah Petroleum Studies and Research Center) to retrieve Circular Carbon Economy (CCE) performance data for participating countries. This integration is **read-only** and is triggered automatically when a user views a Country Profile page.

### 3.2 Integration Details

| Attribute | Detail |
|-----------|--------|
| **Service Name** | Circular Carbon Economy Classification Verification |
| **Operation Type** | Data Retrieval — Read Only |
| **Source System** | KAPSARC API |
| **Triggered By** | Country Profile page load (F014, F059, F060) |
| **Input Parameters** | Country Name, Country Code (ISO 3-character) |
| **Data Retrieved** | CCE Classification, CCE Performance, CCE Total Index |
| **Protocol** | HTTPS over TLS |
| **Data Mutability** | Read-only — no user role can edit KAPSARC-sourced fields |
| **Error Handling** | Graceful degradation — cached data displayed if KAPSARC is unavailable |
| **Fallback Strategy** | Local cache layer to prevent service disruption *(Risk #1, BRD Section 5.3)* |

### 3.3 Integration Flow Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                         USER BROWSER                             │
│               Visits Country Profile Page                        │
└───────────────────────────┬──────────────────────────────────────┘
                            │  HTTPS (TLS)
                            ▼
┌──────────────────────────────────────────────────────────────────┐
│                   WEB APPLICATION SERVER                         │
│                                                                  │
│  1. Load local country profile data from Database                │
│  2. Send API request to KAPSARC                                  │
│     → Input: Country Name + Country Code                         │
└───────────┬──────────────────────────────────┬───────────────────┘
            │  HTTPS (TLS) — Outbound          │  If KAPSARC unavailable
            ▼                                  ▼
┌───────────────────────┐          ┌───────────────────────────────┐
│     KAPSARC API       │          │      LOCAL CACHE LAYER        │
│                       │          │                               │
│  Returns:             │          │  Serves last known CCE data   │
│  • CCE Classification │          │  Prevents page failure        │
│  • CCE Performance    │          │  (Risk #1 mitigation)         │
│  • CCE Total Index    │          └───────────────────────────────┘
└───────────┬───────────┘
            │  Response (Read-Only Data)
            ▼
┌──────────────────────────────────────────────────────────────────┐
│                   WEB APPLICATION SERVER                         │
│                                                                  │
│  3. Merge KAPSARC data with local profile data                   │
│  4. KAPSARC fields rendered as display-only (non-editable)       │
└───────────────────────────┬──────────────────────────────────────┘
                            │  HTTPS (TLS)
                            ▼
┌──────────────────────────────────────────────────────────────────┐
│                         USER BROWSER                             │
│          Country Profile displayed with CCE indicators           │
│                                                                  │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │  CCE Classification  │  [Read-only — sourced from KAPSARC] │   │
│   │  CCE Performance     │  [Read-only — sourced from KAPSARC] │   │
│   │  CCE Total Index     │  [Read-only — sourced from KAPSARC] │   │
│   └─────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
```

---

## Section 4 — File Storage Architecture

### 4.1 Overview

The platform supports file uploads across multiple user roles and features. All uploaded files are stored in an **isolated object storage service** — completely separate from the web server filesystem — and are subject to mandatory security controls both before storage and during retrieval.

### 4.2 Supported Upload Types

| Feature | Accepted Formats | Recommended Max Size | Uploaded By |
|---------|-----------------|----------------------|-------------|
| Sources — Centre (F047) | PDF, Word, URL link | 50 MB | Admins |
| Sources — Countries (F052) | PDF, Word, URL link | 50 MB | Admins, State Representatives |
| News Images (F044) | PNG | 5 MB | Admins, State Representatives |
| Expert CV Attachment (F017) | PDF, Word | 10 MB | Registered Users |
| Country Profile — National Contribution (F060) | PNG | 5 MB | State Representatives, Admin |
| Platform Introduction Video (CMS) | MP4 / Video | 500 MB | Admins |
| How-to-Use Video (CMS) | MP4 / Video | 500 MB | Admins |
| Post Attachments (F026) | To be confirmed | TBC | Registered Users |

### 4.3 Upload Flow Diagram

```
━━━━━━━━━━━━━━━━━━━━━━━━━━  UPLOAD FLOW  ━━━━━━━━━━━━━━━━━━━━━━━━━━

 ┌────────────────────┐
 │   USER / ADMIN     │
 │  Selects file      │
 │  and submits form  │
 └─────────┬──────────┘
           │  HTTPS (TLS)
           ▼
 ┌────────────────────┐
 │       WAF          │──── Blocks known malicious file signatures
 │                    │──── Enforces upload size limits
 │                    │──── Rate limiting on upload endpoints
 └─────────┬──────────┘
           │
           ▼
 ┌────────────────────┐
 │    WEB SERVER      │──── MIME type validation (server-side)
 │                    │──── File extension whitelist enforcement
 │                    │──── Rejects disallowed file types
 └─────────┬──────────┘
           │
           ▼
 ┌────────────────────┐
 │  MALWARE SCANNER   │──── Antivirus / threat detection scan
 │                    │──── Infected files quarantined immediately
 │                    │──── Only clean files proceed
 └─────────┬──────────┘
           │  ✅ Clean file only
           ▼
 ┌────────────────────┐
 │   OBJECT STORAGE   │──── Isolated from web server filesystem
 │   (Isolated Zone)  │──── Files stored with randomised names
 │                    │──── No public direct URL access permitted
 └────────────────────┘
```

### 4.4 Download Flow Diagram

```
━━━━━━━━━━━━━━━━━━━━━━━━━━  DOWNLOAD FLOW  ━━━━━━━━━━━━━━━━━━━━━━━━

 ┌────────────────────┐
 │   USER / ADMIN     │
 │  Requests file     │
 │  download          │
 └─────────┬──────────┘
           │  HTTPS (TLS)
           ▼
 ┌────────────────────┐
 │    WEB SERVER      │──── Validates user session & role
 │                    │──── Checks file access permission
 │                    │──── Rejects unauthorised requests (403)
 └─────────┬──────────┘
           │  ✅ Authorised only
           ▼
 ┌────────────────────┐
 │   SIGNED URL       │──── Time-limited (expires in ~15 minutes)
 │   GENERATOR        │──── Unique cryptographic token per request
 │                    │──── Cannot be shared, replayed, or reused
 └─────────┬──────────┘
           │
           ▼
 ┌────────────────────┐
 │   OBJECT STORAGE   │──── Validates signed token before serving
 │   (Isolated Zone)  │──── Serves file directly to authorised user
 │                    │──── Expired or invalid tokens rejected (403)
 └────────────────────┘
```

### 4.5 Security Controls Summary

| Control | Description |
|---------|-------------|
| **MIME Type Validation** | Server enforces allowed file types regardless of file extension |
| **File Size Limits** | Per-type size limits enforced at both WAF and application layer |
| **Malware Scanning** | All uploads scanned before storage; threats are quarantined |
| **Isolated Storage** | Object storage is network-isolated from the web server |
| **Randomised File Names** | Stored files use randomised names — original names not exposed |
| **Signed Download URLs** | Time-limited, single-use tokens required for all file downloads |
| **Role-Based Access Control** | File access validated against user role and permissions before serving |
| **WAF Upload Rules** | WAF rules specifically block polyglot files and embedded script attacks |
