namespace VaultRoleGenerator.Models;

public class DatabaseSelection
{
    public string DatabaseName { get; set; } = string.Empty;
    public string Permission { get; set; } = "None";
    public bool IsSelected { get; set; }
}

public static class Permissions
{
    public const string None = "None";
    public const string ReadOnly = "ReadOnly";
    public const string ReadWrite = "ReadWrite";

    public static readonly string[] All = { None, ReadOnly, ReadWrite };
} 
