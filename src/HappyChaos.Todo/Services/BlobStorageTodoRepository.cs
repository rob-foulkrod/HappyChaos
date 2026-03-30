using System.Text.Json;
using Azure.Identity;
using Azure.Storage.Blobs;
using HappyChaos.Todo.Models;

namespace HappyChaos.Todo.Services;

public class BlobStorageTodoRepository : ITodoRepository
{
    private readonly BlobContainerClient _containerClient;
    private readonly string _blobName;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<BlobStorageTodoRepository> _logger;
    private volatile bool _containerEnsured;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public BlobStorageTodoRepository(IConfiguration configuration, ILogger<BlobStorageTodoRepository> logger)
    {
        _logger = logger;

        var connectionString = configuration["Storage:BlobStorage:ConnectionString"];
        var containerName = configuration["Storage:BlobStorage:ContainerName"]
            ?? "todo-tasks";
        _blobName = configuration["Storage:BlobStorage:BlobName"]
            ?? "tasks.json";

        var options = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2024_11_04);

        if (!string.IsNullOrEmpty(connectionString))
        {
            _logger.LogInformation(
                "Blob storage configured with connection string (container: {Container})",
                containerName);
            _containerClient = new BlobContainerClient(connectionString, containerName, options);
        }
        else
        {
            var accountName = configuration["Storage:BlobStorage:AccountName"]
                ?? throw new InvalidOperationException(
                    "Storage:BlobStorage:AccountName is required when no ConnectionString is configured. " +
                    "Set Storage:BlobStorage:ConnectionString for Azurite/shared key, " +
                    "or Storage:BlobStorage:AccountName for DefaultAzureCredential.");
            _logger.LogInformation(
                "Blob storage configured with DefaultAzureCredential (account: {Account}, container: {Container})",
                accountName, containerName);
            var serviceUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");
            _containerClient = new BlobContainerClient(serviceUri, new DefaultAzureCredential(), options);
        }
    }

    private async Task EnsureContainerExistsAsync()
    {
        if (_containerEnsured) return;

        try
        {
            await _containerClient.CreateIfNotExistsAsync();
            _containerEnsured = true;
            _logger.LogInformation("Blob container verified/created successfully");
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex,
                "Failed to create or access blob container. Status: {Status}, ErrorCode: {ErrorCode}. " +
                "Verify the storage account exists, credentials are valid, and the identity has " +
                "'Storage Blob Data Contributor' role assigned.",
                ex.Status, ex.ErrorCode);
            throw;
        }
        catch (AuthenticationFailedException ex)
        {
            _logger.LogError(ex,
                "Authentication failed for blob storage. If using DefaultAzureCredential, ensure you are " +
                "logged in via 'az login', or that the app's Managed Identity has access to the storage account.");
            throw;
        }
    }

    private async Task<List<TodoTask>> ReadTasksAsync()
    {
        await EnsureContainerExistsAsync();
        var blobClient = _containerClient.GetBlobClient(_blobName);

        if (!await blobClient.ExistsAsync())
        {
            return new List<TodoTask>();
        }

        var response = await blobClient.DownloadContentAsync();
        var json = response.Value.Content.ToString();
        return JsonSerializer.Deserialize<List<TodoTask>>(json, _jsonOptions) ?? new List<TodoTask>();
    }

    private async Task WriteTasksAsync(List<TodoTask> tasks)
    {
        await EnsureContainerExistsAsync();
        var blobClient = _containerClient.GetBlobClient(_blobName);
        var json = JsonSerializer.Serialize(tasks, _jsonOptions);
        await blobClient.UploadAsync(new BinaryData(json), overwrite: true);
    }

    public async Task<IEnumerable<TodoTask>> GetAllAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await ReadTasksAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<TodoTask>> GetByStatusAsync(Models.TaskStatus status)
    {
        await _semaphore.WaitAsync();
        try
        {
            var tasks = await ReadTasksAsync();
            return tasks.Where(t => t.Status == status).ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<TodoTask?> GetByIdAsync(int id)
    {
        await _semaphore.WaitAsync();
        try
        {
            var tasks = await ReadTasksAsync();
            return tasks.FirstOrDefault(t => t.Id == id);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<TodoTask> AddAsync(TodoTask task)
    {
        await _semaphore.WaitAsync();
        try
        {
            var tasks = await ReadTasksAsync();
            task.Id = tasks.Count > 0 ? tasks.Max(t => t.Id) + 1 : 1;
            task.CreatedAt = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;
            tasks.Add(task);
            await WriteTasksAsync(tasks);
            return task;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> UpdateAsync(TodoTask task)
    {
        await _semaphore.WaitAsync();
        try
        {
            var tasks = await ReadTasksAsync();
            var existing = tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing == null) return false;

            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.Priority = task.Priority;
            existing.Status = task.Status;
            existing.DueDate = task.DueDate;
            existing.AssignedTo = task.AssignedTo;
            existing.Category = task.Category;
            existing.UpdatedAt = DateTime.UtcNow;

            await WriteTasksAsync(tasks);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await _semaphore.WaitAsync();
        try
        {
            var tasks = await ReadTasksAsync();
            var task = tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return false;

            tasks.Remove(task);
            await WriteTasksAsync(tasks);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ReplaceAllAsync(IEnumerable<TodoTask> tasks)
    {
        await _semaphore.WaitAsync();
        try
        {
            await WriteTasksAsync(tasks.ToList());
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
