using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace VaultRoleGenerator.Services;

public interface IVaultService
{
    Task<HttpResponseMessage> CreateRoleAsync(string vaultAddress, string token, object payload, string roleName);
    Task<HttpResponseMessage> CreatePolicyAsync(string vaultAddress, string token, object payload, string policyName);
    Task<HttpResponseMessage> CreateTokenRoleAsync(string vaultAddress, string token, object payload, string roleName);
    Task<HttpResponseMessage> CreateTokenAsync(string vaultAddress, string token, object payload);
    Task<HttpResponseMessage> CreateTokenFromRoleAsync(string vaultAddress, string token, string roleName);
    Task<HttpResponseMessage> GetDatabaseConfigAsync(string vaultAddress, string token, string configName);
    Task<HttpResponseMessage> UpdateDatabaseConfigAsync(string vaultAddress, string token, object payload, string configName);
}

public class VaultService : IVaultService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VaultService> _logger;

    public VaultService(HttpClient httpClient, ILogger<VaultService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    private void AddVaultHeaders(string token)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-Vault-Token", token);
    }

    // Create database role
    public async Task<HttpResponseMessage> CreateRoleAsync(string vaultAddress, string token, object payload, string roleName)
    {
        AddVaultHeaders(token);
        var url = $"{vaultAddress}/v1/database/roles/{roleName}";
        _logger.LogInformation("Creating database role at {Url}", url);
        return await _httpClient.PostAsJsonAsync(url, payload);
    }

    // Create policy
    public async Task<HttpResponseMessage> CreatePolicyAsync(string vaultAddress, string token, object payload, string policyName)
    {
        AddVaultHeaders(token);
        var url = $"{vaultAddress}/v1/sys/policies/acl/{policyName}";
        _logger.LogInformation("Creating or updating Vault policy at {Url}", url);
        return await _httpClient.PostAsJsonAsync(url, payload);
    }

    // Create token role
    public async Task<HttpResponseMessage> CreateTokenRoleAsync(string vaultAddress, string token, object payload, string roleName)
    {
        AddVaultHeaders(token);
        var url = $"{vaultAddress}/v1/auth/token/roles/{roleName}";
        _logger.LogInformation("Creating token role at {Url}", url);
        return await _httpClient.PostAsJsonAsync(url, payload);
    }

    // Create token directly
    public async Task<HttpResponseMessage> CreateTokenAsync(string vaultAddress, string token, object payload)
    {
        AddVaultHeaders(token);
        var url = $"{vaultAddress}/v1/auth/token/create";
        _logger.LogInformation("Creating token at {Url}", url);
        return await _httpClient.PostAsJsonAsync(url, payload);
    }

    // Create token from a token role
    public async Task<HttpResponseMessage> CreateTokenFromRoleAsync(string vaultAddress, string token, string roleName)
    {
        AddVaultHeaders(token);
        var url = $"{vaultAddress}/v1/auth/token/create/{roleName}";
        _logger.LogInformation("Creating token from role at {Url}", url);
        return await _httpClient.PostAsJsonAsync(url, new { });
    }

    // Get database connection configuration
    public async Task<HttpResponseMessage> GetDatabaseConfigAsync(string vaultAddress, string token, string configName)
    {
        AddVaultHeaders(token);
        var url = $"{vaultAddress}/v1/database/config/{configName}";
        _logger.LogInformation("Getting database config at {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    // Update database connection configuration
    public async Task<HttpResponseMessage> UpdateDatabaseConfigAsync(string vaultAddress, string token, object payload, string configName)
    {
        AddVaultHeaders(token);
        var url = $"{vaultAddress}/v1/database/config/{configName}";
        _logger.LogInformation("Updating database config at {Url}", url);
        return await _httpClient.PostAsJsonAsync(url, payload);
    }
}
