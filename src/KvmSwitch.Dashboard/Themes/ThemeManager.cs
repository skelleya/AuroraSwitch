using System;
using System.Windows;

namespace KvmSwitch.Dashboard.Themes;

public static class ThemeManager
{
    private static ResourceDictionary? _currentDictionary;

    public static void ApplyTheme(string? rawTheme)
    {
        var themeName = Normalize(rawTheme);
        try
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri($"Themes/{themeName}Theme.xaml", UriKind.Relative)
            };

            var app = Application.Current;
            if (app == null)
            {
                return;
            }

            if (_currentDictionary != null)
            {
                app.Resources.MergedDictionaries.Remove(_currentDictionary);
            }

            app.Resources.MergedDictionaries.Add(dictionary);
            _currentDictionary = dictionary;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply theme '{themeName}': {ex.Message}");
        }
    }

    private static string Normalize(string? rawTheme)
    {
        return rawTheme?.Trim().ToLowerInvariant() switch
        {
            "light" => "Light",
            "dark" => "Dark",
            _ => "Dark"
        };
    }
}

