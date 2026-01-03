# OpenProfileServer API Specification (ALPHA)

## 1. Overview

### 1.1 Identifiers
All endpoints accepting a profile or organization identifier support two formats:
- **AccountName**: `username` (e.g., `/api/profiles/alice`)
- **UUID**: `@{guid}` (e.g., `/api/profiles/@550e8400-e29b-41d4-a716-446655440000`)

### 1.2 Data Format
- **Casing**: The API uses **PascalCase** for all JSON keys in both requests and responses.
- **Input Flexibility**: JSON property matching is case-insensitive for incoming requests to improve client compatibility.
- **Wrapper**: All successful responses are wrapped in `ApiResponse<T>` containing `Status`, `Message`, and `Data`.

### 1.3 Status & Visibility Logic

The API response and Static Generator behavior depend on the `AccountStatus`.

| Status | API Response | Static Generator | Account Deletion | Social Actions |
| :--- | :--- | :--- | :--- | :--- |
| **Active** | Full Data | Full Generation | ✅ Allowed | ✅ Allowed |
| **Suspended** | Restricted (Basic Info Only) | Restricted (No sub-resources) | ✅ **Allowed** | ❌ Forbidden |
| **Banned** | Banned (Status Only) | Banned (Status Only) | ❌ **Forbidden (Locked)** | ❌ Forbidden |
| **PendingDeletion**| 404 / Error | Skipped | N/A (Must Restore) | ❌ Forbidden |

### 1.4 Update Semantics

- **POST**: Full Update (Replace all fields). Missing fields will be reset to defaults.
- **PATCH**: Partial Update (Update only provided non-null fields).

---

## 2. Server & Meta (Public)

These endpoints provide information about the server instance, enabled features, and site branding.

| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) | Static File |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api` | Get combined server info, features, and site metadata. | N/A | `ServerResponseDto` | `api/index.json` |
| **GET** | `/api/info` | Get standalone server version and mode. | N/A | `ServerInfoDto` | `api/info.json` |
| **GET** | `/api/meta` | Get standalone site branding metadata. | N/A | `SiteMetadataDto` | `api/meta.json` |
| **GET** | `/api/features` | **NEW** Get server feature flags (Email enabled, Registration allowed, etc.). | N/A | `ServerFeaturesDto` | `api/features.json` |

### 2.1 Admin Management (Protected)

Manage site branding (Site Name, Description, Logo, etc.).

| Method | Endpoint | Description | Permission | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/admin/meta` | **Full Update** site metadata. | AdminOrHigher | `UpdateSiteMetadataRequestDto` | `MessageResponse` |
| **PATCH** | `/api/admin/meta` | **Partial Update** site metadata. | AdminOrHigher | `UpdateSiteMetadataRequestDto` | `MessageResponse` |

---

## 3. Authentication (Dynamic Only)

Authentication and Registration flow with mandatory email verification support.

### 3.1 Flow Logic
1.  **Check Configuration**: Call `GET /api/features` (or auth config) to check `EmailVerification`.
2.  **Request Code (If required)**: If `true`, call `POST /api/auth/send-code` with `{ email, type: 0 }`.
3.  **Register**: Call `POST /api/auth/register` including the `code`. The account is created as **Verified** and **Logged In** atomically.

