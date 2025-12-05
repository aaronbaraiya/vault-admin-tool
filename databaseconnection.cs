using System.ComponentModel.DataAnnotations;

namespace VaultRoleGenerator.Models;

public class DatabaseConnection
{
    // Username and Password are provided externally (not hard-coded)
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Database Name (Optional)")]
    public string? InitialDatabase { get; set; }

    [Display(Name = "Trust Server Certificate")]
    public bool TrustServerCertificate { get; set; } = true;

    [Required(ErrorMessage = "Vault Role Name is required")]
    [Display(Name = "Vault Role Name")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Role name can only contain letters, numbers, underscores, and hyphens")]
    public string RoleName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Application Name is required")]
    [Display(Name = "Application Name")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Application name can only contain letters, numbers, underscores, and hyphens")]
    public string AppName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role Permission")]
    public string RolePermission { get; set; } = "rw";

    [Required(ErrorMessage = "Server Name is required")]
    [Display(Name = "Server Name")]
    public string VaultDbConfigName { get; set; } = string.Empty;

    // Mapping Vault DB Config Name → SQL Server hostname
    // IMPORTANT: Replace internal hostnames with placeholders before publishing to GitHub
    private static readonly Dictionary<string, string> ServerMap = new()
    {
        // Example placeholders only the actual names were removed for safely purposes 
        { "dev-sql", "dev-sql.example.com" },
        { "stage-sql", "stage-sql.example.com" },
        { "prod-sql", "prod-sql.example.com" }
    };

    public string BuildConnectionString(string username, string password)
    {
        if (!ServerMap.TryGetValue(VaultDbConfigName, out var server))
            throw new InvalidOperationException($"Unknown Vault DB Config: {VaultDbConfigName}");

        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = server,
            UserID = username,
            Password = password,
            TrustServerCertificate = TrustServerCertificate
        };

        // Explicitly disable Integrated Security – SQL Auth only
        builder.IntegratedSecurity = false;

        if (!string.IsNullOrEmpty(InitialDatabase))
            builder.InitialCatalog = InitialDatabase;

        return builder.ConnectionString;
    }
}
