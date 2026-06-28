# HLD Review — Responses & Enquiry Answers
> Based on: وثيقة متطلبات الأعمال V4.0 — مركز المعرفة لالقتصاد الدائري للكربون  
> Date: May 17, 2026

---

## Part 1 — HLD Comments

### 1. Communication Table

The BRD does not include a dedicated communication table. Based on the documented integrations, roles, and user stories, the following flows are implied and must be formalized in the HLD:

| Source | Destination | Protocol | Purpose |
|--------|-------------|----------|---------|
| Public Users / Visitors | External Web Portal | HTTPS (TLS) | Browse content, register, login |
| Registered Users | External Web Portal | HTTPS (TLS) | Posts, downloads, profile management, ratings |
| State Representatives | External/Internal Portal | HTTPS (TLS) | Upload country resources, update country profile |
| Admins / Content Managers | Internal CMS Portal | HTTPS (TLS) | Content management, user management |
| Super Admin | Internal CMS Portal | HTTPS (TLS) | System administration, policies, user creation |
| Web Application | KAPSARC API | HTTPS (TLS) | Retrieve CCE classification, performance, CCE Total Index |
| Web Application | Email Service | TLS | Notifications: expert registration, source upload, post deletion |
| Web Application | Database Server | Encrypted channel | Data persistence |

> **Action Required:** A formal communication matrix must be included in the HLD covering all flows with: source, destination, port, protocol, direction, and encryption status.

---

### 2. WAF (Web Application Firewall)

The BRD does **not** mention a WAF. However, a WAF is **strongly recommended** given:

- The platform is publicly accessible to global users
- It handles **file uploads** (source documents, CV attachments, images, videos)
- It interfaces with external APIs (KAPSARC)
- Non-functional requirement **NF001** mandates high performance (< 3 sec page load)

> **Action Required:** The HLD should explicitly highlight WAF placement — positioned in front of the public-facing web/API server in the DMZ — with rules covering OWASP Top 10 threats, file upload protection, and rate limiting.

---

### 3. All Communications Over TLS

The BRD specifies a Web App environment for all user stories, inherently implying HTTPS. **All** communication paths must be over TLS, including:

- Browser ↔ Web Server
- Web Server ↔ Database
- Web Server ↔ KAPSARC API *(Section 6.5.1)*
- Web Server ↔ Email delivery service

> **Action Required:** All arrows/flows in the HLD architecture diagrams must be explicitly labelled as TLS-encrypted. No plaintext communication channels should be present.

---

### 4. External API Server Placement — Why Is It in the Internal Zone?

The BRD does not address network zone placement. However, this is a **valid security concern**.

The External API serves public users (Visitors, Registered Users, State Representatives) and **must reside in a DMZ**, not the internal zone. Placing it in the internal zone violates the principle of network segmentation and exposes internal systems to unnecessary risk.

> **Action Required:** Correct the HLD to place the public-facing web/API server in a **DMZ**, fronted by a WAF and firewall, with only controlled, restricted communication allowed inward to the application and database tiers.

---

### 5. VLANs for Servers

The BRD does not specify VLANs. The HLD must define a segmentation scheme. Recommended VLAN structure:

| VLAN | Segment | Servers / Purpose |
|------|---------|-------------------|
| VLAN 10 | DMZ | Web / External API Server |
| VLAN 20 | Application Zone | Internal CMS API, Application Server |
| VLAN 30 | Data Zone | Database Server |
| VLAN 40 | Management | Monitoring, Logging, Admin access |
| VLAN 50 | Integration | KAPSARC integration relay |

> **Action Required:** Assign and document VLANs for each server tier in the HLD.

---

### 6. Logical/Physical Segregation and Network Segmentation Using Firewalls

The BRD does not detail the network architecture. The HLD must document:

- **Firewalls** between all zones: DMZ ↔ Application Zone ↔ Data Zone
- **No direct public access** to the database or internal application servers
- **ACLs** restricting traffic to only required ports and protocols between zones
- **State Representatives** accessing from outside KSA must traverse the DMZ securely with enforced MFA

> **Action Required:** Include a network segmentation diagram in the HLD showing all firewalls, zones, and allowed traffic flows.

