# OpenProfileServer API Specification (ALPHA)

## 1. Overview

### 1.1 Identifiers
All endpoints accepting a profile or organization identifier support two formats:
- **AccountName**: `username` (e.g., `/api/profiles/alice`)
- **UUID**: `@{guid}` (e.g., `/api/profiles/@550e8400-e29b-41d4-a716-446655440000`)

### 1.2 Status & Visibility Logic
The API response and Static Generator behavior depend on the `AccountStatus`.

| Status | API Response | Static Generator | Account Deletion | Social Actions |
| :--- | :--- | :--- | :--- | :--- |
| **Active** | Full Data | Full Generation | ✅ Allowed | ✅ Allowed |
| **Suspended** | Restricted (Basic Info Only) | Restricted (No sub-resources) | ✅ **Allowed** | ❌ Forbidden |
| **Banned** | Banned (Status Only) | Banned (Status Only) | ❌ **Forbidden (Locked)** | ❌ Forbidden |
| **PendingDeletion**| 404 / Error | Skipped | N/A (Must Restore) | ❌ Forbidden |

### 1.3 Update Semantics
- **POST**: Full Update (Replace all fields).
- **PATCH**: Partial Update (Update only provided fields).

---

## 2. Server & Meta (Public)

| Method | Endpoint | Description | Static File |
| :--- | :--- | :--- | :--- |
| **GET** | `/api` | Get server version and mode info. | `api/index.json` |

---

## 3. Business Logic Details (Block & Follow)

### 3.1 Block Priority
When User A attempts to Follow User B, the system checks:
1.  Does A Block B? → **Yes**: Follow Forbidden.
2.  Does B Block A? → **Yes**: Follow Forbidden (Return 403 or 404).

### 3.2 Block Side Effects
When User A calls `POST .../block` to block User B:
1.  If A is Following B → **Auto Unfollow**.
2.  If B is Following A → **Auto Remove Follower** (B no longer follows A).

### 3.3 Static Generation Privacy
- **Status Filtering**: When generating `followers.json` or `following.json`, users with `Status != Active` must be filtered out.
- **Privacy Settings**:
    - If `AccountSettings.ShowFollowersList == false`, the generator should produce an empty array `[]` (instead of 404) to maintain frontend consistency.
    - The same logic applies to `following.json`.

---

## 4. Public Profiles (Read-Only / Static Compatible)

These endpoints are used by the Static Generator.

| Method | Endpoint | Description | Static File |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/profiles/{profile}` | Get profile info. Content depends on Status. | `api/profiles/{profile}.json` |
| **GET** | `/api/profiles/{profile}/{resource}` | Get sub-resources (projects, gallery, etc.). | `api/profiles/{profile}/{resource}.json` |
| **GET** | `/api/profiles/{profile}/members` | Get **public** members (Org only). | `api/profiles/{profile}/members.json` |
| **GET** | `/api/profiles/{profile}/followers` | Get followers list. | `api/profiles/{profile}/followers.json` |
| **GET** | `/api/profiles/{profile}/following` | Get following list. | `api/profiles/{profile}/following.json` |

*Resources: `projects`, `gallery`, `socials`, `certificates`, `sponsorships`, `work`, `education`*

---

## 5. Personal Management (Dynamic / Protected)

### 5.1 Lifecycle
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **DELETE** | `/api/me` | **Request Account Deletion** (Cooling-off). Forbidden if Banned. | Owner |
| **POST** | `/api/me/restore` | **Restore Account** (Cancel deletion). | Owner |

### 5.2 Account & Security
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/me` | Get full account details. | Owner |
| **GET** | `/api/me/emails` | List all emails. | Owner |
| **POST** | `/api/me/emails` | Add new email. | Owner |
| **POST** | `/api/me/emails/{email}/primary` | Set as primary email. | Owner |
| **POST** | `/api/me/emails/{email}/verify` | Verify email with code. | Owner |
| **DELETE** | `/api/me/emails/{email}` | Remove email. | Owner |
| **POST** | `/api/me/password` | Change password. | Owner |
| **GET** | `/api/me/blocks` | **List blocked users**. | Owner |

