// OpenInstaller — original code, MIT licensed.
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenInstaller.Models;

/// <summary>
/// One package as reported by the public winget CLI. This is a plain data
/// holder; it contains nothing copied from any other installer program.
/// It notifies the UI when details filled in after selection change.
/// </summary>
public sealed class PackageInfo : INotifyPropertyChanged
{
    private string _publisher = string.Empty;
    private string _description = string.Empty;
    private string _installedVersion = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;

    /// <summary>Filled in later, on a best-effort basis, from `winget show`.</summary>
    public string Publisher
    {
        get => _publisher;
        set => SetField(ref _publisher, value);
    }

    /// <summary>Filled in later, on a best-effort basis, from `winget show`.</summary>
    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    /// <summary>Version currently installed on this machine, if any.</summary>
    public string InstalledVersion
    {
        get => _installedVersion;
        set
        {
            if (SetField(ref _installedVersion, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInstalled)));
            }
        }
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

    public bool IsInstalled => InstalledVersion.Length > 0;

    public string DisplayText
    {
        get
        {
            var text = string.IsNullOrWhiteSpace(Name) ? Id : Name;
            if (!string.IsNullOrWhiteSpace(Id) && !string.Equals(text, Id, StringComparison.Ordinal))
            {
                text += $" ({Id})";
            }
            if (!string.IsNullOrWhiteSpace(Version))
            {
                text += $" — {Version}";
            }
            return text;
        }
    }

    public override string ToString() => DisplayText;
}
