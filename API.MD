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
| **PendingDeletion** | 404 / Error | Skipped | N/A (Must Restore) | ❌ Forbidden |

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
| **GET** | `/api/features` | Get server feature flags (Email/Registration status). | N/A | `ServerFeaturesDto` | `api/features.json` |

### 2.1 Admin Management (Protected)

Manage site branding (Site Name, Description, Logo, etc.).

| Method | Endpoint | Description | Permission | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/admin/meta` | **Full Update** site metadata. | `AdminOrHigher` | `UpdateSiteMetadataRequestDto` | `MessageResponse` |
| **PATCH** | `/api/admin/meta` | **Partial Update** site metadata. | `AdminOrHigher` | `UpdateSiteMetadataRequestDto` | `MessageResponse` |

---

## 3. Authentication (Dynamic Only)

Authentication and Registration flow with mandatory email verification support.

### 3.1 Flow Logic
1. **Check Configuration**: Call `GET /api/features` to check `EmailVerification`.
2. **Request Code (If required)**: If `true`, call `POST /api/auth/send-code` with `{ email, type: 0 }`.
3. **Register**: Call `POST /api/auth/register` including the `code`. The account is created as **Verified** and **Logged In** atomically.
4. **Account Type Restriction**: Only accounts of type `Personal` and `System` are allowed to login. `Organization` accounts are non-login entities.

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
1. Does A Block B? → **Yes**: Follow Forbidden.
2. Does B Block A? → **Yes**: Follow Forbidden (Return 403 or 404).

### 4.2 Block Side Effects
When User A calls `POST .../block` to block User B:
1. If A is Following B → **Auto Unfollow**.
2. If B is Following A → **Auto Remove Follower** (B no longer follows A).

### 4.3 Static Generation Privacy
- **Status Filtering**: When generating `followers.json` or `following.json`, users with `Status != Active` must be filtered out.
- **Privacy Settings**:
  - If `AccountSettings.ShowFollowersList == false`, the generator produces `[]` (empty array) to maintain consistency.
  - The same logic applies to `following.json`.

---

## 5. Public Profiles (Read-Only / Static Compatible)

These endpoints are used by the Static Generator.

| Method | Endpoint | Description | Response Data (DTO) | Static File |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/profiles/{profile}` | Get profile info. | `ProfileDto` | `api/profiles/{profile}.json` |
| **GET** | `/api/profiles/{profile}/memberships` | Get public organizations. | `IEnumerable<PublicOrganizationMembershipDto>` | `api/profiles/{profile}/memberships.json` |
| **GET** | `/api/profiles/{profile}/members` | Get public members (Org). | `IEnumerable<OrganizationMemberDto>` | `api/profiles/{profile}/members.json` |
| **GET** | `/api/profiles/{profile}/followers` | Get followers list. | `IEnumerable<FollowerDto>` | `api/profiles/{profile}/followers.json` |
| **GET** | `/api/profiles/{profile}/following` | Get following list. | `IEnumerable<FollowerDto>` | `api/profiles/{profile}/following.json` |
| **GET** | `/api/profiles/{profile}/privacy` | Get list privacy status. | `ProfilePrivacyDto` | `api/profiles/{profile}/privacy.json` |
| **GET** | `/api/profiles/{profile}/created-at` | Get creation date. | `AccountCreatedDateDto` | `api/profiles/{profile}/created-at.json` |

### 5.1 Public Sub-Resources

Specific endpoints for sub-resources (also used by Static Generator).

| Endpoint | Response Data (DTO) | Static File |
| :--- | :--- | :--- |
| `/api/profiles/{profile}/work` | `IEnumerable<WorkExperienceDto>` | `.../work.json` |
| `/api/profiles/{profile}/education` | `IEnumerable<EducationExperienceDto>` | `.../education.json` |
| `/api/profiles/{profile}/projects` | `IEnumerable<ProjectDto>` | `.../projects.json` |
| `/api/profiles/{profile}/socials` | `IEnumerable<SocialLinkDto>` | `.../socials.json` |
| `/api/profiles/{profile}/contacts` | `IEnumerable<ContactMethodDto>` | `.../contacts.json` |
| `/api/profiles/{profile}/gallery` | `IEnumerable<GalleryItemDto>` | `.../gallery.json` |
| `/api/profiles/{profile}/certificates` | `IEnumerable<CertificateDto>` | `.../certificates.json` |
| `/api/profiles/{profile}/sponsorships` | `IEnumerable<SponsorshipItemDto>` | `.../sponsorships.json` |

