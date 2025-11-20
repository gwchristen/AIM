using System;

namespace AIM.Models;

/// <summary>
/// Represents an authorized user in the AIM security database.
/// </summary>
public class AuthorizedUser
{
    /// <summary>
    /// Gets or sets the unique identifier for this user record.
    /// </summary>
    public int ID { get; set; }

    /// <summary>
    /// Gets or sets the username (typically Windows username).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the user's department.
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Gets or sets the user's access level.
    /// 1 = Basic, 2 = Admin, 3 = SuperAdmin
    /// </summary>
    public int AccessLevel { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the username of who created this user record.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets when this user record was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the username of who last modified this user record.
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Gets or sets when this user record was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a display name for the access level.
    /// </summary>
    public string AccessLevelName => AccessLevel switch
    {
        1 => "Basic",
        2 => "Admin",
        3 => "SuperAdmin",
        _ => "Unknown"
    };
}