| Method | Endpoint | Description | Auth | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/auth/config` | Get auth settings. | No | N/A | `AuthConfigDto` |
| **POST** | `/api/auth/send-code` | Send verification code. | No | `SendCodeRequestDto` | `MessageResponse` |
| **POST** | `/api/auth/register` | Create account. | No | `RegisterRequestDto` | `TokenResponseDto` |
| **POST** | `/api/auth/login` | Login. | No | `LoginRequestDto` | `TokenResponseDto` |
| **POST** | `/api/auth/refresh` | Exchange RefreshToken. | No | `RefreshTokenRequestDto` | `TokenResponseDto` |
| **POST** | `/api/auth/logout` | Revoke a specific token. | Yes | `RefreshTokenRequestDto` | `MessageResponse` |
| **POST** | `/api/auth/logout-all` | **SecurityStamp Reset**. | Yes | N/A | `MessageResponse` |

---

## 4. Business Logic Details (Block & Follow)

### 4.1 Block Priority
When User A attempts to Follow User B, the system checks:
1.  Does A Block B? → **Yes**: Follow Forbidden.
2.  Does B Block A? → **Yes**: Follow Forbidden (Return 403 or 404).

### 4.2 Block Side Effects
When User A calls `POST .../block` to block User B:
1.  If A is Following B → **Auto Unfollow**.
2.  If B is Following A → **Auto Remove Follower** (B no longer follows A).

### 4.3 Static Generation Privacy
- **Status Filtering**: When generating `followers.json` or `following.json`, users with `Status != Active` must be filtered out.
- **Privacy Settings**:
  - If `AccountSettings.ShowFollowersList == false`, the generator should produce an empty array `[]` (instead of 404) to maintain frontend consistency.
  - The same logic applies to `following.json`.

---

## 5. Public Profiles (Read-Only / Static Compatible)

These endpoints are used by the Static Generator.

| Method | Endpoint | Description | Response Data (DTO) | Static File |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/profiles/{profile}` | Get profile info. | `ProfileDto` | `api/profiles/{profile}.json` |
| **GET** | `/api/profiles/{profile}/memberships` | Get public organizations. | `IEnumerable<PublicOrganizationMembershipDto>` | `api/profiles/{profile}/memberships.json` |
| **GET** | `/api/profiles/{profile}/members` | Get **public** members (Org only). | `IEnumerable<OrganizationMemberDto>` | `api/profiles/{profile}/members.json` |
| **GET** | `/api/profiles/{profile}/followers` | Get followers list. | `IEnumerable<FollowerDto>` | `api/profiles/{profile}/followers.json` |
| **GET** | `/api/profiles/{profile}/following` | Get following list. | `IEnumerable<FollowerDto>` | `api/profiles/{profile}/following.json` |

### 5.1 Public Sub-Resources
Specific endpoints for sub-resources (also used by Static Generator).

| Endpoint | Response Data (DTO) | Static File |
| :--- | :--- | :--- |
| `/api/profiles/{profile}/work` | `IEnumerable<WorkExperienceDto>` | `.../work.json` |
| `/api/profiles/{profile}/education` | `IEnumerable<EducationExperienceDto>` | `.../education.json` |
| `/api/profiles/{profile}/projects` | `IEnumerable<ProjectDto>` | `.../projects.json` |
| `/api/profiles/{profile}/socials` | `IEnumerable<SocialLinkDto>` | `.../socials.json` |
| `/api/profiles/{profile}/gallery` | `IEnumerable<GalleryItemDto>` | `.../gallery.json` |
| `/api/profiles/{profile}/certificates`| `IEnumerable<CertificateDto>` | `.../certificates.json` |
| `/api/profiles/{profile}/sponsorships`| `IEnumerable<SponsorshipItemDto>` | `.../sponsorships.json` |

---

## 6. Personal Management (Dynamic / Protected)

### 6.1 Lifecycle
| Method | Endpoint | Description | Permission | Request Body | Response Data |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **DELETE** | `/api/me` | **Request Account Deletion**. | Owner | N/A | `MessageResponse` |
| **POST** | `/api/me/restore` | **Restore Account**. | Owner | N/A | `MessageResponse` |

