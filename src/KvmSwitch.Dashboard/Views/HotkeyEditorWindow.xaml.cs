using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using KvmSwitch.Core.Models;
using CoreModifierKeys = KvmSwitch.Core.Models.ModifierKeys;

namespace KvmSwitch.Dashboard.Views;

public partial class HotkeyEditorWindow : Window, INotifyPropertyChanged
{
    private readonly HotkeyMapping? _existingMapping;
    public ObservableCollection<KeyOption> KeyOptions { get; } = new();
    public ObservableCollection<Endpoint> AvailableEndpoints { get; } = new();

    private bool _useCtrl;
    private bool _useAlt;
    private bool _useShift;
    private bool _useWin;
    private bool _isGlobal = true;
    private KeyOption? _selectedKeyOption;
    private Endpoint? _selectedEndpoint;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool UseCtrl { get => _useCtrl; set { _useCtrl = value; OnPropertyChanged(nameof(UseCtrl)); } }
    public bool UseAlt { get => _useAlt; set { _useAlt = value; OnPropertyChanged(nameof(UseAlt)); } }
    public bool UseShift { get => _useShift; set { _useShift = value; OnPropertyChanged(nameof(UseShift)); } }
    public bool UseWin { get => _useWin; set { _useWin = value; OnPropertyChanged(nameof(UseWin)); } }
    public bool IsGlobal { get => _isGlobal; set { _isGlobal = value; OnPropertyChanged(nameof(IsGlobal)); } }

    public KeyOption? SelectedKeyOption
    {
        get => _selectedKeyOption;
        set
        {
            _selectedKeyOption = value;
            OnPropertyChanged(nameof(SelectedKeyOption));
        }
    }

    public Endpoint? SelectedEndpoint
    {
        get => _selectedEndpoint;
        set
        {
            _selectedEndpoint = value;
            OnPropertyChanged(nameof(SelectedEndpoint));
        }
    }

    public HotkeyMapping? Result { get; private set; }

    public HotkeyEditorWindow(IEnumerable<Endpoint> endpoints, HotkeyMapping? existing = null)
    {
        InitializeComponent();
        _existingMapping = existing;
        foreach (var endpoint in endpoints)
        {
            AvailableEndpoints.Add(endpoint);
        }

        LoadKeyOptions();
        DataContext = this;
        PopulateExistingValues();
    }

    private void LoadKeyOptions()
    {
        var keys = new[]
        {
            Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12
        }.Concat(Enumerable.Range((int)Key.A, 26).Select(i => (Key)i))
         .Concat(Enumerable.Range((int)Key.D0, 10).Select(i => (Key)i));

        foreach (var key in keys)
        {
            KeyOptions.Add(new KeyOption
            {
                DisplayName = key.ToString(),
                Key = key
            });
        }
    }

    private void PopulateExistingValues()
    {
        if (_existingMapping == null)
        {
            return;
        }

        UseCtrl = _existingMapping.Modifiers.HasFlag(CoreModifierKeys.Ctrl);
        UseAlt = _existingMapping.Modifiers.HasFlag(CoreModifierKeys.Alt);
        UseShift = _existingMapping.Modifiers.HasFlag(CoreModifierKeys.Shift);
        UseWin = _existingMapping.Modifiers.HasFlag(CoreModifierKeys.Win);
        IsGlobal = _existingMapping.IsGlobal;

        var key = KeyInterop.KeyFromVirtualKey(_existingMapping.KeyCode);
        SelectedKeyOption = KeyOptions.FirstOrDefault(k => k.Key == key);
        SelectedEndpoint = AvailableEndpoints.FirstOrDefault(e => e.Id == _existingMapping.EndpointId);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedKeyOption == null || SelectedEndpoint == null)
        {
            MessageBox.Show("Select both a key and an endpoint.", "Hotkey Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var modifiers = CoreModifierKeys.None;
        if (UseCtrl) modifiers |= CoreModifierKeys.Ctrl;
        if (UseAlt) modifiers |= CoreModifierKeys.Alt;
        if (UseShift) modifiers |= CoreModifierKeys.Shift;
        if (UseWin) modifiers |= CoreModifierKeys.Win;

        if (modifiers == CoreModifierKeys.None)
        {
            MessageBox.Show("Please include at least one modifier (Ctrl, Alt, Shift, Win).", "Hotkey Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new HotkeyMapping
        {
            Id = _existingMapping?.Id ?? Guid.NewGuid().ToString(),
            Modifiers = modifiers,
            KeyCode = KeyInterop.VirtualKeyFromKey(SelectedKeyOption.Key),
            EndpointId = SelectedEndpoint.Id,
            IsGlobal = IsGlobal,
            CreatedAt = _existingMapping?.CreatedAt ?? DateTime.UtcNow
        };

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class KeyOption
    {
        public required string DisplayName { get; init; }
        public required Key Key { get; init; }
    }
}

