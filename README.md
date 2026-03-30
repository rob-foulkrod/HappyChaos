# HappyChaos

A .NET 10 ASP.NET Core Todo/Task management application with an integrated [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server for AI tool integration.

## MCP Server

HappyChaos exposes an MCP server over HTTP that allows AI assistants (such as GitHub Copilot) to manage tasks. The server is **unauthenticated** and uses the official [ModelContextProtocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) (v1.2.0).

### Endpoint

The MCP server endpoint for the default Azure deployment is:

```
https://happychaos.azurewebsites.net/mcp
```

### GitHub Copilot Configuration

To use the MCP server with GitHub Copilot in VS Code, add the following to your `.vscode/mcp.json` (or your user `settings.json` under `"mcp"`):

```json
{
  "servers": {
    "happychaos-tasks": {
      "type": "http",
      "url": "https://happychaos.azurewebsites.net/mcp"
    }
  }
}
```

### Available MCP Tools

| Tool | Description | Parameters |
|------|-------------|------------|
| **GetTasks** | Get all tasks, optionally filtered by status | `status` (optional): `NotStarted`, `InProgress`, `OnHold`, `Completed` |
| **GetTaskById** | Get a specific task by its ID | `id` (required): Task ID |
| **AddTask** | Create a new task | `title` (required), `description`, `priority` (`Low`/`Medium`/`High`/`Critical`), `status`, `dueDate` (ISO 8601), `assignedTo`, `category` |
| **EditTask** | Update an existing task (only provided fields are changed) | `id` (required), `title`, `description`, `priority`, `status`, `dueDate`, `assignedTo`, `category` |
| **DeleteTask** | Delete a task by ID | `id` (required): Task ID |
| **GetTaskSummary** | Get a summary with counts by status, overdue, and due soon | _(none)_ |
| **SearchTasks** | Search tasks by keyword in title, description, and assignee | `keyword` (required): Search term |
| **ExportBackup** | Export all tasks as a JSON backup (includes tasks and categories) | _(none)_ |
| **RestoreBackup** | Restore tasks from a JSON backup (replaces all existing tasks) | `backupJson` (required): JSON string from ExportBackup |

### Task Model

Each task has the following properties:

| Property | Type | Description |
|----------|------|-------------|
| `Id` | int | Auto-assigned unique identifier |
| `Title` | string | Task title (2–200 characters, required) |
| `Description` | string? | Task description (max 1000 characters) |
| `Priority` | enum | `Low`, `Medium`, `High`, or `Critical` |
| `Status` | enum | `NotStarted`, `InProgress`, `OnHold`, or `Completed` |
| `DueDate` | DateTime? | Optional due date |
| `AssignedTo` | string? | Optional assignee name |
| `Category` | string? | Optional category label |
| `CreatedAt` | DateTime | UTC timestamp when created |
| `UpdatedAt` | DateTime | UTC timestamp when last updated |

## Configuration

All settings live in `appsettings.json` (or per-environment overrides like `appsettings.Development.json`) and can be overridden via environment variables using the standard `__` separator (e.g., `Storage__Provider`).

### General

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `DEFAULT_MESSAGE` | string | `"Welcome to HappyChaos Todo!"` | Message displayed on the home page |

### Storage Provider (`Storage` section)

| Setting | Values | Default | Description |
|---------|--------|---------|-------------|
| `Storage:Provider` | `InMemory`, `Blob` | `InMemory` | Which storage backend to use |

#### InMemory (default)

No additional settings required. Data is held in memory and lost on restart. Starts with 5 seed tasks.

#### Azure Blob Storage (`Storage:Provider` = `Blob`)

Tasks are persisted as a single JSON blob in Azure Blob Storage. Auth is determined by which settings are present:

| Setting | Required | Default | Description |
|---------|----------|---------|-------------|
| `Storage:BlobStorage:AccountName` | Yes (if no `ConnectionString`) | — | Azure Storage account name. Used with `DefaultAzureCredential` (Managed Identity, Azure CLI, VS login, etc.) |
| `Storage:BlobStorage:ConnectionString` | No | — | Connection string for shared key or Azurite. **If set, takes priority over `AccountName`/DAC.** Use `UseDevelopmentStorage=true` for Azurite. |
| `Storage:BlobStorage:ContainerName` | No | `todo-tasks` | Blob container name (created automatically if missing) |
| `Storage:BlobStorage:BlobName` | No | `tasks.json` | Name of the JSON blob inside the container |

**Auth priority:**
1. If `ConnectionString` is set → connection string / shared key auth (Azurite, legacy)
2. Otherwise → `DefaultAzureCredential` using `AccountName` (Managed Identity, `az login`, VS credentials, etc.)

**Example — production (DAC via Managed Identity):**

```json
{
  "Storage": {
    "Provider": "Blob",
    "BlobStorage": {
      "AccountName": "mystorageaccount"
    }
  }
}
```

**Example — local dev with Azurite:**

```json
{
  "Storage": {
    "Provider": "Blob",
    "BlobStorage": {
      "ConnectionString": "UseDevelopmentStorage=true"
    }
  }
}
```

## Running Locally

```bash
cd src/HappyChaos.Todo
dotnet run
```

The application will start at `http://localhost:5175` (or `https://localhost:7029`). The MCP endpoint will be available at `/mcp`.