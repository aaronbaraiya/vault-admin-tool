# Vault Admin Tool

A C# ASP.NET Razor Pages application designed to streamline the creation of database roles, Vault token roles, and Vault access policies for HashiCorp Vault–managed SQL Server environments.

This tool automates SQL permission generation, connects to SQL Server for metadata retrieval, and securely communicates with Vault’s API to create dynamic roles and issue tokens.

> **Collaboration:** Developed in collaboration with a full-stack developer, focusing on backend implementation, UI workflow, and Vault API integration.

---

## Features

- **Multi-step workflow**
  - Step 1 — Connect to SQL Server and configure Vault role details  
  - Step 2 — Select databases and assign permissions (ReadOnly / ReadWrite)  
  - Step 3 — Auto-generate SQL creation & revocation statements  
  - Step 4 — Build Vault configurations (DB role, policy, token role, token)

- **SQL Automation**
  - Automatically generates per-database SQL statements for creating users & granting permissions.
  - Supports both ReadOnly and ReadWrite access modes.
  - Clean formatting with one SQL statement per line.

- **Vault Integration**
  - Creates Vault database roles under `/v1/database/roles/<role>`
  - Creates Vault token roles under `/v1/auth/token/roles/<role>`
  - Creates Vault policies under `/v1/sys/policy/<policy>`
  - Generates application tokens via `/v1/auth/token/create/<role>`

- **Dynamic Role Naming**
  - Auto-builds role names using pattern:  
    `environment_appname_permission_dynamic`
  - Environment inferred from the selected Vault DB Config (e.g., DEV, STG).

- **User-Friendly UI**
  - Hidden persistence fields for smooth multi-step navigation  
  - TempData usage to maintain values between stages  
  - Form validation and error reporting  
  - Copy-to-clipboard utilities for SQL, policy, and token role statements

---

## Tech Stack

- **C# / .NET 8**
- **ASP.NET Razor Pages**
- **Microsoft SQL Server**
- **HashiCorp Vault API**
- **Dependency Injection**
- **TempData & Model Binding**
- **Bootstrap 5**

---

## Installation & Setup

### Prerequisites
- .NET 8 SDK  
- SQL Server instance with valid credentials  
- Access token for HashiCorp Vault with permissions to:
  - `database/roles`
  - `sys/policy`
  - `auth/token/roles`
  - `auth/token/create`

### Running the Application
```bash
git clone <repo-url>
cd vault-admin-tool
dotnet restore
dotnet run