### 6.2 Account & Security
| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/me` | Get full account details. | N/A | `AccountDto` |
| **GET** | `/api/me/permissions` | **Get Role and Type**. | N/A | `AccountPermissionsDto` |
| **GET** | `/api/me/emails` | List all emails. | N/A | `IEnumerable<AccountEmailDto>` |
| **POST** | `/api/me/emails` | Add new email. | `AddEmailRequestDto` | `MessageResponse` |
| **POST** | `/api/me/emails/{email}/primary`| Set as primary. | N/A | `MessageResponse` |
| **POST** | `/api/me/emails/{email}/verify` | Verify email with code. | `VerifyEmailRequestDto`| `MessageResponse` |
| **DELETE** | `/api/me/emails/{email}` | Remove email. | N/A | `MessageResponse` |
| **POST** | `/api/me/password` | Change password. | `ChangePasswordRequestDto` | `MessageResponse` |
| **GET** | `/api/me/blocks` | **List blocked users**. | N/A | `IEnumerable<BlockDto>` |

### 6.3 Settings & Profile
| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/me/settings` | Get personal settings. | N/A | `PersonalSettingsDto` |
| **POST** | `/api/me/settings` | Full update settings. | `UpdatePersonalSettingsRequestDto` | `MessageResponse` |
| **PATCH** | `/api/me/settings` | Partial update settings. | `UpdatePersonalSettingsRequestDto` | `MessageResponse` |
| **GET** | `/api/me/profile` | Get profile (Edit View). | N/A | `ProfileDto` |
| **POST** | `/api/me/profile` | Full update profile. | `UpdateProfileRequestDto` | `MessageResponse` |
| **PATCH** | `/api/me/profile` | Partial update profile. | `UpdateProfileRequestDto` | `MessageResponse` |

### 6.4 Invitations (Incoming)
| Method | Endpoint | Description | Request Body | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/me/invitations` | List invitations. | N/A | `IEnumerable<OrganizationInvitationDto>` |
| **POST** | `.../{id}/accept` | Accept invitation. | N/A | `MessageResponse` |
| **POST** | `.../{id}/decline` | Decline invitation. | N/A | `MessageResponse` |

### 6.5 Social Actions
*Operator must be in **Active** status.*

| Method | Endpoint | Description | Response Data (DTO) |
| :--- | :--- | :--- | :--- |
| **POST** | `/api/profiles/{profile}/follow` | **Follow** target user/org. | `MessageResponse` |
| **DELETE** | `/api/profiles/{profile}/follow` | **Unfollow**. | `MessageResponse` |
| **GET** | `/api/profiles/{profile}/follow` | Check follow status. | `FollowStatusDto` |
| **POST** | `/api/profiles/{profile}/block` | **Block** target. | `MessageResponse` |
| **DELETE** | `/api/profiles/{profile}/block` | **Unblock** target. | `MessageResponse` |

### 6.6 Sub-Resources (Management)
Endpoints follow the pattern `/api/me/{resource}`.
All **POST/PATCH** requests return `MessageResponse`.

| Resource (URL Part) | GET Response (DTO) | CREATE/UPDATE Request (DTO) |
| :--- | :--- | :--- |
| `work` | `IEnumerable<WorkExperienceDto>` | `UpdateWorkExperienceRequestDto` |
| `education` | `IEnumerable<EducationExperienceDto>` | `UpdateEducationExperienceRequestDto` |
| `projects` | `IEnumerable<ProjectDto>` | `UpdateProjectRequestDto` |
| `socials` | `IEnumerable<SocialLinkDto>` | `UpdateSocialLinkRequestDto` |
| `gallery` | `IEnumerable<GalleryItemDto>` | `UpdateGalleryItemRequestDto` |
| `certificates` | `IEnumerable<CertificateDto>` | `UpdateCertificateRequestDto` |
| `sponsorships` | `IEnumerable<SponsorshipItemDto>` | `UpdateSponsorshipItemRequestDto` |

### 6.7 Notifications (Inbox)

| Method | Endpoint | Description | Request Body | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/me/notifications` | List notifications. | Query (`page`, `unreadOnly`) | `PagedResponse<NotificationDto>` |
| **GET** | `.../unread-count` | Get total unread. | N/A | `int` |
| **PATCH** | `.../{id}/read` | Mark as read. | N/A | `MessageResponse` |
| **POST** | `.../read-all` | Mark all read. | N/A | `MessageResponse` |
| **DELETE** | `.../{id}` | Delete notification. | N/A | `MessageResponse` |
| **DELETE** | `/api/me/notifications` | Delete all read. | N/A | `MessageResponse` |

---

## 7. Organization Management (Dynamic / Protected)

