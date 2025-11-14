using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;
using Microsoft.Extensions.Logging;

namespace KvmSwitch.Core.Services;

/// <summary>
/// Manages persistence of hotkey mappings to SQLite database.
/// </summary>
public class HotkeyRegistry
{
    private readonly string _storagePath;
    private readonly ILogger<HotkeyRegistry>? _logger;

    public HotkeyRegistry(ILogger<HotkeyRegistry>? logger = null)
    {
        _logger = logger;
        _storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KvmSwitch", "endpoints.db");
        var directory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public async Task SaveHotkeysAsync(IEnumerable<HotkeyMapping> hotkeys)
    {
        try
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_storagePath}");
            await connection.OpenAsync();

            // Create table if it doesn't exist
            using var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS hotkey_mappings (
                    id TEXT PRIMARY KEY,
                    modifiers INTEGER NOT NULL,
                    key_code INTEGER NOT NULL,
                    endpoint_id TEXT NOT NULL,
                    is_global INTEGER NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    UNIQUE(modifiers, key_code)
                )";
            await createTableCmd.ExecuteNonQueryAsync();

            // Clear existing hotkeys
            using var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM hotkey_mappings";
            await deleteCmd.ExecuteNonQueryAsync();

            // Insert hotkeys
            foreach (var hotkey in hotkeys)
            {
                using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO hotkey_mappings 
                    (id, modifiers, key_code, endpoint_id, is_global, created_at, updated_at)
                    VALUES (@id, @modifiers, @key_code, @endpoint_id, @is_global, @created_at, @updated_at)";
                
                insertCmd.Parameters.AddWithValue("@id", hotkey.Id);
                insertCmd.Parameters.AddWithValue("@modifiers", (int)hotkey.Modifiers);
                insertCmd.Parameters.AddWithValue("@key_code", hotkey.KeyCode);
                insertCmd.Parameters.AddWithValue("@endpoint_id", hotkey.EndpointId);
                insertCmd.Parameters.AddWithValue("@is_global", hotkey.IsGlobal ? 1 : 0);
                
                var now = DateTime.UtcNow.ToString("O");
                insertCmd.Parameters.AddWithValue("@created_at", hotkey.CreatedAt.ToString("O"));
                insertCmd.Parameters.AddWithValue("@updated_at", now);
                
                await insertCmd.ExecuteNonQueryAsync();
            }

            _logger?.LogInformation("Saved {Count} hotkeys to database", hotkeys.Count());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving hotkeys to database");
        }
    }

    public async Task<IEnumerable<HotkeyMapping>> LoadHotkeysAsync()
    {
        var hotkeys = new List<HotkeyMapping>();

        try
        {
            if (!File.Exists(_storagePath))
            {
                return hotkeys;
            }

            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_storagePath}");
            await connection.OpenAsync();

            using var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM hotkey_mappings";

            using var reader = await selectCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var idOrdinal = reader.GetOrdinal("id");
                var modifiersOrdinal = reader.GetOrdinal("modifiers");
                var keyOrdinal = reader.GetOrdinal("key_code");
                var endpointOrdinal = reader.GetOrdinal("endpoint_id");
                var globalOrdinal = reader.GetOrdinal("is_global");
                var createdOrdinal = reader.GetOrdinal("created_at");

                hotkeys.Add(new HotkeyMapping
                {
                    Id = reader.GetString(idOrdinal),
                    Modifiers = (ModifierKeys)reader.GetInt32(modifiersOrdinal),
                    KeyCode = reader.GetInt32(keyOrdinal),
                    EndpointId = reader.GetString(endpointOrdinal),
                    IsGlobal = reader.GetInt32(globalOrdinal) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(createdOrdinal))
                });
            }

            _logger?.LogInformation("Loaded {Count} hotkeys from database", hotkeys.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading hotkeys from database");
        }

        return hotkeys;
    }
}

