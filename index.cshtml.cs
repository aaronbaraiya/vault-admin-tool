using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VaultRoleGenerator.Models;
using VaultRoleGenerator.Services;
using System.Text;
using System.Text.Json;

namespace VaultRoleGenerator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabaseService _databaseService;
    private readonly IVaultService _vaultService;
    private readonly IConfiguration _configuration;

    [BindProperty] public string CreationStatements { get; set; } = string.Empty;
    [BindProperty] public string RevocationStatements { get; set; } = string.Empty;
    [BindProperty] public string PolicyStatement { get; set; } = string.Empty;
    [BindProperty] public string TokenRoleStatement { get; set; } = string.Empty;

    [BindProperty] public DatabaseConnection Connection { get; set; } = new();
    [BindProperty] public List<DatabaseSelection> DatabaseSelections { get; set; } = new();
    [BindProperty] public string? Action { get; set; }
    [BindProperty] public string? GeneratedToken { get; set; }

    public bool ShowDatabaseList { get; set; }
    public bool ShowSql { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public IndexModel(
        ILogger<IndexModel> logger,
        IDatabaseService databaseService,
        IVaultService vaultService,
        IConfiguration configuration)
    {
        _logger = logger;
        _databaseService = databaseService;
        _vaultService = vaultService;
        _configuration = configuration;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove(nameof(CreationStatements));
        ModelState.Remove(nameof(RevocationStatements));
        ModelState.Remove(nameof(PolicyStatement));
        ModelState.Remove(nameof(TokenRoleStatement));

        if (Action == "test-connection")
            return await TestConnectionAsync();

        if (Action == "load-databases")
            return await LoadDatabasesAsync();

        if (Action == "generate-sql")
            return GenerateSqlAsync();

        if (Action == "build-vault")
            return await BuildVaultAsync();

        return Page();
    }

    private async Task<IActionResult> TestConnectionAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var username = _configuration["SqlUsername"] ?? throw new InvalidOperationException("SqlUsername not configured");
            var password = _configuration["SqlPassword"] ?? throw new InvalidOperationException("SqlPassword not configured");
            
            var conn = Connection.BuildConnectionString(username, password);
            var ok = await _databaseService.TestConnectionAsync(conn);

            SuccessMessage = ok
                ? "Connection successful! You can now load the database list."
                : "Connection failed. Please check your credentials and try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            ErrorMessage = "Connection test failed. Please check your connection details.";
        }

        return Page();
    }

    private async Task<IActionResult> LoadDatabasesAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var username = _configuration["SqlUsername"] ?? throw new InvalidOperationException("SqlUsername not configured");
            var password = _configuration["SqlPassword"] ?? throw new InvalidOperationException("SqlPassword not configured");
            
            var conn = Connection.BuildConnectionString(username, password);
            var dbs = await _databaseService.GetDatabaseListAsync(conn);

            DatabaseSelections = dbs.Select(db => new DatabaseSelection
            {
                DatabaseName = db,
                Permission = Permissions.None,
                IsSelected = false
            }).ToList();

            ShowDatabaseList = true;
            SuccessMessage = $"Successfully loaded {dbs.Count} databases.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load databases");
            ErrorMessage = ex.Message;
        }

        return Page();
    }

    private IActionResult GenerateSqlAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var selected = DatabaseSelections
            .Where(ds => ds.IsSelected && ds.Permission != Permissions.None)
            .ToList();

        if (!selected.Any())
        {
            ErrorMessage = "Please select at least one database with permissions.";
            ShowDatabaseList = true;
            return Page();
        }

        GenerateSql(selected);
        ShowSql = true;

        return Page();
    }

    private void GenerateSql(List<DatabaseSelection> selected)
    {
        var creation = new StringBuilder();
        var rev = new StringBuilder();
        var policy = new StringBuilder();
        var tokenRole = new StringBuilder();

        creation.AppendLine("CREATE LOGIN [{{name}}] WITH PASSWORD = '{{password}}';");
        rev.AppendLine("DROP LOGIN [{{name}}];");

        policy.AppendLine($"# Vault Policy for {Connection.RoleName}");
        policy.AppendLine("path \"database/creds/" + Connection.RoleName + "\" {");
        policy.AppendLine("  capabilities = [ \"read\" ]");
        policy.AppendLine("}");

        bool anyRW = selected.Any(s => s.Permission == Permissions.ReadWrite);
        string suf = anyRW ? "rw" : "ro";

        tokenRole.AppendLine($"vault write auth/token/roles/{Connection.AppName}_{suf} " +
                             $"allowed_policies=\"{Connection.RoleName}_policy\" period=720h");

        foreach (var s in selected)
        {
            if (s.Permission == Permissions.ReadOnly)
            {
                creation.AppendLine($"USE [{s.DatabaseName}];");
                creation.AppendLine($"CREATE USER [{{{{name}}}}] FOR LOGIN [{{{{name}}}}];");
                creation.AppendLine($"GRANT SELECT TO [{{{{name}}}}];");
                rev.AppendLine($"USE [{s.DatabaseName}];");
                rev.AppendLine($"DROP USER IF EXISTS [{{{{name}}}}];");
            }
            else
            {
                creation.AppendLine($"USE [{s.DatabaseName}];");
                creation.AppendLine($"CREATE USER [{{{{name}}}}] FOR LOGIN [{{{{name}}}}];");
                creation.AppendLine($"GRANT SELECT, UPDATE, INSERT, DELETE, EXECUTE TO [{{{{name}}}}];");
                rev.AppendLine($"USE [{s.DatabaseName}];");
                rev.AppendLine($"DROP USER IF EXISTS [{{{{name}}}}];");
            }
        }

        CreationStatements = creation.ToString().Trim();
        RevocationStatements = rev.ToString().Trim();
        PolicyStatement = policy.ToString().Trim();
        TokenRoleStatement = tokenRole.ToString().Trim();
    }


    private async Task<IActionResult> BuildVaultAsync()
    {
        try
        {
            var vaultAddress = "vaultAddress";
            var vaultToken = "vaultToken";

            // GROUP CREATION STATEMENTS
            var groupedCreation = new List<string>();
            var buffer = new List<string>();

            foreach (var line in (CreationStatements ?? "")
                     .Split(';', StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => s.Trim()))
            {
                if (line.StartsWith("USE [", StringComparison.OrdinalIgnoreCase))
                {
                    if (buffer.Count > 0)
                    {
                        groupedCreation.Add(string.Join("; ", buffer) + ";");
                        buffer.Clear();
                    }
                }
                buffer.Add(line);
            }
            if (buffer.Count > 0)
                groupedCreation.Add(string.Join("; ", buffer) + ";");

            // GROUP REVOCATION STATEMENTS
            var groupedRev = new List<string>();
            buffer = new List<string>();

            foreach (var line in (RevocationStatements ?? "")
                     .Split(';', StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => s.Trim()))
            {
                if (line.StartsWith("USE [", StringComparison.OrdinalIgnoreCase))
                {
                    if (buffer.Count > 0)
                    {
                        groupedRev.Add(string.Join("; ", buffer) + ";");
                        buffer.Clear();
                    }
                }
                buffer.Add(line);
            }
            if (buffer.Count > 0)
                groupedRev.Add(string.Join("; ", buffer) + ";");


            // UPDATE DATABASE CONFIG TO ADD ALLOWED ROLE
            var configResp = await _vaultService.GetDatabaseConfigAsync(vaultAddress, vaultToken, Connection.VaultDbConfigName);
            if (!configResp.IsSuccessStatusCode)
            {
                ErrorMessage = $"Failed to get database configuration '{Connection.VaultDbConfigName}'.";
                return Page();
            }

            var configBody = await configResp.Content.ReadAsStringAsync();
            var configJson = JsonDocument.Parse(configBody);
            var dataElement = configJson.RootElement.GetProperty("data");

            // Get current allowed_roles or create empty list
            var allowedRoles = new List<string>();
            if (dataElement.TryGetProperty("allowed_roles", out var allowedRolesElement))
            {
                if (allowedRolesElement.ValueKind == JsonValueKind.Array)
                {
                    allowedRoles = allowedRolesElement.EnumerateArray()
                        .Select(e => e.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
            }

            // Add new role if not already present
            if (!allowedRoles.Contains(Connection.RoleName))
            {
                allowedRoles.Add(Connection.RoleName);

                // Update the config with new allowed_roles
                var updateConfigPayload = new
                {
                    allowed_roles = allowedRoles.ToArray()
                };

                var updateConfigResp = await _vaultService.UpdateDatabaseConfigAsync(vaultAddress, vaultToken, updateConfigPayload, Connection.VaultDbConfigName);
                if (!updateConfigResp.IsSuccessStatusCode)
                {
                    ErrorMessage = "Failed to update database configuration with new allowed role.";
                    return Page();
                }

                _logger.LogInformation("Added role '{RoleName}' to allowed_roles for config '{ConfigName}'", Connection.RoleName, Connection.VaultDbConfigName);
            }


            // CREATE ROLE
            var rolePayload = new
            {
                db_name = Connection.VaultDbConfigName,
                creation_statements = groupedCreation.ToArray(),
                revocation_statements = groupedRev.ToArray(),
                default_ttl = "1h",
                max_ttl = "12h"
            };

            var roleResp = await _vaultService.CreateRoleAsync(vaultAddress, vaultToken, rolePayload, Connection.RoleName);
            var roleBody = await roleResp.Content.ReadAsStringAsync();
            _logger.LogInformation("Role => {code}: {body}", roleResp.StatusCode, roleBody);

            if (!roleResp.IsSuccessStatusCode)
            {
                ErrorMessage = "Failed to create Vault role.";
                return Page();
            }


            // CREATE POLICY
            var policyPayload = new { policy = PolicyStatement };
            string policyName = $"{Connection.RoleName}_policy";

            var policyResp = await _vaultService.CreatePolicyAsync(vaultAddress, vaultToken, policyPayload, policyName);
            if (!policyResp.IsSuccessStatusCode)
            {
                ErrorMessage = "Failed to create Vault policy.";
                return Page();
            }


            // CREATE TOKEN ROLE
            bool hasRW = DatabaseSelections.Any(s => s.IsSelected && s.Permission == Permissions.ReadWrite);
            string suffix = hasRW ? "rw" : "ro";
            string tokenRoleName = $"{Connection.AppName}_{suffix}";

            var tokenRolePayload = new
            {
                allowed_policies = new[] { $"{Connection.RoleName}_policy" },
                period = "720h",
                orphan = true,
                display_name = tokenRoleName
            };

            var tokenRoleResp = await _vaultService.CreateTokenRoleAsync(vaultAddress, vaultToken, tokenRolePayload, tokenRoleName);
            if (!tokenRoleResp.IsSuccessStatusCode)
            {
                ErrorMessage = "Failed to create Vault token role.";
                return Page();
            }


            // CREATE TOKEN
            var tokenResp = await _vaultService.CreateTokenFromRoleAsync(vaultAddress, vaultToken, tokenRoleName);
            var tokenBody = await tokenResp.Content.ReadAsStringAsync();

            if (!tokenResp.IsSuccessStatusCode)
            {
                ErrorMessage = "Failed to create Vault token.";
                return Page();
            }

            var json = JsonDocument.Parse(tokenBody);
            GeneratedToken = json.RootElement
                                 .GetProperty("auth")
                                 .GetProperty("client_token")
                                 .GetString();

            SuccessMessage = "Vault role, token role, and token created successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vault build failed");
            ErrorMessage = "Error occurred while creating Vault configurations.";
        }

        return Page();
    }
}