---

### 7. Secure Usage Policy for External Website Users

The BRD includes a **Policies and Terms feature (F032)**, managed exclusively by the Super Admin (F039). This covers general terms, privacy policy, and applicable laws.

The HLD should reference this and ensure:

- Users must **accept T&C** during account creation (F033 — إنشاء حساب)
- The policy page is publicly accessible (Visitor + Registered User, per permissions matrix)
- The secure usage policy addresses: data handling, acceptable use, and **PDPL** (Saudi Personal Data Protection Law) compliance

> **Action Required:** The HLD should reference the policy management feature and confirm the enforcement mechanism at the application layer (e.g., checkbox on registration, session-based acceptance tracking).

---

### 8. Multi-Factor Authentication (MFA) for External Users

The BRD **does not explicitly mention MFA**. The login flow (F034 / US034) only requires email + password. However, given:

- The platform handles **country-sensitive data** (Country Profile)
- **State Representatives** have elevated upload and edit privileges
- The platform is **internationally accessible**

**MFA must be addressed in the HLD.** Recommended approach:

| Role | MFA Requirement |
|------|----------------|
| Visitors | Not applicable (no login) |
| Registered Users | Optional MFA (e.g., OTP via email) |
| State Representatives | **Mandatory MFA** — elevated privileges + remote access |
| Content Managers | **Mandatory MFA** |
| Admins / Super Admin | **Mandatory MFA** |

> **Recommended Implementation:** Time-based OTP (TOTP) or email OTP injected into the existing login flow (after password validation). The HLD must specify the MFA mechanism and enforcement points.

---

## Part 2 — Enquiries

### 1. Super Admin, Admin, and Content Manager — Ministry of Energy Employees?

**Partially confirmed — with an important distinction.**

This is an **international platform**, therefore not all administrative roles will access the system from within the ministry's internal network. The access model is clarified as follows:

| Role | Access Location | Authentication Method |
|------|----------------|-----------------------|
| Super Admin | Internal (Ministry network) | Active Directory (AD) **or** standard email/password |
| Admin | Internal (Ministry network) | Active Directory (AD) **or** standard email/password |
| Content Manager | Internal (Ministry network) | Active Directory (AD) **or** standard email/password |
| State Representative | External (international) | Standard email/password + MFA |
| Registered User | External (global) | Standard email/password |

**Key clarification:** Admins (Super Admin, Admin, Content Manager) will have the option to authenticate via **Active Directory (AD)** integration in addition to the standard email/password login. This dual-authentication support must be reflected in the HLD's identity and access management (IAM) design.

Regarding country-specific resource and profile management — this is confirmed to be within the responsibilities of Admin and Super Admin roles, as documented in the permissions matrix *(Sections 4.1.23, 4.1.26)*.

---

### 2. URL Address for the Website

**Confirmed.** The platform URL is:

