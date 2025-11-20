using System;
using System.Data.SQLite;
using System.IO;

namespace AIM.Installer
{
    /// <summary>
    /// Helper class to create and initialize the AIM security database.
    /// </summary>
    public static class DatabaseInitializer
    {
        /// <summary>
        /// Creates the AIM security database with the schema and seeds it with the initial SuperAdmin user.
        /// </summary>
        /// <param name="databasePath">Full path to the database file to create.</param>
        /// <param name="initialUsername">Username of the initial SuperAdmin user (typically the installer user).</param>
        public static void CreateAndSeedDatabase(string databasePath, string initialUsername)
        {
            try
            {
                // Ensure the directory exists
                var directory = Path.GetDirectoryName(databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // If database already exists, delete it to start fresh
                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }

                // Create the database file
                SQLiteConnection.CreateFile(databasePath);

                using var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;");
                connection.Open();

                // Create the schema
                string createTablesScript = @"
                    CREATE TABLE AuthorizedUsers (
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

                    CREATE TABLE SecuritySettings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT NOT NULL,
                        ModifiedBy TEXT,
                        ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE SecurityAuditLog (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Action TEXT NOT NULL,
                        TargetUser TEXT,
                        ModifiedBy TEXT NOT NULL,
                        Details TEXT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                ";

                using (var command = new SQLiteCommand(createTablesScript, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Seed the initial SuperAdmin user
                string seedUserScript = @"
                    INSERT INTO AuthorizedUsers (Username, FullName, Department, AccessLevel, CreatedBy, ModifiedBy, CreatedDate, ModifiedDate)
                    VALUES (@Username, @FullName, @Department, 3, 'INSTALLER', 'INSTALLER', datetime('now'), datetime('now'));

                    INSERT INTO SecurityAuditLog (Action, TargetUser, ModifiedBy, Details, Timestamp)
                    VALUES ('INITIAL_SETUP', @Username, 'INSTALLER', 'Database created and initial SuperAdmin user added', datetime('now'));
                ";

                using (var command = new SQLiteCommand(seedUserScript, connection))
                {
                    command.Parameters.AddWithValue("@Username", initialUsername);
                    command.Parameters.AddWithValue("@FullName", $"Initial Admin ({initialUsername})");
                    command.Parameters.AddWithValue("@Department", "Administration");
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create and seed database: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verifies that the database was created correctly by checking the schema and initial data.
        /// </summary>
        /// <param name="databasePath">Path to the database to verify.</param>
        /// <returns>True if the database is valid, false otherwise.</returns>
        public static bool VerifyDatabase(string databasePath)
        {
            try
            {
                if (!File.Exists(databasePath))
                    return false;

                using var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;");
                connection.Open();

                // Check that all tables exist
                string checkTablesQuery = @"
                    SELECT COUNT(*) FROM sqlite_master 
                    WHERE type='table' AND name IN ('AuthorizedUsers', 'SecuritySettings', 'SecurityAuditLog')
                ";

                using var command = new SQLiteCommand(checkTablesQuery, connection);
                var tableCount = Convert.ToInt32(command.ExecuteScalar());

                if (tableCount != 3)
                    return false;

                // Check that at least one user exists
                string checkUserQuery = "SELECT COUNT(*) FROM AuthorizedUsers WHERE IsActive = 1";
                using var userCommand = new SQLiteCommand(checkUserQuery, connection);
                var userCount = Convert.ToInt32(userCommand.ExecuteScalar());

                return userCount > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