### 7.1 Lifecycle
| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs` | List my organizations. | N/A | `IEnumerable<OrganizationDto>` |
| **POST** | `/api/orgs` | Create new organization. | `CreateOrganizationRequestDto` | `Guid` (Org ID) |
| **DELETE** | `/api/orgs/{org}` | **Request Dissolution**. | N/A | `MessageResponse` |
| **POST** | `.../{org}/restore` | **Restore Organization**. | N/A | `MessageResponse` |

### 7.2 Settings & Profile
| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}` | Org Dashboard. | N/A | `OrganizationDto` |
| **GET** | `.../settings` | Get settings. | N/A | `OrganizationSettingsDto` |
| **POST** | `.../settings` | Full update settings. | `UpdateOrganizationSettingsRequestDto` | `MessageResponse` |
| **PATCH** | `.../settings` | Partial update settings.| `UpdateOrganizationSettingsRequestDto` | `MessageResponse` |
| **POST** | `.../profile` | Full update profile. | `UpdateProfileRequestDto` | `MessageResponse` |
| **PATCH** | `.../profile` | Partial update profile. | `UpdateProfileRequestDto` | `MessageResponse` |

### 7.3 Member Management
| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `.../members` | List all members. | N/A | `IEnumerable<OrganizationMemberDto>` |
| **GET** | `.../members/me/role` | **Get my Role**. | N/A | `MemberRoleDto` |
| **POST** | `.../members` | Invite/Add member. | `InviteMemberRequestDto` | `MessageResponse` |
| **PATCH** | `.../members/me` | Update my info. | `UpdateMemberRequestDto` | `MessageResponse` |
| **DELETE** | `.../members/me` | Leave organization. | N/A | `MessageResponse` |
| **PATCH** | `.../members/{user}` | Update member role. | `UpdateMemberRequestDto` | `MessageResponse` |
| **DELETE** | `.../members/{user}` | Kick member. | N/A | `MessageResponse` |

### 7.4 Invitations (Outgoing)
| Method | Endpoint | Description | Request Body | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `.../invitations` | List pending. | N/A | `IEnumerable<OrganizationInvitationDto>` |
| **POST** | `.../invitations` | Send invitation. | `InviteMemberRequestDto` | `MessageResponse` |
| **DELETE** | `.../invitations/{id}`| Revoke invitation. | N/A | `MessageResponse` |

---

## 8. System Administration (Dynamic / Protected)

Admins can perform operations that bypass standard checks.

### 8.1 User & Account Management
| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/admin/users` | List users. | Query (`UserFilterDto`) | `PagedResponse<UserAdminDto>` |
| **POST** | `/api/admin/users` | Force create user/org. | `CreateUserRequestDto` | `UserAdminDto` |
| **PATCH** | `.../{user}/status` | Change status. | `UpdateUserStatusRequestDto` | `MessageResponse` |
| **DELETE** | `.../{user}` | **Physical Delete**. | N/A | `MessageResponse` |
| **POST** | `.../{user}/role` | Promote/Demote. | `UpdateUserRoleRequestDto` | `MessageResponse` |

### 8.2 Organization Administration (Force Actions)
| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `.../orgs/{org}/members` | **List Members**. | N/A | `IEnumerable<OrganizationMemberDto>` |
| **POST** | `.../orgs/{org}/members` | **Force Add**. | `InviteMemberRequestDto` | `MessageResponse` |
| **PATCH** | `.../members/{user}` | **Force Update**. | `UpdateMemberRequestDto` | `MessageResponse` |
| **DELETE** | `.../members/{user}` | **Force Kick**. | N/A | `MessageResponse` |

### 8.3 System Configuration (Runtime)
| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `.../system-settings` | List all settings. | N/A | `IEnumerable<SystemSettingDto>` |
| **GET** | `.../system-settings/{key}`| Get specific setting. | N/A | `SystemSettingDto` |
| **PUT** | `.../system-settings/{key}`| **Update** setting. | `UpdateSystemSettingRequestDto` | `MessageResponse` |
