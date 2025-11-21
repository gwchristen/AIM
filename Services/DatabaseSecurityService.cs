using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AIM.Models;

namespace AIM.Services;

/// <summary>
/// Provides database operations for the centralized security system.
/// Manages SQLite database for authorized users, security settings, and audit logs.
/// </summary>
public class DatabaseSecurityService
{
    private readonly string _databasePath;
    private readonly object _lockObject = new object();
    
    // Database timeout and retry configuration
    private const int DEFAULT_DB_TIMEOUT = 30; // seconds
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const int INITIAL_RETRY_DELAY_MS = 100;
    private const int MAX_RETRY_DELAY_MS = 500;

    /// <summary>
    /// Initializes a new instance of the DatabaseSecurityService.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    public DatabaseSecurityService(string databasePath)
    {
        _databasePath = databasePath;
        Debug.WriteLine($"[DatabaseSecurity] Initialized with database path: {databasePath}");
    }

    /// <summary>
    /// Gets the connection string for the SQLite database with timeout configured.
    /// </summary>
    private string ConnectionString
    {
        get
        {
            var builder = new System.Data.SQLite.SQLiteConnectionStringBuilder
            {
                DataSource = _databasePath,
                Version = 3,
                DefaultTimeout = DEFAULT_DB_TIMEOUT
            };
            return builder.ConnectionString;
        }
    }

    /// <summary>
    /// Initializes the database schema if it doesn't exist.
    /// Creates the AuthorizedUsers, SecuritySettings, and SecurityAuditLog tables.
    /// Implements retry logic with exponential backoff for database locking scenarios.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < MAX_RETRY_ATTEMPTS)
        {
            try
            {
                attempt++;
                if (attempt > 1)
                {
                    Debug.WriteLine($"[DatabaseSecurity] Retry attempt {attempt}/{MAX_RETRY_ATTEMPTS}");
                }

                // Ensure the directory exists
                var directory = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Debug.WriteLine($"[DatabaseSecurity] Created directory: {directory}");
                }

                using var connection = new SQLiteConnection(ConnectionString);
                await connection.OpenAsync();

                string createTablesScript = @"
                    CREATE TABLE IF NOT EXISTS AuthorizedUsers (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE COLLATE NOCASE,
                        FullName TEXT,
                        Department TEXT,
                        AccessLevel INTEGER DEFAULT 1,
                        IsActive BOOLEAN DEFAULT 1,
                        CreatedBy TEXT,
                        CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        ModifiedBy TEXT,
                        ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE IF NOT EXISTS SecuritySettings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT NOT NULL,
                        ModifiedBy TEXT,
                        ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE IF NOT EXISTS SecurityAuditLog (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Action TEXT NOT NULL,
                        TargetUser TEXT,
                        ModifiedBy TEXT NOT NULL,
                        Details TEXT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                ";

                using var command = new SQLiteCommand(createTablesScript, connection);
                await command.ExecuteNonQueryAsync();

                Debug.WriteLine("[DatabaseSecurity] Database schema initialized successfully");
                return; // Success - exit retry loop
            }
            catch (SQLiteException ex) when (IsDatabaseLockedException(ex) && attempt < MAX_RETRY_ATTEMPTS)
            {
                lastException = ex;
                int delayMs = CalculateRetryDelay(attempt);
                Debug.WriteLine($"[DatabaseSecurity] Database locked, retrying in {delayMs}ms (attempt {attempt}/{MAX_RETRY_ATTEMPTS})");
                await Task.Delay(delayMs);
            }
            catch (SQLiteException ex) when (IsDatabaseTimeoutException(ex))
            {
                Debug.WriteLine($"[DatabaseSecurity] Database connection timeout: {ex.Message}");
                throw new InvalidOperationException(
                    $"Database connection timed out after {DEFAULT_DB_TIMEOUT} seconds. " +
                    "The network database may be unavailable or slow to respond.", ex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseSecurity] ERROR initializing database: {ex.Message}");
                throw new InvalidOperationException($"Failed to initialize security database: {ex.Message}", ex);
            }
        }

        // All retry attempts exhausted
        Debug.WriteLine($"[DatabaseSecurity] All {MAX_RETRY_ATTEMPTS} retry attempts exhausted");
        throw new InvalidOperationException(
            $"Database initialization failed after {MAX_RETRY_ATTEMPTS} attempts. " +
            "The database may be locked by another process or the network path may be inaccessible.",
            lastException);
    }