### 5.3 Settings & Profile
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/me/settings` | Get personal settings. | Owner |
| **POST** | `/api/me/settings` | Full update settings. | Owner |
| **PATCH** | `/api/me/settings` | Partial update settings. | Owner |
| **GET** | `/api/me/profile` | Get profile (Edit View). | Owner |
| **POST** | `/api/me/profile` | Full update profile. | Owner |
| **PATCH** | `/api/me/profile` | Partial update profile. | Owner |

### 5.4 Invitations (Incoming)
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/me/invitations` | List received invitations. | Owner |
| **POST** | `/api/me/invitations/{id}/accept` | Accept invitation. | Owner |
| **POST** | `/api/me/invitations/{id}/decline` | Decline invitation. | Owner |

### 5.5 Social Actions
*Operator must be in **Active** status.*

| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **POST** | `/api/profiles/{profile}/follow` | **Follow** target user/org. | User (Active) |
| **DELETE** | `/api/profiles/{profile}/follow` | **Unfollow**. | User (Active) |
| **GET** | `/api/profiles/{profile}/follow` | Check if I am following this user. | User |
| **POST** | `/api/profiles/{profile}/block` | **Block** target (Auto unfollow & remove follower). | User (Active) |
| **DELETE** | `/api/profiles/{profile}/block` | **Unblock** target. | User (Active) |

### 5.6 Sub-Resources
*Endpoints: `/api/me/{resource}` and `/api/me/{resource}/{id}`*
*Methods: GET, POST (Create/Update), PATCH (Partial Update), DELETE.*

---

## 6. Organization Management (Dynamic / Protected)

### 6.1 Lifecycle
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs` | List my organizations. | User |
| **POST** | `/api/orgs` | Create new organization. | User |
| **DELETE** | `/api/orgs/{org}` | **Request Dissolution** (Forbidden if Banned). | **Owner** |
| **POST** | `/api/orgs/{org}/restore` | **Restore Organization**. | **Owner** |

### 6.2 Settings & Profile
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}` | Org Dashboard. | Member+ |
| **GET** | `/api/orgs/{org}/settings` | Get settings. | **Owner** |
| **POST** | `/api/orgs/{org}/settings` | Full update settings. | **Owner** |
| **PATCH** | `/api/orgs/{org}/settings` | Partial update settings. | **Owner** |
| **POST** | `/api/orgs/{org}/profile` | Full update profile. | **Admin+** |
| **PATCH** | `/api/orgs/{org}/profile` | Partial update profile. | **Admin+** |

### 6.3 Member Management
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}/members` | List all members. | Member+ |
| **POST** | `/api/orgs/{org}/members` | Invite/Add member. | **Admin+** |
| **PATCH** | `/api/orgs/{org}/members/me` | Update my visibility/title. | Member+ |
| **DELETE** | `/api/orgs/{org}/members/me` | Leave organization. | Member+ |
| **PATCH** | `/api/orgs/{org}/members/{user}` | Update member role. | **Owner** |
| **DELETE** | `/api/orgs/{org}/members/{user}` | Kick member. | **Owner** |

### 6.4 Invitations (Outgoing)
| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/orgs/{org}/invitations` | List pending invitations. | **Admin+** |
| **POST** | `/api/orgs/{org}/invitations` | Send invitation. | **Admin+** |
| **DELETE** | `/api/orgs/{org}/invitations/{id}` | Revoke invitation. | **Admin+** |

### 6.5 Sub-Resources
*Endpoints: `/api/orgs/{org}/{resource}`*
*Permission: **Admin+***

---

## 7. System Administration (Dynamic / Protected)

| Method | Endpoint | Description | Permission |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/admin/users` | List all users. | Admin+ |
| **POST** | `/api/admin/users` | Force create user/org. | Admin+ |
| **PATCH** | `/api/admin/users/{user}/status` | Change status (Active/Suspended/Banned). | Admin+ |
| **DELETE** | `/api/admin/users/{user}` | **Physical Delete** (No cooling-off). | **Root**/Admin |
| **POST** | `/api/admin/users/{user}/role` | Promote/Demote system role. | **Root** |
