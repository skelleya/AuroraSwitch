using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Models;
using CoreModifierKeys = KvmSwitch.Core.Models.ModifierKeys;

namespace KvmSwitch.Dashboard.Views;

public partial class ManageHotkeysWindow : Window, INotifyPropertyChanged
{
    private readonly IHotkeyManager _hotkeyManager;
    private readonly IEndpointRegistry _endpointRegistry;
    private HotkeyDisplayItem? _selectedHotkey;
    private string _statusMessage = "Ready";

    public ObservableCollection<HotkeyDisplayItem> HotkeyItems { get; } = new();
    public ObservableCollection<Endpoint> AvailableEndpoints { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public HotkeyDisplayItem? SelectedHotkey
    {
        get => _selectedHotkey;
        set
        {
            _selectedHotkey = value;
            OnPropertyChanged(nameof(SelectedHotkey));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    public ManageHotkeysWindow(IHotkeyManager hotkeyManager, IEndpointRegistry endpointRegistry)
    {
        InitializeComponent();
        _hotkeyManager = hotkeyManager;
        _endpointRegistry = endpointRegistry;
        DataContext = this;
        Loaded += ManageHotkeysWindow_Loaded;
    }

    private async void ManageHotkeysWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadEndpointsAsync();
        await LoadHotkeysAsync();
    }

    private async Task LoadEndpointsAsync()
    {
        var endpoints = (await _endpointRegistry.GetAllEndpointsAsync())
            .OrderBy(e => e.Name)
            .ToList();

        AvailableEndpoints.Clear();
        foreach (var endpoint in endpoints)
        {
            AvailableEndpoints.Add(endpoint);
        }
    }

    private async Task LoadHotkeysAsync()
    {
        var endpointLookup = (await _endpointRegistry.GetAllEndpointsAsync()).ToDictionary(e => e.Id, e => e.Name);
        HotkeyItems.Clear();

        var hotkeys = await _hotkeyManager.GetRegisteredHotkeysAsync();
        foreach (var hotkey in hotkeys.OrderBy(h => h.CreatedAt))
        {
            var display = new HotkeyDisplayItem
            {
                Mapping = hotkey,
                DisplayText = FormatHotkey(hotkey),
                EndpointName = endpointLookup.TryGetValue(hotkey.EndpointId, out var name) ? name : hotkey.EndpointId,
                Scope = hotkey.IsGlobal ? "Global" : "UI"
            };
            HotkeyItems.Add(display);
        }

        StatusMessage = $"Loaded {HotkeyItems.Count} hotkey(s)";
    }

    private async void AddHotkey_Click(object sender, RoutedEventArgs e)
    {
        var mapping = ShowEditor();
        if (mapping == null)
        {
            return;
        }

        await SaveHotkeyAsync(mapping, null);
    }

    private async void EditHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedHotkey == null)
        {
            MessageBox.Show("Select a hotkey first.", "Manage Hotkeys", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var mapping = ShowEditor(SelectedHotkey.Mapping);
        if (mapping == null)
        {
            return;
        }

        await SaveHotkeyAsync(mapping, SelectedHotkey.Mapping);
    }

    private async Task SaveHotkeyAsync(HotkeyMapping mapping, HotkeyMapping? existing)
    {
        if (!await _hotkeyManager.IsHotkeyAvailableAsync(mapping.Modifiers, mapping.KeyCode) && (existing == null ||
            existing.Modifiers != mapping.Modifiers || existing.KeyCode != mapping.KeyCode))
        {
            MessageBox.Show("Another hotkey already uses that combination.", "Hotkey Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (existing != null && existing.Id != mapping.Id)
        {
            mapping.Id = existing.Id;
        }

        if (existing != null)
        {
            await _hotkeyManager.UnregisterHotkeyAsync(existing.Id);
        }

        var success = await _hotkeyManager.RegisterHotkeyAsync(mapping);
        if (!success)
        {
            MessageBox.Show("Unable to register hotkey.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await LoadHotkeysAsync();
        StatusMessage = existing == null ? "Hotkey added" : "Hotkey updated";
    }

    private async void RemoveHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedHotkey == null)
        {
            MessageBox.Show("Select a hotkey to remove.", "Manage Hotkeys", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show($"Remove hotkey {SelectedHotkey.DisplayText}?", "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        await _hotkeyManager.UnregisterHotkeyAsync(SelectedHotkey.Mapping.Id);
        await LoadHotkeysAsync();
        StatusMessage = "Hotkey removed";
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private HotkeyMapping? ShowEditor(HotkeyMapping? existing = null)
    {
        var editor = new HotkeyEditorWindow(AvailableEndpoints, existing) { Owner = this };
        var result = editor.ShowDialog();
        return result == true ? editor.Result : null;
    }

    private static string FormatHotkey(HotkeyMapping mapping)
    {
        var parts = new List<string>();
        if (mapping.Modifiers.HasFlag(CoreModifierKeys.Ctrl)) parts.Add("Ctrl");
        if (mapping.Modifiers.HasFlag(CoreModifierKeys.Alt)) parts.Add("Alt");
        if (mapping.Modifiers.HasFlag(CoreModifierKeys.Shift)) parts.Add("Shift");
        if (mapping.Modifiers.HasFlag(CoreModifierKeys.Win)) parts.Add("Win");

        var key = KeyInterop.KeyFromVirtualKey(mapping.KeyCode);
        parts.Add(key.ToString());

        return string.Join(" + ", parts);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class HotkeyDisplayItem
    {
        public required HotkeyMapping Mapping { get; init; }
        public required string DisplayText { get; init; }
        public required string EndpointName { get; init; }
        public required string Scope { get; init; }
    }
}