### 5.2 Public Asset Library

Public asset library for static generator and public access. Returns 404 if asset is private, deleted, or belongs to suspended/banned account.

| Method | Endpoint | Description | Response Data (DTO) | Static File |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/assets/{uuid}` | Get public asset by UUID. | `AccountAssetDto` | `api/assets/{uuid}.json` |
| **GET** | `/api/assets/{uuid}.json` | Alias for static generator compatibility. | `AccountAssetDto` | `api/assets/{uuid}.json` |

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
| **POST** | `/api/me/emails/{email}/primary` | Set as primary. | N/A | `MessageResponse` |
| **POST** | `/api/me/emails/{email}/verify` | Verify email with code. | `VerifyEmailRequestDto` | `MessageResponse` |
| **DELETE** | `/api/me/emails/{email}` | Remove email. | N/A | `MessageResponse` |
| **POST** | `/api/me/password` | Change password. | `ChangePasswordRequestDto` | `MessageResponse` |
| **GET** | `/api/me/blocks` | **List blocked users**. | N/A | `IEnumerable<BlockDto>` |
| **GET** | `/api/me/follow-stats` | Get follow counts. | N/A | `FollowCountsDto` |
| **GET** | `/api/me/followers` | List my followers. | N/A | `IEnumerable<FollowerDto>` |
| **GET** | `/api/me/following` | List my following. | N/A | `IEnumerable<FollowerDto>` |

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
| **POST** | `/api/me/invitations/{id}/accept` | Accept invitation. | N/A | `MessageResponse` |
| **POST** | `/api/me/invitations/{id}/decline` | Decline invitation. | N/A | `MessageResponse` |

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

Endpoints follow the pattern `/api/me/{resource}`. All **POST/PATCH** return `MessageResponse`.

| Resource (URL Part) | GET Response (DTO) | CREATE/UPDATE Request (DTO) |
| :--- | :--- | :--- |
| `work` | `IEnumerable<WorkExperienceDto>` | `UpdateWorkExperienceRequestDto` |
| `education` | `IEnumerable<EducationExperienceDto>` | `UpdateEducationExperienceRequestDto` |
| `projects` | `IEnumerable<ProjectDto>` | `UpdateProjectRequestDto` |
| `socials` | `IEnumerable<SocialLinkDto>` | `UpdateSocialLinkRequestDto` |
| `contacts` | `IEnumerable<ContactMethodDto>` | `UpdateContactMethodRequestDto` |
| `gallery` | `IEnumerable<GalleryItemDto>` | `UpdateGalleryItemRequestDto` |
| `certificates` | `IEnumerable<CertificateDto>` | `UpdateCertificateRequestDto` |
| `sponsorships` | `IEnumerable<SponsorshipItemDto>` | `UpdateSponsorshipItemRequestDto` |

### 6.7 Notifications (Inbox)

| Method | Endpoint | Description | Request Body | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/me/notifications` | List notifications. | Query (`page`, `unreadOnly`) | `PagedResponse<NotificationDto>` |
| **GET** | `/api/me/notifications/unread-count` | Get total unread. | N/A | `int` |
| **PATCH** | `/api/me/notifications/{id}/read` | Mark as read. | N/A | `MessageResponse` |
| **POST** | `/api/me/notifications/read-all` | Mark all read. | N/A | `MessageResponse` |
| **DELETE** | `/api/me/notifications/{id}` | Delete notification. | N/A | `MessageResponse` |
| **DELETE** | `/api/me/notifications` | Delete all read. | N/A | `MessageResponse` |

### 6.8 Asset Library (Personal Management)

Manage your personal asset library. Assets can be images, icons, or other reusable resources.

#### 6.8.1 Asset Management

| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/me/assets` | List my assets (paginated). | Query: `?page=&pageSize=&category=&visibility=&search=` | `PagedResponse<AccountAssetDto>` |
| **GET** | `/api/me/assets/{uuid}` | Get specific asset (my own). | N/A | `AccountAssetDto` |
| **POST** | `/api/me/assets` | Create new asset. | `CreateAccountAssetRequestDto` | `AccountAssetDto` |
| **PUT** | `/api/me/assets/{uuid}` | Full update asset. | `CreateAccountAssetRequestDto` | `AccountAssetDto` |
| **PATCH** | `/api/me/assets/{uuid}` | Partial update asset. | `UpdateAccountAssetRequestDto` | `AccountAssetDto` |
| **DELETE** | `/api/me/assets/{uuid}` | Delete asset. | N/A | `MessageResponse` |

#### 6.8.2 Batch Operations

| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **PATCH** | `/api/me/assets/batch/visibility` | Batch update visibility. | `BatchUpdateVisibilityRequestDto` | `MessageResponse` |
| **DELETE** | `/api/me/assets/batch` | Batch delete assets. | `BatchDeleteRequestDto` | `MessageResponse` |

#### 6.8.3 Categories

| Method | Endpoint | Description | Response Data (DTO) |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/me/assets/categories` | List all my categories (distinct). | `IEnumerable<string>` |

---

## 7. Organization Management (Dynamic / Protected)

### 7.1 Lifecycle

| Method | Endpoint | Description | Permission | Request Body | Response |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs` | List my organizations. | Authenticated | N/A | `IEnumerable<OrganizationDto>` |
| **POST** | `/api/orgs` | Create new organization. | Authenticated | `CreateOrganizationRequestDto` | `Guid` (Org ID) |
| **DELETE** | `/api/orgs/{org}` | **Request Dissolution**. | Owner | N/A | `MessageResponse` |
| **POST** | `/api/orgs/{org}/restore` | **Restore Organization**. | Owner | N/A | `MessageResponse` |

### 7.2 Settings & Profile

| Method | Endpoint | Description | Permission | Request Body | Response |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}` | Org Dashboard summary. | Any Member | N/A | `OrganizationDto` |
| **GET** | `/api/orgs/{org}/settings` | Get org settings. | Owner/Admin | N/A | `OrganizationSettingsDto` |
| **POST** | `/api/orgs/{org}/settings` | Full update settings. | Owner | `UpdateOrganizationSettingsRequestDto` | `MessageResponse` |
| **PATCH** | `/api/orgs/{org}/settings` | Partial update settings. | Owner | `UpdateOrganizationSettingsRequestDto` | `MessageResponse` |
| **GET** | `/api/orgs/{org}/profile` | **Get profile (Edit View).** | **Any Member** | N/A | `ProfileDto` |
| **POST** | `/api/orgs/{org}/profile` | Full update profile. | Owner/Admin | `UpdateProfileRequestDto` | `MessageResponse` |
| **PATCH** | `/api/orgs/{org}/profile` | Partial update profile. | Owner/Admin | `UpdateProfileRequestDto` | `MessageResponse` |
| **GET** | `/api/orgs/{org}/follow-stats` | Get org follow counts. | Any Member | N/A | `FollowCountsDto` |
| **GET** | `/api/orgs/{org}/followers` | List org followers. | Any Member | N/A | `IEnumerable<FollowerDto>` |
| **GET** | `/api/orgs/{org}/following` | List org following. | Any Member | N/A | `IEnumerable<FollowerDto>` |

### 7.3 Member Management

| Method | Endpoint | Description | Permission | Request Body | Response |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}/members` | List all members. | Any Member | N/A | `IEnumerable<OrganizationMemberDto>` |
| **GET** | `/api/orgs/{org}/members/me/role` | Get my Role. | Any Member | N/A | `MemberRoleDto` |
| **GET** | `/api/orgs/{org}/permissions` | Get my permissions. | Any Member | N/A | `OrganizationPermissionsDto` |
| **POST** | `/api/orgs/{org}/members` | Invite/Add member. | Owner/Admin* | `InviteMemberRequestDto` | `MessageResponse` |
| **PATCH** | `/api/orgs/{org}/members/me` | Update my info. | Any Member | `UpdateMemberRequestDto` | `MessageResponse` |
| **DELETE** | `/api/orgs/{org}/members/me` | Leave organization. | Any Member | N/A | `MessageResponse` |
| **PATCH** | `/api/orgs/{org}/members/{user}` | Update member role. | Owner | `UpdateMemberRequestDto` | `MessageResponse` |
| **DELETE** | `/api/orgs/{org}/members/{user}` | Kick member. | Owner/Admin | N/A | `MessageResponse` |

*\*Members may invite others if `AllowMemberInvite` is enabled in Settings.*

### 7.4 Invitations (Outgoing)

| Method | Endpoint | Description | Permission | Request Body | Response |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}/invitations` | List pending invites. | Owner/Admin | N/A | `IEnumerable<OrganizationInvitationDto>` |
| **POST** | `/api/orgs/{org}/invitations` | Send invitation. | Owner/Admin* | `InviteMemberRequestDto` | `MessageResponse` |
| **DELETE** | `/api/orgs/{org}/invitations/{id}` | Revoke invitation. | Owner/Admin | N/A | `MessageResponse` |

