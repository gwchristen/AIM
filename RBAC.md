# Role-Based Access Control (RBAC) Guide

## Overview

AIM implements a three-tier role-based access control (RBAC) system managed through a centralized SQLite database. This document provides a complete reference for understanding, implementing, and managing user roles and permissions.

## Table of Contents

- [Access Levels](#access-levels)
- [Permission Matrix](#permission-matrix)
- [User Management](#user-management)
- [Implementation Details](#implementation-details)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

---

## Access Levels

### Level 1: Basic User

**Purpose**: Read-only access for end users who need to view and search assets.

**Capabilities**:
- ✅ Browse directory structures
- ✅ View file contents
- ✅ Search by filename
- ✅ Search by file content
- ✅ View inventory data
- ✅ View audit logs
- ✅ Change personal theme preferences

**Restrictions**:
- ❌ Cannot modify directory settings
- ❌ Cannot manage users
- ❌ Cannot change configuration paths
- ❌ Cannot clear audit logs
- ❌ Cannot delete or move files
- ❌ Cannot access Admin Tools

**Use Cases**:
- Warehouse staff viewing inventory
- End users searching for assets
- Read-only access for reporting purposes

---

### Level 2: Admin

**Purpose**: Operational administrators who manage users and configuration.

**Capabilities**:
- ✅ All Basic User permissions
- ✅ Manage users (add, edit, remove)
- ✅ Modify directory paths via Settings
- ✅ Change configuration settings
- ✅ Clear audit logs
- ✅ Access Admin Tools
- ✅ Move, copy, delete files
- ✅ Batch operations (rename, clone, archive)

**Restrictions**:
- ⚠️ Cannot change security database location (requires reinstall)
- ⚠️ Subject to organizational policies

**Use Cases**:
- IT staff managing user access
- System administrators configuring paths
- Operations managers performing maintenance
- Power users needing file management

---

### Level 3: SuperAdmin

**Purpose**: Full system access for IT administrators.

**Capabilities**:
- ✅ All Admin permissions
- ✅ Full unrestricted access to all features
- ✅ Cannot be restricted by organizational policies
- ✅ Automatically created during installation

**Restrictions**:
- ⚠️ Should be limited to IT administrators only
- ⚠️ Cannot change database location (requires reinstall)

**Use Cases**:
- IT administrators with full control
- System setup and initial configuration
- Emergency access and troubleshooting

**Security Note**: SuperAdmin should be reserved for IT staff only. Create Admin users for operational management and Basic users for general access.

---

## Permission Matrix

| Feature | Basic | Admin | SuperAdmin |
|---------|:-----:|:-----:|:----------:|
| **General Access** |
| Browse files/directories | ✅ | ✅ | ✅ |
| View file contents | ✅ | ✅ | ✅ |
| Search files | ✅ | ✅ | ✅ |
| View inventory | ✅ | ✅ | ✅ |
| View audit logs | ✅ | ✅ | ✅ |
| Change theme | ✅ | ✅ | ✅ |
| **File Operations** |
| Move files | ❌ | ✅ | ✅ |
| Copy files | ❌ | ✅ | ✅ |
| Delete files | ❌ | ✅ | ✅ |
| Rename files | ❌ | ✅ | ✅ |
| **Configuration** |
| Change directory paths | ❌ | ✅ | ✅ |
| Modify settings | ❌ | ✅ | ✅ |
| Clear audit logs | ❌ | ✅ | ✅ |
| **User Management** |
| View users | ❌ | ✅ | ✅ |
| Add users | ❌ | ✅ | ✅ |
| Edit users | ❌ | ✅ | ✅ |
| Remove users | ❌ | ✅ | ✅ |
| Change access levels | ❌ | ✅ | ✅ |
| **Admin Tools** |
| Directory analysis | ❌ | ✅ | ✅ |
| Batch renamer | ❌ | ✅ | ✅ |
| Directory cloner | ❌ | ✅ | ✅ |
| Inventory archiver | ❌ | ✅ | ✅ |

---

## User Management

### Initial Setup

During installation, the AIM installer automatically:

1. Creates the security database at the configured network location
2. Initializes the database schema
3. Adds the current Windows user as SuperAdmin (Level 3)

**First SuperAdmin Record:**
```sql
Username: [Current Windows User]
FullName: Initial SuperAdmin
Department: IT
AccessLevel: 3 (SuperAdmin)
IsActive: true
CreatedBy: Installer
```

### Adding Users

**Requirements**: Admin or SuperAdmin access

**Steps**:

1. Navigate to **Settings → User Management**
2. Click **Add User**
3. Fill in user details:
   - **Username**: Windows username (e.g., `jdoe`, `DOMAIN\jdoe`)
   - **Full Name**: User's full name (e.g., `John Doe`)
   - **Department**: User's department (e.g., `Warehouse`, `IT`)
   - **Access Level**: Select Basic, Admin, or SuperAdmin
4. Click **Save**

**Validation**:
- Username must be unique
- Username should match Windows username format
- Full Name is required
- Access Level must be 1, 2, or 3

**Synchronization**:
- Changes sync to all AIM instances within 30 seconds
- User can access AIM immediately on next launch
- Refresh manually via "Refresh Users" button if needed

### Editing Users

**Requirements**: Admin or SuperAdmin access

**Steps**:

1. Navigate to **Settings → User Management**
2. Select user from list
3. Click **Edit**
4. Modify allowed fields:
   - Full Name
   - Department
   - Access Level
5. Click **Save**

**Restrictions**:
- Cannot edit Username (primary key)
- Cannot edit your own access level
- Changes are audited in SecurityAuditLog

### Removing Users

**Requirements**: Admin or SuperAdmin access

**Steps**:

1. Navigate to **Settings → User Management**
2. Select user from list
3. Click **Remove**
4. Confirm removal

**Behavior**:
- User is **soft-deleted** (IsActive = false)
- User record remains in database for audit purposes
- User immediately loses access to AIM
- Can be reactivated by Admin if needed

**Restrictions**:
- Cannot remove yourself
- Cannot remove last SuperAdmin

### Access Level Changes

**Promoting Users**:
- Basic → Admin: User gains configuration and user management access
- Admin → SuperAdmin: User gains full unrestricted access

**Demoting Users**:
- SuperAdmin → Admin: User loses unrestricted access, still has management capabilities
- Admin → Basic: User loses all management capabilities, read-only access only

**Best Practice**: Use the minimum access level required for each user's role.

---

## Implementation Details

### Database Schema

**AuthorizedUsers Table:**

```sql
CREATE TABLE AuthorizedUsers (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    FullName TEXT,
    Department TEXT,
    AccessLevel INTEGER NOT NULL DEFAULT 1,  -- 1=Basic, 2=Admin, 3=SuperAdmin
    IsActive INTEGER NOT NULL DEFAULT 1,     -- 1=Active, 0=Inactive
    CreatedBy TEXT,
    CreatedDate TEXT NOT NULL,
    ModifiedBy TEXT,
    ModifiedDate TEXT NOT NULL
);
```

### Access Control Checks

**In Code (C#):**

```csharp
// Check if user is authorized
if (!_securityService.IsFullyUnlocked)
{
    await _dialogService.ShowErrorAsync("Access Denied", "You are not authorized to use AIM.");
    return;
}

// Check for Admin access
if (!_securityService.IsCurrentUserAdmin())
{
    await _dialogService.ShowErrorAsync("Access Denied", "This feature requires Admin access.");
    return;
}

// Check for SuperAdmin access
if (!_securityService.IsCurrentUserSuperAdmin())
{
    await _dialogService.ShowErrorAsync("Access Denied", "This feature requires SuperAdmin access.");
    return;
}

// Get current user's access level
int accessLevel = _securityService.GetCurrentUserAccessLevel();
if (accessLevel < 2) // Not Admin+
{
    // Disable or hide feature
}
```

**In XAML (UI Binding):**

```xml
<!-- Show element only for Admin+ users -->
<Button 
    Content="Manage Users" 
    Visibility="{x:Bind ViewModel.IsCurrentUserAdmin, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}"
    Command="{x:Bind ViewModel.ManageUsersCommand}" />

<!-- Show element only for SuperAdmin -->
<TextBlock 
    Text="SuperAdmin Access" 
    Visibility="{x:Bind ViewModel.IsCurrentUserSuperAdmin, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />
```

### Synchronization Mechanism

**Automatic Refresh (Every 30 seconds):**

```csharp
// In SecurityService.cs
private Timer _userSyncTimer;

private void StartUserSyncTimer()
{
    _userSyncTimer = new Timer(30000); // 30 seconds
    _userSyncTimer.Elapsed += async (s, e) => 
    {
        await RefreshUsersFromDatabaseAsync();
    };
    _userSyncTimer.Start();
}
```

**Manual Refresh:**

Users can click "Refresh Users" button in Settings → User Management to immediately sync from database.

### Audit Logging

All user management operations are logged to SecurityAuditLog table:

```sql
CREATE TABLE SecurityAuditLog (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp TEXT NOT NULL,
    Username TEXT NOT NULL,           -- Who performed the action
    Action TEXT NOT NULL,              -- ADD_USER, EDIT_USER, REMOVE_USER, etc.
    TargetUser TEXT,                   -- Which user was affected
    Details TEXT,                      -- JSON with change details
    Success INTEGER NOT NULL           -- 1=Success, 0=Failure
);
```

**Logged Actions**:
- User added
- User edited (with before/after values)
- User removed
- Access level changed
- User activated/deactivated

---

## Best Practices

### Access Level Assignment

**Basic Users** (Level 1):
- Warehouse staff
- General users who only need to view data
- External contractors (with appropriate access controls)
- Temporary employees

**Admin Users** (Level 2):
- Department managers
- Operations staff who configure settings
- Power users who perform file operations
- Anyone who needs to manage other users

**SuperAdmin Users** (Level 3):
- IT administrators
- System administrators
- Application owners
- Emergency access only

### Security Recommendations

1. **Principle of Least Privilege**:
   - Assign the minimum access level required
   - Regularly review user access levels
   - Remove access when no longer needed

2. **SuperAdmin Limitation**:
   - Keep SuperAdmin count to minimum (2-3 users)
   - Document who has SuperAdmin access
   - Review SuperAdmin list quarterly

3. **Regular Audits**:
   - Review SecurityAuditLog regularly
   - Monitor for unusual access patterns
   - Verify inactive users are deactivated

4. **User Lifecycle**:
   - Add users when onboarded
   - Update access levels when roles change
   - Deactivate users when they leave organization
   - Never delete users (preserve audit trail)

5. **Documentation**:
   - Maintain list of users and their roles
   - Document access level justifications
   - Keep organizational chart up to date

### User Naming Conventions

**Windows Usernames**:
- Use organizational standard (e.g., `firstname.lastname` or `flastname`)
- Include domain if applicable (e.g., `DOMAIN\username`)
- Be consistent across organization

**Full Names**:
- Use proper capitalization (e.g., `John Doe`)
- Include middle initial if needed (e.g., `John M. Doe`)
- Keep format consistent

**Departments**:
- Use standardized department names
- Keep list of valid departments
- Use abbreviations consistently (e.g., `IT` vs `Information Technology`)

---

## Troubleshooting

### Common Issues

#### User Cannot Access AIM

**Symptoms**: User gets "Access Denied" message on launch

**Causes**:
- User not in AuthorizedUsers table
- User's IsActive = false
- Database not accessible from user's machine

**Solutions**:
1. Verify user exists in database (Settings → User Management)
2. Check user's IsActive status
3. Verify network path to database is accessible
4. Check SecurityDatabasePath in settings.json
5. Add user if missing

#### User Has Wrong Access Level

**Symptoms**: User cannot access expected features

**Causes**:
- User assigned wrong access level
- Access level not synced yet

**Solutions**:
1. Check user's access level in database
2. Edit user and change access level if needed
3. Wait 30 seconds for sync or click "Refresh Users"
4. User should restart AIM

#### Changes Not Syncing

**Symptoms**: User changes don't appear in other instances

**Causes**:
- Sync timer not running
- Database connection lost
- Network issues

**Solutions**:
1. Click "Refresh Users" button
2. Check database is accessible
3. Verify network connectivity
4. Restart AIM if needed
5. Check application logs for errors

#### Cannot Remove User

**Symptoms**: Remove button disabled or operation fails

**Causes**:
- Trying to remove yourself
- Trying to remove last SuperAdmin
- Database permission issues

**Solutions**:
1. Have another Admin remove the user
2. Ensure at least 2 SuperAdmins exist before removing one
3. Check database write permissions

### Database Issues

#### Database Not Found

**Error**: "Security database not found"

**Solution**:
1. Verify SecurityDatabasePath in settings.json is correct
2. Check network path is accessible
3. Verify database file exists at that location
4. Check file permissions

#### Database Locked

**Error**: "Database is locked"

**Solution**:
1. Close other applications accessing database
2. Wait a few seconds and retry
3. Check for file locks on database
4. Restart AIM

#### Schema Mismatch

**Error**: "Database schema is invalid"

**Solution**:
1. Database may be corrupted
2. Restore from backup if available
3. May need to reinstall AIM to recreate database
4. Contact IT support

### Audit Logging

#### View Security Audit Log

**From Database** (SQL query):

```sql
SELECT 
    Timestamp,
    Username,
    Action,
    TargetUser,
    Details,
    CASE Success WHEN 1 THEN 'Success' ELSE 'Failure' END as Status
FROM SecurityAuditLog
ORDER BY Timestamp DESC
LIMIT 100;
```

**From Application**:
- Navigate to Log Viewer for application logs
- Database audit log must be queried directly (future enhancement)

---

## API Reference

### SecurityService Methods

```csharp
// Check authorization
public bool IsFullyUnlocked { get; }
public int GetCurrentUserAccessLevel()
public bool IsCurrentUserAdmin()
public bool IsCurrentUserSuperAdmin()

// User management (Admin+ required)
public async Task RefreshUsersFromDatabaseAsync()
```

### DatabaseSecurityService Methods

```csharp
// User CRUD operations
public async Task<List<AuthorizedUser>> GetAuthorizedUsersAsync()
public async Task AddAuthorizedUserAsync(AuthorizedUser user)
public async Task UpdateAuthorizedUserAsync(AuthorizedUser user)
public async Task RemoveAuthorizedUserAsync(string username)

// Audit logging
public async Task LogSecurityActionAsync(
    string action, 
    string targetUser, 
    string details, 
    bool success)
```

---

## Appendices

### Appendix A: Access Level Codes

| Code | Name | Description |
|------|------|-------------|
| 0 | None | No access (not authorized) |
| 1 | Basic | Read-only access |
| 2 | Admin | Configuration and user management |
| 3 | SuperAdmin | Full unrestricted access |

### Appendix B: Security Action Types

| Action | Description |
|--------|-------------|
| `ADD_USER` | New user added to database |
| `EDIT_USER` | User details modified |
| `REMOVE_USER` | User deactivated |
| `CHANGE_ACCESS_LEVEL` | User's access level changed |
| `USER_LOGIN_SUCCESS` | User successfully authenticated |
| `USER_LOGIN_FAILURE` | User authentication failed |
| `ACCESS_DENIED` | User attempted unauthorized operation |

### Appendix C: Sample Database Queries

**List all active users:**
```sql
SELECT Username, FullName, Department, AccessLevel 
FROM AuthorizedUsers 
WHERE IsActive = 1
ORDER BY AccessLevel DESC, Username;
```

**Count users by access level:**
```sql
SELECT 
    AccessLevel,
    CASE AccessLevel
        WHEN 1 THEN 'Basic'
        WHEN 2 THEN 'Admin'
        WHEN 3 THEN 'SuperAdmin'
    END as LevelName,
    COUNT(*) as UserCount
FROM AuthorizedUsers
WHERE IsActive = 1
GROUP BY AccessLevel;
```

**Recent security events:**
```sql
SELECT 
    datetime(Timestamp) as Time,
    Username as Who,
    Action as What,
    TargetUser as Whom,
    Details
FROM SecurityAuditLog
WHERE Success = 1
ORDER BY Timestamp DESC
LIMIT 20;
```

---

## Conclusion

Role-based access control in AIM provides a flexible, secure, and auditable way to manage user permissions. By following the best practices outlined in this document, administrators can ensure appropriate access levels are assigned and maintained across the organization.

For additional information:
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture and security design
- [IMPLEMENTATION-DATABASE-SECURITY.md](IMPLEMENTATION-DATABASE-SECURITY.md) - Implementation details
- [README.md](README.md) - General application overview

For support, open an issue on the [GitHub repository](https://github.com/gwchristen/AIM/issues).
