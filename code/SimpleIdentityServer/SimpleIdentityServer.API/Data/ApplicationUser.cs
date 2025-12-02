using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SimpleIdentityServer.API.Data;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }

    public DateTime? EmailConfirmedAt { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? LastFailedLoginAt { get; set; }
}