### 7.5 Sub-Resources (Portfolio Management)

- **Read Access**: Any member can GET resources to view the "Edit View".
- **Write Access**: Restricted to **Owner** and **Admin**.

| Resource (URL Part) | GET Response (DTO) | CREATE/UPDATE Request (DTO) |
| :--- | :--- | :--- |
| `projects` | `IEnumerable<ProjectDto>` | `UpdateProjectRequestDto` |
| `socials` | `IEnumerable<SocialLinkDto>` | `UpdateSocialLinkRequestDto` |
| `contacts` | `IEnumerable<ContactMethodDto>` | `UpdateContactMethodRequestDto` |
| `gallery` | `IEnumerable<GalleryItemDto>` | `UpdateGalleryItemRequestDto` |
| `certificates` | `IEnumerable<CertificateDto>` | `UpdateCertificateRequestDto` |
| `sponsorships` | `IEnumerable<SponsorshipItemDto>` | `UpdateSponsorshipItemRequestDto` |

**Permissions Logic**:
- **GET**: Authorized if User is an Organization Member. Returns all items regardless of `Visibility` setting.
- **POST**: Authorized if User is Owner or Admin.
- **PATCH** `.../{id}`: Authorized if User is Owner or Admin.
- **DELETE** `.../{id}`: Authorized if User is Owner or Admin.

### 7.6 Asset Library (Organization Management)

Manage organization asset library. Assets can be images, icons, or other reusable resources.

#### 7.6.1 Asset Management

| Method | Endpoint | Description | Permission | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}/assets` | List organization assets (paginated). | Any Member | Query: `?page=&pageSize=&category=&visibility=&search=` | `PagedResponse<AccountAssetDto>` |
| **GET** | `/api/orgs/{org}/assets/{uuid}` | Get specific organization asset. | Any Member | N/A | `AccountAssetDto` |
| **POST** | `/api/orgs/{org}/assets` | Create new organization asset. | Owner/Admin | `CreateAccountAssetRequestDto` | `AccountAssetDto` |
| **PUT** | `/api/orgs/{org}/assets/{uuid}` | Full update organization asset. | Owner/Admin | `CreateAccountAssetRequestDto` | `AccountAssetDto` |
| **PATCH** | `/api/orgs/{org}/assets/{uuid}` | Partial update organization asset. | Owner/Admin | `UpdateAccountAssetRequestDto` | `AccountAssetDto` |
| **DELETE** | `/api/orgs/{org}/assets/{uuid}` | Delete organization asset. | Owner/Admin | N/A | `MessageResponse` |

#### 7.6.2 Batch Operations

| Method | Endpoint | Description | Permission | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **PATCH** | `/api/orgs/{org}/assets/batch/visibility` | Batch update visibility. | Owner/Admin | `BatchUpdateVisibilityRequestDto` | `MessageResponse` |
| **DELETE** | `/api/orgs/{org}/assets/batch` | Batch delete assets. | Owner/Admin | `BatchDeleteRequestDto` | `MessageResponse` |

#### 7.6.3 Categories

| Method | Endpoint | Description | Permission | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}/assets/categories` | List all organization categories (distinct). | Any Member | `IEnumerable<string>` |

### 7.7 Notifications (Inbox)

Organization notifications management. Only Owner and Admin can access.

