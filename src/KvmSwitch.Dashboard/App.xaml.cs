using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Unity;
using KvmSwitch.Core.Interfaces;
using KvmSwitch.Core.Services;
using KvmSwitch.Core.Models;
using KvmSwitch.Routing.Interfaces;
using KvmSwitch.Routing.Services;
using KvmSwitch.Capture.Interfaces;
using KvmSwitch.Capture.Services;
using KvmSwitch.Dashboard.ViewModels;
using KvmSwitch.Dashboard.Services;
using Application = System.Windows.Application;

namespace KvmSwitch.Dashboard;

public partial class App : PrismApplication
{
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private AppSettings? _currentSettings;
    protected override Window CreateShell()
    {
        try
        {
            // Create service provider
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            
            var logger = serviceProvider.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Initializing AuroraSwitch Dashboard...");
            
            // Initialize services with error handling (async initialization)
            var endpointRegistry = serviceProvider.GetRequiredService<IEndpointRegistry>();
            try
            {
                // Use ConfigureAwait(false) and timeout to avoid blocking
                var loadTask = endpointRegistry.LoadAsync();
                if (loadTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    logger.LogInformation("Endpoint registry loaded successfully");
                }
                else
                {
                    logger.LogWarning("Endpoint registry load timed out, continuing with empty registry");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load endpoint registry, continuing with empty registry");
            }
            
            // Ensure host endpoint exists
            try
            {
                var getHostTask = endpointRegistry.GetEndpointByIdAsync("host");
                Core.Models.Endpoint? hostEndpoint = null;
                if (getHostTask.Wait(TimeSpan.FromSeconds(2)))
                {
                    hostEndpoint = getHostTask.Result;
                }
                
                if (hostEndpoint == null)
                {
                    hostEndpoint = new Core.Models.Endpoint
                    {
                        Id = "host",
                        Name = "Host PC",
                        Type = Core.Models.EndpointType.Host,
                        ConnectionType = Core.Models.ConnectionType.Hybrid,
                        Status = Core.Models.EndpointStatus.Active,
                        LastSeen = DateTime.UtcNow
                    };
                    var addHostTask = endpointRegistry.AddOrUpdateEndpointAsync(hostEndpoint);
                    if (addHostTask.Wait(TimeSpan.FromSeconds(2)))
                    {
                        logger.LogInformation("Created default host endpoint");
                    }
                    else
                    {
                        logger.LogWarning("Timeout creating host endpoint");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create host endpoint");
            }
            
            // Initialize hotkey manager
            try
            {
                var hotkeyManager = serviceProvider.GetRequiredService<IHotkeyManager>();
                if (hotkeyManager is HotkeyManager hm)
                {
                    try
                    {
                        var loadHotkeysTask = hm.LoadHotkeysAsync();
                        if (loadHotkeysTask.Wait(TimeSpan.FromSeconds(3)))
                        {
                            logger.LogInformation("Hotkeys loaded successfully");
                        }
                        else
                        {
                            logger.LogWarning("Hotkey load timed out, setting up defaults");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to load hotkeys, setting up defaults");
                    }
                    
                    // Set up default hotkeys if none exist (async with timeout)
                    try
                    {
                        var defaultSetup = new Core.Services.DefaultHotkeySetup(
                            hotkeyManager, 
                            endpointRegistry,
                            serviceProvider.GetRequiredService<ILogger<Core.Services.DefaultHotkeySetup>>());
                        var setupTask = defaultSetup.SetupDefaultsAsync();
                        if (setupTask.Wait(TimeSpan.FromSeconds(3)))
                        {
                            logger.LogInformation("Default hotkeys configured");
                        }
                        else
                        {
                            logger.LogWarning("Default hotkey setup timed out");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to setup default hotkeys");
                    }
                }
                hotkeyManager.StartListening();
                logger.LogInformation("Hotkey manager started");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize hotkey manager");
            }
            
            // Initialize HID input interceptor (optional - don't block startup if it fails)
            try
            {
                var hidInterceptor = serviceProvider.GetRequiredService<KvmSwitch.Routing.Services.HidInputInterceptor>();
                logger.LogInformation("HID input interceptor initialized");
            }
            catch (Exception ex)
            {
                // Log warning but continue - HID interception is optional for basic functionality
                logger.LogWarning(ex, "Failed to initialize HID input interceptor - continuing without input interception");
            }
            
            var settingsService = serviceProvider.GetRequiredService<IAppSettingsService>();
            try
            {
                _currentSettings = settingsService.GetAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load app settings, using defaults");
                _currentSettings = new AppSettings();
            }

            // Create view model
            MainViewModel viewModel;
            try
            {
                viewModel = serviceProvider.GetRequiredService<MainViewModel>();
                logger.LogInformation("Dashboard initialized successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create MainViewModel");
                throw; // Re-throw as this is critical
            }
            
            // Create and show window
            var window = new MainWindow(
                viewModel,
                serviceProvider.GetRequiredService<IHotkeyManager>(),
                serviceProvider.GetRequiredService<IEndpointRegistry>(),
                settingsService,
                serviceProvider.GetRequiredService<IUpdateService>());
            logger.LogInformation("MainWindow created - application ready");
            
            // Ensure window is visible
            window.Show();
            
            // Initialize system tray icon
            InitializeSystemTray(window, _currentSettings);
            
            return window;
        }
        catch (Exception ex)
        {
            // Show error dialog with detailed information
            var errorMessage = $"Failed to initialize AuroraSwitch Dashboard:\n\n" +
                $"Error: {ex.Message}\n\n" +
                $"Type: {ex.GetType().Name}\n\n";
            
            if (ex.InnerException != null)
            {
                errorMessage += $"Inner Exception: {ex.InnerException.Message}\n\n";
            }
            
            errorMessage += "Stack Trace:\n" + ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace.Length)) + "\n\n" +
                "Please check the Event Viewer (Applications and Services Logs) for more details.";
            
            // Show error dialog (this should always work even if logging fails)
            try
            {
                System.Windows.MessageBox.Show(
                    errorMessage,
                    "Initialization Error - AuroraSwitch Dashboard",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // If MessageBox fails, try console output
                Console.Error.WriteLine($"Critical error: {ex}");
            }
            
            // Try to log error to Event Viewer
            try
            {
                var basicServices = new ServiceCollection();
                basicServices.AddLogging(builder => 
                {
                    builder.AddEventLog(settings => 
                    {
                        settings.SourceName = "AuroraSwitch";
                        settings.LogName = "Application";
                    });
                    builder.SetMinimumLevel(LogLevel.Error);
                });
                var basicProvider = basicServices.BuildServiceProvider();
                var basicLogger = basicProvider.GetRequiredService<ILogger<App>>();
                basicLogger.LogError(ex, "Critical error during application initialization: {ErrorMessage}", ex.Message);
            }
            catch
            {
                // If logging fails, write to console
                Console.Error.WriteLine($"Failed to log error: {ex}");
            }
            
            // Shutdown application
            try
            {
                Application.Current?.Shutdown(1);
            }
            catch
            {
                Environment.Exit(1);
            }
            
            return null;
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Logging - console and debug output
        services.AddLogging(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
            builder.AddDebug().SetMinimumLevel(LogLevel.Debug);
        });
        
        // Core services
        services.AddSingleton<HotkeyRegistry>();
        services.AddSingleton<IEndpointRegistry, EndpointRegistry>();
        services.AddSingleton<IAppSettingsService, AppSettingsService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IHotkeyManager>(sp => 
        {
            var logger = sp.GetRequiredService<ILogger<HotkeyManager>>();
            var registry = sp.GetRequiredService<HotkeyRegistry>();
            return new HotkeyManager(logger, registry);
        });
        services.AddSingleton<IDeviceDiscovery, DeviceDiscovery>();
        
        // Routing services
        services.AddSingleton<IPeripheralRouter, PeripheralRouter>();
        
        // HID Input Interceptor - should not throw during construction
        // The actual hook installation happens lazily when an endpoint becomes active
        services.AddSingleton<KvmSwitch.Routing.Services.HidInputInterceptor>(sp =>
        {
            var router = sp.GetRequiredService<IPeripheralRouter>();
            var logger = sp.GetRequiredService<ILogger<KvmSwitch.Routing.Services.HidInputInterceptor>>();
            // Constructor should be safe - it doesn't install hooks, just sets up event handlers
            return new KvmSwitch.Routing.Services.HidInputInterceptor(router, logger);
        });
        
        // Capture services (can be null if no capture devices)
        services.AddSingleton<IVideoCapture, VideoCaptureService>();
        services.AddSingleton<IAudioCapture, AudioCaptureService>();
        
        // ViewModels
        services.AddTransient<MainViewModel>(sp =>
        {
            var endpointRegistry = sp.GetRequiredService<IEndpointRegistry>();
            var peripheralRouter = sp.GetRequiredService<IPeripheralRouter>();
            var videoCapture = sp.GetRequiredService<IVideoCapture>();
            var deviceDiscovery = sp.GetRequiredService<IDeviceDiscovery>();
            var hotkeyManager = sp.GetRequiredService<IHotkeyManager>();
            return new MainViewModel(endpointRegistry, peripheralRouter, videoCapture, deviceDiscovery, hotkeyManager);
        });
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Types registered via ConfigureServices
    }

    private void InitializeSystemTray(Window mainWindow, AppSettings? settings)
    {
        if (settings != null && !settings.EnableSystemTray)
        {
            return;
        }

        try
        {
            // Create system tray icon
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = GetApplicationIcon(),
                Text = "AuroraSwitch KVM Dashboard",
                Visible = true
            };

            // Create context menu
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            
            var showMenuItem = new System.Windows.Forms.ToolStripMenuItem("Show Dashboard");
            showMenuItem.Click += (s, e) =>
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            };
            contextMenu.Items.Add(showMenuItem);
            
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            
            var exitMenuItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
            exitMenuItem.Click += (s, e) =>
            {
                _notifyIcon?.Dispose();
                Application.Current.Shutdown();
            };
            contextMenu.Items.Add(exitMenuItem);
            
            _notifyIcon.ContextMenuStrip = contextMenu;
            
            // Double-click to show window
            _notifyIcon.DoubleClick += (s, e) =>
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            };
            
            // Handle application exit - use Dispatcher to ensure it's on the right thread
            if (Application.Current != null)
            {
                Application.Current.Exit += (s, e) =>
                {
                    _notifyIcon?.Dispose();
                };
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail startup
            System.Diagnostics.Debug.WriteLine($"Failed to initialize system tray: {ex.Message}");
        }
    }

    private System.Drawing.Icon GetApplicationIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "icon.ico");
            if (File.Exists(iconPath))
            {
                return new System.Drawing.Icon(iconPath);
            }
        }
        catch
        {
            // Fall through to default icon
        }
        
        // Return default system icon if custom icon not found
        return System.Drawing.SystemIcons.Application;
    }
}

