using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public interface IDataPersistenceService
{
    Task<T> LoadAsync<T>(string key);
    Task SaveAsync<T>(string key, T data);
    Task AppendAuditLogAsync(string action, string userId, string details);
}

public class DataPersistenceService : IDataPersistenceService
{
    private readonly string _dataDirectory;
    private readonly ConcurrentDictionary<string, object> _cache = new();
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public DataPersistenceService(string dataDirectory = "data")
    {
        _dataDirectory = dataDirectory;
        Directory.CreateDirectory(_dataDirectory);
    }

    public async Task<T> LoadAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            return (T)cached;
        }

        var filePath = Path.Combine(_dataDirectory, $"{key}.json");
        if (!File.Exists(filePath))
        {
            return default(T);
        }

        await _fileLock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var data = JsonConvert.DeserializeObject<T>(json);
            _cache[key] = data;
            return data;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveAsync<T>(string key, T data)
    {
        _cache[key] = data;
        var filePath = Path.Combine(_dataDirectory, $"{key}.json");

        await _fileLock.WaitAsync();
        try
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task AppendAuditLogAsync(string action, string userId, string details)
    {
        var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}|{userId}|{action}|{details}";
        var logPath = Path.Combine(_dataDirectory, "audit.log");

        await _fileLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(logPath, logEntry + Environment.NewLine);
        }
        finally
        {
            _fileLock.Release();
        }
    }
}