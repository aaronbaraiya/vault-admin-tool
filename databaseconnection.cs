using System.ComponentModel.DataAnnotations;

namespace VaultRoleGenerator.Models;

public class DatabaseConnection
{
    // Username and Password are now retrieved from configuration, not user input
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

    // Mapping Vault DB Config Name → Actual SQL Server hostname
    private static readonly Dictionary<string, string> ServerMap = new()
    {
        { "DCIR-DEVDB", "dcir-devdb.domain-msi.local" },
        { "DCIR-STGDB", "dcir-stgdb.domain-msi.local" },
        { "MSI-DEVERPDB", "msi-deverpdb.domain-msi.local" },
        { "MSI-STGERPDB", "msi-stgerpdbl.domain-msi.local" }
    };

    public string BuildConnectionString(string username, string password)
    {
        if (!ServerMap.TryGetValue(VaultDbConfigName, out var server))
            throw new InvalidOperationException($"Unknown Vault DB Config: {VaultDbConfigName}");

        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = server, // no port → defaults to 1433
            UserID = username,
            Password = password,
            TrustServerCertificate = TrustServerCertificate
        };
        
        // Explicitly set IntegratedSecurity to false AFTER construction to ensure SQL Authentication
        builder.IntegratedSecurity = false;

        if (!string.IsNullOrEmpty(InitialDatabase))
            builder.InitialCatalog = InitialDatabase;

        return builder.ConnectionString;
    }
}
