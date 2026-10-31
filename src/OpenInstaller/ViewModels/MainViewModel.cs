// OpenInstaller — original code, MIT licensed.
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OpenInstaller.Models;
using OpenInstaller.Services;

namespace OpenInstaller.ViewModels;

/// <summary>
/// View model for the single main window. It owns no UI types: the window
/// binds to its properties and calls its methods from button handlers.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private const int MaxLogLines = 200;

    private readonly IWingetService _winget;
    private readonly List<string> _logLines = new();
    private CancellationTokenSource? _installCts;
    private string? _lastActionId;
    private bool _lastActionWasUpgrade;

    private string _query = string.Empty;
    private PackageInfo? _selectedPackage;
    private string _statusText = "Enter a package name or id, then press Search.";
    private string _logText = string.Empty;
    private string _installedStatus = "Not checked yet.";
    private string _availableVersion = string.Empty;
    private string _updateStatus = string.Empty;
    private string _wingetVersion = "Checking…";
    private double _progressValue;
    private bool _progressIndeterminate;
    private bool _isBusy;
    private bool _isInstalling;

    public MainViewModel(IWingetService winget)
    {
        _winget = winget;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PackageInfo> Results { get; } = new();

    public string Query
    {
        get => _query;
        set => SetField(ref _query, value);
    }

    public PackageInfo? SelectedPackage
    {
        get => _selectedPackage;
        set
        {
            if (SetField(ref _selectedPackage, value) && value is not null)
            {
                // Fire and forget: selection details are best-effort and
                // must never block the UI thread.
                _ = RefreshSelectedPackageAsync(value);
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public string LogText
    {
        get => _logText;
        private set => SetField(ref _logText, value);
    }

    public string InstalledStatus
    {
        get => _installedStatus;
        private set => SetField(ref _installedStatus, value);
    }

    public string AvailableVersion
    {
        get => _availableVersion;
        private set => SetField(ref _availableVersion, value);
    }

    public string UpdateStatus
    {
        get => _updateStatus;
        private set => SetField(ref _updateStatus, value);
    }

    public string WingetVersion
    {
        get => _wingetVersion;
        private set => SetField(ref _wingetVersion, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetField(ref _progressValue, value);
    }

    public bool ProgressIndeterminate
    {
        get => _progressIndeterminate;
        private set => SetField(ref _progressIndeterminate, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public bool IsInstalling
    {
        get => _isInstalling;
        private set => SetField(ref _isInstalling, value);
    }

    /// <summary>Called once when the window opens: checks winget itself.</summary>
    public async Task InitializeAsync()
    {
        try
        {
            var version = await _winget.GetWingetVersionAsync().ConfigureAwait(false);
            WingetVersion = version is null
                ? "winget: not detected"
                : $"winget {version}";
            if (version is null)
            {
                StatusText = "winget was not found. Install 'App Installer' first, then restart OpenInstaller.";
            }
        }
        catch (WingetNotAvailableException ex)
        {
            WingetVersion = "winget: not installed";
            StatusText = ex.Message;
        }
        catch (Exception ex)
        {
            WingetVersion = "winget: check failed";
            StatusText = $"Could not check winget: {ex.Message}";
        }
    }

    public async Task SearchAsync()
    {
        if (IsBusy)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(Query))
        {
            StatusText = "Type a package name or id first.";
            return;
        }

        IsBusy = true;
        StatusText = $"Searching for “{Query.Trim()}”…";
        try
        {
            var results = await _winget.SearchAsync(Query.Trim()).ConfigureAwait(false);
            Results.Clear();
            foreach (var package in results)
            {
                Results.Add(package);
            }
            StatusText = results.Count == 0
                ? "Package not found. Check the spelling, or try a broader name."
                : $"Found {results.Count} package(s). Select one to see its details.";
            if (results.Count > 0)
            {
                SelectedPackage = results[0];
            }
        }
        catch (WingetNotAvailableException ex)
        {
            StatusText = ex.Message;
        }
        catch (OperationCanceledException)
        {
            StatusText = "Search cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task InstallSelectedAsync()
    {
        if (SelectedPackage is null)
        {
            StatusText = "Select a package first.";
            return;
        }
        await RunInstallAsync(SelectedPackage.Id, isUpgrade: false).ConfigureAwait(false);
    }

    public async Task UpdateSelectedAsync()
    {
        if (SelectedPackage is null)
        {
            StatusText = "Select a package first.";
            return;
        }
        await RunInstallAsync(SelectedPackage.Id, isUpgrade: true).ConfigureAwait(false);
    }

    public async Task RetryAsync()
    {
        if (_lastActionId is null)
        {
            StatusText = "Nothing to retry yet. Search and install a package first.";
            return;
        }
        await RunInstallAsync(_lastActionId, _lastActionWasUpgrade).ConfigureAwait(false);
    }

    public void CancelInstall()
    {
        _installCts?.Cancel();
    }

    public async Task LaunchSelectedAsync()
    {
        if (SelectedPackage is null)
        {
            StatusText = "Select a package first.";
            return;
        }
        StatusText = $"Trying to launch {SelectedPackage.Name}…";
        var launched = await _winget.TryLaunchAsync(SelectedPackage.Name).ConfigureAwait(false);
        StatusText = launched
            ? $"Launched {SelectedPackage.Name}."
            : "Could not launch it automatically. Open it from the Start menu instead.";
    }

    private async Task RunInstallAsync(string id, bool isUpgrade)
    {
        if (IsBusy)
        {
            return;
        }

        _lastActionId = id;
        _lastActionWasUpgrade = isUpgrade;
        _installCts?.Dispose();
        _installCts = new CancellationTokenSource();

        IsBusy = true;
        IsInstalling = true;
        ProgressIndeterminate = true;
        ProgressValue = 0;
        var verb = isUpgrade ? "Updating" : "Installing";
        StatusText = $"{verb} {id}…";
        AppendLog($"{verb} {id}");

        try
        {
            var progress = new Progress<InstallProgress>(report =>
            {
                if (report.Percent.HasValue)
                {
                    ProgressIndeterminate = false;
                    ProgressValue = report.Percent.Value;
                }
                if (!string.IsNullOrWhiteSpace(report.Status))
                {
                    StatusText = report.Status;
                    AppendLog(report.Status);
                }
            });

            var outcome = isUpgrade
                ? await _winget.UpgradeAsync(id, progress, _installCts.Token).ConfigureAwait(false)
                : await _winget.InstallAsync(id, progress, _installCts.Token).ConfigureAwait(false);

            if (outcome.Cancelled)
            {
                StatusText = "Installation cancelled.";
            }
            else if (outcome.AlreadyInstalled)
            {
                StatusText = "Already installed. Use Update if a newer version is available.";
            }
            else if (outcome.Success)
            {
                ProgressValue = 100;
                StatusText = isUpgrade ? "Update completed successfully." : "Install completed successfully. You can now Launch the app.";
            }
            else
            {
                StatusText = outcome.Message;
            }
            AppendLog(StatusText);
        }
        catch (WingetNotAvailableException ex)
        {
            StatusText = ex.Message;
        }
        catch (OperationCanceledException)
        {
            StatusText = "Installation cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Install failed: {ex.Message}";
        }
        finally
        {
            IsInstalling = false;
            ProgressIndeterminate = false;
            IsBusy = false;
            if (SelectedPackage is not null && string.Equals(SelectedPackage.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                _ = RefreshSelectedPackageAsync(SelectedPackage);
            }
        }
    }

    /// <summary>Re-checks installed status, latest version, and details for the selection.</summary>
    public async Task CheckSelectedAsync()
    {
        if (SelectedPackage is not null && !IsBusy)
        {
            await RefreshSelectedPackageAsync(SelectedPackage).ConfigureAwait(false);
        }
    }

    private async Task RefreshSelectedPackageAsync(PackageInfo package)
    {
        InstalledStatus = "Checking whether it is installed…";
        UpdateStatus = string.Empty;
        AvailableVersion = package.Version;
        try
        {
            var details = await _winget.GetDetailsAsync(package.Id).ConfigureAwait(false);
            if (details is not null)
            {
                if (details.TryGetValue("Publisher", out var publisher))
                {
                    package.Publisher = publisher;
                }
                if (details.TryGetValue("Description", out var description))
                {
                    package.Description = description;
                }
            }
            var installed = await _winget.GetInstalledAsync(package.Id).ConfigureAwait(false);
            if (installed is null)
            {
                package.InstalledVersion = string.Empty;
                InstalledStatus = "Not installed.";
            }
            else
            {
                package.InstalledVersion = installed.InstalledVersion;
                InstalledStatus = $"Installed: version {installed.InstalledVersion}.";
            }

            var latest = await _winget.SearchExactAsync(package.Id).ConfigureAwait(false);
            if (latest is not null && !string.IsNullOrWhiteSpace(latest.Version))
            {
                AvailableVersion = latest.Version;
                if (package.IsInstalled
                    && WingetOutputParser.CompareVersions(latest.Version, package.InstalledVersion) > 0)
                {
                    UpdateStatus = $"Update available: {package.InstalledVersion} → {latest.Version}.";
                }
                else if (package.IsInstalled)
                {
                    UpdateStatus = "Up to date.";
                }
            }
        }
        catch (WingetNotAvailableException ex)
        {
            InstalledStatus = ex.Message;
        }
        catch (Exception)
        {
            // Installed/update checks are informational; a failure here
            // must not hide the search results or block an install.
            InstalledStatus = "Could not check installed status.";
        }
    }

    private void AppendLog(string line)
    {
        _logLines.Add(line);
        while (_logLines.Count > MaxLogLines)
        {
            _logLines.RemoveAt(0);
        }
        LogText = string.Join(Environment.NewLine, _logLines);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