    /// <summary>
    /// Checks if an exception is a SQLite database locked error.
    /// </summary>
    private bool IsDatabaseLockedException(SQLiteException ex)
    {
        return ex.Message.Contains("database is locked", StringComparison.OrdinalIgnoreCase) ||
               (ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase) && 
                ex.ErrorCode == 6;
    }

    /// <summary>
    /// Checks if an exception is a SQLite timeout error.
    /// </summary>
    private bool IsDatabaseTimeoutException(SQLiteException ex)
    {
        return ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Calculates retry delay with exponential backoff.
    /// </summary>
    private int CalculateRetryDelay(int attemptNumber)
    {
        int delay = INITIAL_RETRY_DELAY_MS * (1 << (attemptNumber - 1)); // Exponential: 100, 200, 400
        return Math.Min(delay, MAX_RETRY_DELAY_MS);
    }

    /// <summary>
    /// Gets all active authorized users from the database.
    /// </summary>
    public async Task<List<AuthorizedUser>> GetAuthorizedUsersAsync()
    {
        var users = new List<AuthorizedUser>();

        try
        {
            using var connection = new SQLiteConnection(ConnectionString);
            await connection.OpenAsync();

            string query = "SELECT * FROM AuthorizedUsers WHERE IsActive = 1 ORDER BY Username";
            using var command = new SQLiteCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new AuthorizedUser
                {
                    ID = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    FullName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Department = reader.IsDBNull(3) ? null : reader.GetString(3),
                    AccessLevel = reader.GetInt32(4),
                    IsActive = reader.GetBoolean(5),
                    CreatedBy = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CreatedDate = reader.GetDateTime(7),
                    ModifiedBy = reader.IsDBNull(8) ? null : reader.GetString(8),
                    ModifiedDate = reader.GetDateTime(9)
                });
            }

            Debug.WriteLine($"[DatabaseSecurity] Retrieved {users.Count} authorized users");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseSecurity] ERROR getting authorized users: {ex.Message}");
            throw;
        }

        return users;
    }

    /// <summary>
    /// Gets a specific authorized user by username.
    /// </summary>
    public async Task<AuthorizedUser?> GetUserByUsernameAsync(string username)
    {
        try
        {
            using var connection = new SQLiteConnection(ConnectionString);
            await connection.OpenAsync();

            string query = "SELECT * FROM AuthorizedUsers WHERE Username = @Username COLLATE NOCASE AND IsActive = 1";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new AuthorizedUser
                {
                    ID = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    FullName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Department = reader.IsDBNull(3) ? null : reader.GetString(3),
                    AccessLevel = reader.GetInt32(4),
                    IsActive = reader.GetBoolean(5),
                    CreatedBy = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CreatedDate = reader.GetDateTime(7),
                    ModifiedBy = reader.IsDBNull(8) ? null : reader.GetString(8),
                    ModifiedDate = reader.GetDateTime(9)
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseSecurity] ERROR getting user by username: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds a new authorized user to the database.
    /// </summary>
    public async Task AddAuthorizedUserAsync(string username, string? fullName, string? department, int accessLevel, string createdBy)
    {
        try
        {
            using var connection = new SQLiteConnection(ConnectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO AuthorizedUsers (Username, FullName, Department, AccessLevel, CreatedBy, ModifiedBy, CreatedDate, ModifiedDate)
                VALUES (@Username, @FullName, @Department, @AccessLevel, @CreatedBy, @ModifiedBy, @CreatedDate, @ModifiedDate)
            ";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@FullName", fullName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Department", department ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@AccessLevel", accessLevel);
            command.Parameters.AddWithValue("@CreatedBy", createdBy);
            command.Parameters.AddWithValue("@ModifiedBy", createdBy);
            command.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);
            command.Parameters.AddWithValue("@ModifiedDate", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();

            Debug.WriteLine($"[DatabaseSecurity] Added user: {username} with access level {accessLevel}");

            // Log the action
            await LogSecurityActionAsync("ADD_USER", username, createdBy, $"Added user with access level {accessLevel}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseSecurity] ERROR adding user: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Updates an existing authorized user.
    /// </summary>
    public async Task UpdateAuthorizedUserAsync(int userId, string? fullName, string? department, int accessLevel, string modifiedBy)
    {
        try
        {
            using var connection = new SQLiteConnection(ConnectionString);
            await connection.OpenAsync();

            string query = @"
                UPDATE AuthorizedUsers
                SET FullName = @FullName, Department = @Department, AccessLevel = @AccessLevel, 
                    ModifiedBy = @ModifiedBy, ModifiedDate = @ModifiedDate
                WHERE ID = @ID
            ";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@ID", userId);
            command.Parameters.AddWithValue("@FullName", fullName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Department", department ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@AccessLevel", accessLevel);
            command.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
            command.Parameters.AddWithValue("@ModifiedDate", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();

            Debug.WriteLine($"[DatabaseSecurity] Updated user ID: {userId}");

            // Log the action
            await LogSecurityActionAsync("MODIFY_USER", null, modifiedBy, $"Updated user ID {userId} with access level {accessLevel}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseSecurity] ERROR updating user: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Removes an authorized user (soft delete by setting IsActive = false).
    /// </summary>
    public async Task RemoveAuthorizedUserAsync(string username, string modifiedBy)
    {
        try
        {
            using var connection = new SQLiteConnection(ConnectionString);
            await connection.OpenAsync();

            string query = @"
                UPDATE AuthorizedUsers
                SET IsActive = 0, ModifiedBy = @ModifiedBy, ModifiedDate = @ModifiedDate
                WHERE Username = @Username COLLATE NOCASE
            ";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
            command.Parameters.AddWithValue("@ModifiedDate", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();

            Debug.WriteLine($"[DatabaseSecurity] Removed user: {username}");

            // Log the action
            await LogSecurityActionAsync("REMOVE_USER", username, modifiedBy, "User deactivated");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseSecurity] ERROR removing user: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets a security setting value by key.
    /// </summary>
    public async Task<string?> GetSecuritySettingAsync(string key)
    {
        try
        {
            using var connection = new SQLiteConnection(ConnectionString);
            await connection.OpenAsync();

            string query = "SELECT Value FROM SecuritySettings WHERE Key = @Key";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Key", key);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseSecurity] ERROR getting security setting: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Sets a security setting value.
    /// </summary>
    public async Task SetSecuritySettingAsync(string key, string value, string modifiedBy)
    {
        try
        {
            using var connection = new SQLiteConnection(ConnectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT OR REPLACE INTO SecuritySettings (Key, Value, ModifiedBy, ModifiedDate)
                VALUES (@Key, @Value, @ModifiedBy, @ModifiedDate)
            ";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Key", key);
            command.Parameters.AddWithValue("@Value", value);
            command.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
            command.Parameters.AddWithValue("@ModifiedDate", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();

            Debug.WriteLine($"[DatabaseSecurity] Set security setting: {key}");

            // Log password changes
            if (key == "MasterPasswordHash")
            {
                await LogSecurityActionAsync("PASSWORD_CHANGE", null, modifiedBy, "Master password changed");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseSecurity] ERROR setting security setting: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Logs a security action to the audit log.
    /// </summary>
    public async Task LogSecurityActionAsync(string action, string? targetUser, string modifiedBy, string? details)
    {
        try
        {
            using var connection = new SQLiteConnection(ConnectionString);
            await connection.OpenAsync();

            string query = @"
                INSERT INTO SecurityAuditLog (Action, TargetUser, ModifiedBy, Details, Timestamp)
                VALUES (@Action, @TargetUser, @ModifiedBy, @Details, @Timestamp)
            ";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Action", action);
            command.Parameters.AddWithValue("@TargetUser", targetUser ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
            command.Parameters.AddWithValue("@Details", details ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseSecurity] ERROR logging security action: {ex.Message}");
            // Don't throw - logging failures shouldn't break the application
        }
    }

    /// <summary>
    /// Gets recent security audit logs.
    /// </summary>
    public async Task<List<SecurityAuditLog>> GetSecurityAuditLogsAsync(int limit = 100)
    {
        var logs = new List<SecurityAuditLog>();

        try
        {
            using var connection = new SQLiteConnection(ConnectionString);
            await connection.OpenAsync();

            string query = "SELECT * FROM SecurityAuditLog ORDER BY Timestamp DESC LIMIT @Limit";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new SecurityAuditLog
                {
                    ID = reader.GetInt32(0),
                    Action = reader.GetString(1),
                    TargetUser = reader.IsDBNull(2) ? null : reader.GetString(2),
                    ModifiedBy = reader.GetString(3),
                    Details = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Timestamp = reader.GetDateTime(5)
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseSecurity] ERROR getting audit logs: {ex.Message}");
            throw;
        }

        return logs;
    }

    /// <summary>
    /// Checks if the database file exists and is accessible.
    /// </summary>
    public bool IsDatabaseAvailable()
    {
        try
        {
            return File.Exists(_databasePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DatabaseSecurity] ERROR checking database availability: {ex.Message}");
            return false;
        }
    }
}