| Method | Endpoint | Description | Permission | Request Body | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}/notifications` | List notifications. | Owner/Admin | Query (`page`, `unreadOnly`) | `PagedResponse<NotificationDto>` |
| **GET** | `/api/orgs/{org}/notifications/unread-count` | Get total unread. | Owner/Admin | N/A | `int` |
| **PATCH** | `/api/orgs/{org}/notifications/{id}/read` | Mark as read. | Owner/Admin | N/A | `MessageResponse` |
| **POST** | `/api/orgs/{org}/notifications/read-all` | Mark all read. | Owner/Admin | N/A | `MessageResponse` |
| **DELETE** | `/api/orgs/{org}/notifications/{id}` | Delete notification. | Owner/Admin | N/A | `MessageResponse` |
| **DELETE** | `/api/orgs/{org}/notifications` | Delete all read. | Owner/Admin | N/A | `MessageResponse` |

---

## 8. System Administration (Dynamic / Protected)

Admins can perform operations that bypass standard checks.

| Method | Endpoint | Description | Permission | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/admin/system/status` | **System Status Overview**. | `AdminOrHigher` | `SystemStatusDto` |

### 8.1 User & Account Management
| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/admin/users` | List users. | `UserFilterDto` | `PagedResponse<UserAdminDto>` |
| **POST** | `/api/admin/users` | Force create user/org. | `CreateUserRequestDto` | `UserAdminDto` |
| **PATCH** | `/api/admin/users/{user}/status` | Change status. | `UpdateUserStatusRequestDto` | `MessageResponse` |
| **DELETE** | `/api/admin/users/{user}` | **Physical Delete**. | N/A | `MessageResponse` |
| **POST** | `/api/admin/users/{user}/role` | Promote/Demote. | `UpdateUserRoleRequestDto` | `MessageResponse` |
| **POST** | `/api/admin/users/{user}/password/reset` | **Force Reset Password**. | `AdminOrHigher` | `MessageResponse` |
| **POST** | `/api/admin/users/{user}/notifications` | **Send notification**. | `AdminOrHigher` | `MessageResponse` |
| **GET** | `/api/admin/users/{user}/emails` | **List User Emails**. | `AdminOrHigher` | `IEnumerable<AccountEmailDto>` |
| **POST** | `/api/admin/users/{user}/emails` | **Force Add Email**. | `AdminOrHigher` | `MessageResponse` |
| **PATCH** | `/api/admin/users/{user}/emails/{email}` | **Update Email Status**. | `AdminOrHigher` | `MessageResponse` |
| **DELETE** | `/api/admin/users/{user}/emails/{email}` | **Force Remove Email**. | `AdminOrHigher` | `MessageResponse` |

### 8.2 Organization Administration (Force Actions)
| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/admin/orgs/{org}/members` | **List Members**. | N/A | `IEnumerable<OrganizationMemberDto>` |
| **POST** | `/api/admin/orgs/{org}/members` | **Force Add**. | `InviteMemberRequestDto` | `MessageResponse` |
| **PATCH** | `/api/admin/orgs/{org}/members/{user}` | **Force Update**. | `UpdateMemberRequestDto` | `MessageResponse` |
| **DELETE** | `/api/admin/orgs/{org}/members/{user}` | **Force Kick**. | N/A | `MessageResponse` |

### 8.3 System Configuration (Runtime)
| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/admin/system-settings` | List all settings. | N/A | `IEnumerable<SystemSettingDto>` |
| **GET** | `/api/admin/system-settings/{key}` | Get specific setting. | N/A | `SystemSettingDto` |
| **PUT** | `/api/admin/system-settings/{key}` | **Update** setting. | `UpdateSystemSettingRequestDto` | `MessageResponse` |

### 8.4 System Assets (Admin Only)

Manage system-wide global assets reserved for system use only (e.g., default avatars, system icons).

| Method | Endpoint | Description | Request Body (DTO) | Response Data (DTO) |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/admin/assets` | List all system assets. | Query: `?page=&pageSize=&category=&visibility=` | `PagedResponse<SystemAssetDto>` |
| **GET** | `/api/admin/assets/{uuid}` | Get specific system asset. | N/A | `SystemAssetDto` |
| **POST** | `/api/admin/assets` | Create system asset. | `CreateSystemAssetRequestDto` | `SystemAssetDto` |
| **PUT** | `/api/admin/assets/{uuid}` | Full update system asset. | `CreateSystemAssetRequestDto` | `SystemAssetDto` |
| **PATCH** | `/api/admin/assets/{uuid}` | Partial update system asset. | `UpdateSystemAssetRequestDto` | `SystemAssetDto` |
| **DELETE** | `/api/admin/assets/{uuid}` | Delete system asset. | N/A | `MessageResponse` |