> **[www.test.com](https://www.test.com)**

This must be reflected across all relevant HLD components, including:

- **DNS configuration** — A/CNAME record pointing to the load balancer or WAF ingress
- **TLS certificate provisioning** — A valid certificate must be issued and auto-renewal configured for this domain
- **WAF policy setup** — The domain must be registered and protected under the WAF configuration
- **CORS and CSP policies** — Application-level security headers must reference this domain
- **Email notifications** — All system-generated emails (MSG001–MSG005) must reference this domain in links and branding

---

### 3. Web Servers — Standalone VMs? Database in Containers?

The BRD **does not specify the infrastructure deployment model**. It only states a `Web App` environment for all user stories. The deployment architecture (standalone VMs vs. containers vs. PaaS) must be **confirmed with the technical/infrastructure team** and explicitly documented in the HLD.

---

### 4. State Representative Access — From Outside KSA?

**Confirmed.** All users — including State Representatives — will access the platform from **anywhere in the world**. There are no geographic restrictions on access.

As an **international platform**, the system is designed to serve users across the globe. State Representatives act on behalf of their respective participating countries and will access the platform remotely from their home countries or any other location.

This has the following infrastructure and security implications:

- The platform must be **globally accessible** with no IP-based geo-blocking (unless explicitly required for specific admin functions)
- The public-facing server must be placed in a **DMZ** and fronted by a **WAF** to handle international traffic securely
- **Mandatory MFA** must be enforced for State Representatives given their elevated privileges (source uploads, country profile updates)
- **CDN** usage should be considered for static assets to optimize performance for international users
- All access must be over **TLS (HTTPS)** regardless of the user's geographic origin

---

### 5. Registered Users — Do They Encompass All Public Users, Inside and Outside KSA?

**Confirmed.** Registered Users are **not** limited to country representatives or KSA-based users. The platform is open to the general public worldwide.

The user base is structured as follows:

| User Type | Scope | Registration Required |
|-----------|-------|-----------------------|
| Visitor | Global — anyone worldwide | No |
| Registered User (Beneficiary) | Global — any individual, inside or outside KSA | Yes (F033) |
| State Representative | International — representative of a participating country | Yes (created by Admin) |

**Key clarification:** Registered Users are not restricted to citizens or residents of participating countries. Any member of the public — regardless of nationality or location — may create an account and access registered-user features (posts, profile, expert registration, personalized recommendations, etc.).

This has implications for:

- **Privacy and data handling** — The platform must comply with data protection regulations applicable across multiple jurisdictions, including **PDPL** for KSA-related personal data
- **Scalability** — The system must be architected to handle a potentially large, geographically distributed user base
- **Localization** — Multi-language support should be considered to serve a global audience effectively

---

### 6. KAPSARC Integration

**Yes — explicitly documented in Section 6.5.1.**

| Attribute | Detail |
|-----------|--------|
| **Service Name** | Circular Carbon Economy Classification Verification |
| **Operation Type** | Data Retrieval (read-only) |
| **Source System** | KAPSARC (King Abdullah Petroleum Studies and Research Center) |
| **Triggered By** | Country Profile page (F014, F059, F060) |
| **Inputs Sent** | Country Name + Country Code |
| **Data Retrieved** | CCE Classification, CCE Performance, CCE Total Index |
| **Constraint** | Retrieved data is **read-only** — State Representatives cannot edit KAPSARC-sourced fields |
| **Error Handling** | ER001 — graceful degradation if KAPSARC returns no data |
| **Risk** | Risk #1 *(Section 5.3)* — caching mechanism recommended as fallback |

> **Action Required:** The HLD must show this integration with a dedicated communication lane, over TLS, with appropriate error fallback (caching or graceful degradation).

---

### 7. Will There Be File Uploads?

**Confirmed.** The platform supports multiple file upload types across different roles and features, as explicitly documented in the BRD:

| Feature | Accepted Formats | Uploaded By |
|---------|-----------------|-------------|
| Sources — Centre (F047) | PDF, Word, or URL link | Admins |
| Sources — Countries (F052) | PDF, Word, or URL link | Admins, State Representatives |
| News Images (F044) | PNG | Admins, State Representatives |
| Expert CV — Attachment (F017) | PDF, Word | Registered Users |
| Country Profile — National Contribution (F060) | PNG | State Representatives, Admin |
| Platform Introduction Video (CMS) | Video file | Admins |
| How-to-Use Video (CMS) | Video file | Admins |
| Post Attachments (F026) | To be confirmed | Registered Users |

> **Note:** File upload functionality spans all user tiers — from public Registered Users to internal Admins — making it a critical surface area requiring robust security controls.

**Security requirements for the HLD:**

- **Server-side validation** — File type (MIME type) and file size limits must be enforced at the server layer, independent of client-side checks
- **Malware scanning** — All uploaded files must pass through an antivirus/malware scanning service before being stored or made available for download
- **Isolated storage** — Uploaded files must be stored in a dedicated, isolated storage zone (e.g., object storage service), completely separate from the web server filesystem
- **WAF protection** — WAF rules must include policies targeting malicious file upload attempts (e.g., embedded scripts, polyglot files)
- **Signed/time-limited download URLs** — File download links must be dynamically generated with expiry tokens to prevent unauthorized direct access to stored files
- **Access control** — File access must be validated against the user's role and permissions before any download is served
