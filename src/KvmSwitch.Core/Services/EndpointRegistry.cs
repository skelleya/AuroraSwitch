using System.Collections.Concurrent;
using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Core.Services;

public class EndpointRegistry : IEndpointRegistry
{
    private readonly ConcurrentDictionary<string, Endpoint> _endpoints = new();
    private readonly ILogger<EndpointRegistry>? _logger;
    private readonly string _storagePath;

    public event EventHandler<Endpoint>? EndpointAdded;
    public event EventHandler<Endpoint>? EndpointRemoved;
    public event EventHandler<Endpoint>? EndpointUpdated;

    public EndpointRegistry(ILogger<EndpointRegistry>? logger = null)
    {
        _logger = logger;
        _storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KvmSwitch", "endpoints.db");
        var directory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public Task<IEnumerable<Endpoint>> GetAllEndpointsAsync()
    {
        return Task.FromResult(_endpoints.Values.AsEnumerable());
    }

    public Task<Endpoint?> GetEndpointByIdAsync(string id)
    {
        _endpoints.TryGetValue(id, out var endpoint);
        return Task.FromResult<Endpoint?>(endpoint);
    }

    public Task<Endpoint?> GetEndpointByHotkeyAsync(ModifierKeys modifiers, int keyCode)
    {
        // TODO: Implement hotkey lookup from stored mappings
        return Task.FromResult<Endpoint?>(null);
    }

    public Task AddOrUpdateEndpointAsync(Endpoint endpoint)
    {
        var isNew = _endpoints.TryAdd(endpoint.Id, endpoint);
        if (!isNew)
        {
            _endpoints[endpoint.Id] = endpoint;
            EndpointUpdated?.Invoke(this, endpoint);
            _logger?.LogInformation("Updated endpoint: {EndpointId} - {EndpointName}", endpoint.Id, endpoint.Name);
        }
        else
        {
            EndpointAdded?.Invoke(this, endpoint);
            _logger?.LogInformation("Added endpoint: {EndpointId} - {EndpointName}", endpoint.Id, endpoint.Name);
        }
        return Task.CompletedTask;
    }

    public Task RemoveEndpointAsync(string id)
    {
        if (_endpoints.TryRemove(id, out var endpoint))
        {
            EndpointRemoved?.Invoke(this, endpoint);
            _logger?.LogInformation("Removed endpoint: {EndpointId}", id);
        }
        return Task.CompletedTask;
    }

    public async Task SaveAsync()
    {
        try
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_storagePath}");
            await connection.OpenAsync();

            // Create tables if they don't exist
            using var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS endpoints (
                    id TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    type INTEGER NOT NULL,
                    connection_type INTEGER NOT NULL,
                    device_id TEXT,
                    vendor_id TEXT,
                    product_id TEXT,
                    status INTEGER NOT NULL,
                    last_seen TEXT NOT NULL,
                    metadata TEXT,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )";
            await createTableCmd.ExecuteNonQueryAsync();

            // Save all endpoints
            foreach (var endpoint in _endpoints.Values)
            {
                using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT OR REPLACE INTO endpoints 
                    (id, name, type, connection_type, device_id, vendor_id, product_id, status, last_seen, metadata, created_at, updated_at)
                    VALUES (@id, @name, @type, @connection_type, @device_id, @vendor_id, @product_id, @status, @last_seen, @metadata, @created_at, @updated_at)";
                
                insertCmd.Parameters.AddWithValue("@id", endpoint.Id);
                insertCmd.Parameters.AddWithValue("@name", endpoint.Name);
                insertCmd.Parameters.AddWithValue("@type", (int)endpoint.Type);
                insertCmd.Parameters.AddWithValue("@connection_type", (int)endpoint.ConnectionType);
                insertCmd.Parameters.AddWithValue("@device_id", endpoint.DeviceId ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@vendor_id", endpoint.VendorId ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@product_id", endpoint.ProductId ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@status", (int)endpoint.Status);
                insertCmd.Parameters.AddWithValue("@last_seen", endpoint.LastSeen.ToString("O"));
                
                var metadataJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    capture_device_ids = endpoint.CaptureDeviceIds,
                    metadata = endpoint.Metadata
                });
                insertCmd.Parameters.AddWithValue("@metadata", metadataJson);
                
                var now = DateTime.UtcNow.ToString("O");
                insertCmd.Parameters.AddWithValue("@created_at", now);
                insertCmd.Parameters.AddWithValue("@updated_at", now);
                
                await insertCmd.ExecuteNonQueryAsync();
            }

            _logger?.LogInformation("Saved {Count} endpoints to database", _endpoints.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving endpoints to database");
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(_storagePath))
            {
                _logger?.LogInformation("Database file does not exist, skipping load");
                return;
            }

            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_storagePath}");
            await connection.OpenAsync();

            using var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM endpoints";

            using var reader = await selectCmd.ExecuteReaderAsync();
            while (reader.Read())
            {
                var idOrdinal = reader.GetOrdinal("id");
                var nameOrdinal = reader.GetOrdinal("name");
                var typeOrdinal = reader.GetOrdinal("type");
                var connectionTypeOrdinal = reader.GetOrdinal("connection_type");
                var deviceIdOrdinal = reader.GetOrdinal("device_id");
                var vendorIdOrdinal = reader.GetOrdinal("vendor_id");
                var productIdOrdinal = reader.GetOrdinal("product_id");
                var statusOrdinal = reader.GetOrdinal("status");
                var lastSeenOrdinal = reader.GetOrdinal("last_seen");
                var metadataOrdinal = reader.GetOrdinal("metadata");

                var endpoint = new Endpoint
                {
                    Id = reader.GetString(idOrdinal),
                    Name = reader.GetString(nameOrdinal),
                    Type = (EndpointType)reader.GetInt32(typeOrdinal),
                    ConnectionType = (ConnectionType)reader.GetInt32(connectionTypeOrdinal),
                    DeviceId = reader.IsDBNull(deviceIdOrdinal) ? null : reader.GetString(deviceIdOrdinal),
                    VendorId = reader.IsDBNull(vendorIdOrdinal) ? null : reader.GetString(vendorIdOrdinal),
                    ProductId = reader.IsDBNull(productIdOrdinal) ? null : reader.GetString(productIdOrdinal),
                    Status = (EndpointStatus)reader.GetInt32(statusOrdinal),
                    LastSeen = DateTime.Parse(reader.GetString(lastSeenOrdinal))
                };

                if (!reader.IsDBNull(metadataOrdinal))
                {
                    var metadataJson = reader.GetString(metadataOrdinal);
                    var metadataObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
                    if (metadataObj != null)
                    {
                        if (metadataObj.TryGetValue("capture_device_ids", out var captureIds) && captureIds != null)
                        {
                            var ids = System.Text.Json.JsonSerializer.Deserialize<List<string>>(captureIds.ToString() ?? "[]");
                            if (ids != null)
                            {
                                endpoint.CaptureDeviceIds = ids;
                            }
                        }
                        if (metadataObj.TryGetValue("metadata", out var meta) && meta != null)
                        {
                            var metaDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(meta.ToString() ?? "{}");
                            if (metaDict != null)
                            {
                                endpoint.Metadata = metaDict;
                            }
                        }
                    }
                }

                _endpoints.TryAdd(endpoint.Id, endpoint);
            }

            _logger?.LogInformation("Loaded {Count} endpoints from database", _endpoints.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading endpoints from database");
        }
    }
}

