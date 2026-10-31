// OpenInstaller — original code, MIT licensed.
//
// Thin wrapper around the PUBLIC winget command-line interface. It never
// talks to any private store API, never uploads anything, and never runs
// any other installer binary. All winget output is treated as untrusted
// text and parsed by WingetOutputParser.
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenInstaller.Models;

namespace OpenInstaller.Services;

/// <summary>Raised when the winget executable cannot be started at all.</summary>
public sealed class WingetNotAvailableException : Exception
{
    public WingetNotAvailableException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

public sealed record InstallProgress(int? Percent, string Status);

public sealed record CommandResult(int ExitCode, IReadOnlyList<string> Lines)
{
    public string Text => string.Join(Environment.NewLine, Lines);
}

public sealed record InstallOutcome(
    bool Success,
    bool Cancelled,
    bool AlreadyInstalled,
    string Message,
    IReadOnlyList<string> Output);

public interface IWingetService
{
    Task<string?> GetWingetVersionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PackageInfo>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<PackageInfo?> SearchExactAsync(string id, CancellationToken cancellationToken = default);
    Task<PackageInfo?> GetInstalledAsync(string id, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>?> GetDetailsAsync(string id, CancellationToken cancellationToken = default);
    Task<InstallOutcome> InstallAsync(string id, IProgress<InstallProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<InstallOutcome> UpgradeAsync(string id, IProgress<InstallProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<bool> TryLaunchAsync(string appName, CancellationToken cancellationToken = default);
}

public sealed class WingetService : IWingetService
{
    private static readonly Regex PercentRegex = new(@"(\d{1,3})\s*%", RegexOptions.Compiled);

    public async Task<string?> GetWingetVersionAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(new[] { "--version" }, null, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0 || result.Lines.Count == 0)
        {
            return null;
        }
        return result.Lines[0].Trim();
    }

    public async Task<IReadOnlyList<PackageInfo>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(new[] { "search", "--query", query }, null, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0 || WingetOutputParser.IsNoPackageFound(result.Text))
        {
            return Array.Empty<PackageInfo>();
        }
        return WingetOutputParser.ParseSearch(result.Text);
    }

    public async Task<PackageInfo?> SearchExactAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(new[] { "search", "--id", id, "--exact" }, null, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            return null;
        }
        return WingetOutputParser.ParseSearch(result.Text).FirstOrDefault();
    }

    public async Task<PackageInfo?> GetInstalledAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(new[] { "list", "--id", id, "--exact" }, null, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0 || WingetOutputParser.IsNoPackageFound(result.Text))
        {
            return null;
        }
        var row = WingetOutputParser.ParseList(result.Text).FirstOrDefault();
        if (row.Id.Length == 0)
        {
            return null;
        }
        return new PackageInfo
        {
            Id = row.Id,
            Name = row.Name,
            Version = row.Version,
            InstalledVersion = row.Version,
            Source = row.Source,
        };
    }

    public async Task<Dictionary<string, string>?> GetDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(new[] { "show", "--id", id, "--exact" }, null, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            return null;
        }
        return WingetOutputParser.ParseShow(result.Text);
    }

    public Task<InstallOutcome> InstallAsync(string id, IProgress<InstallProgress>? progress = null, CancellationToken cancellationToken = default)
        => RunInstallCommandAsync(
            new[] { "install", "--id", id, "--exact", "--accept-package-agreements", "--accept-source-agreements" },
            progress, cancellationToken);

    public Task<InstallOutcome> UpgradeAsync(string id, IProgress<InstallProgress>? progress = null, CancellationToken cancellationToken = default)
        => RunInstallCommandAsync(
            new[] { "upgrade", "--id", id, "--exact", "--accept-package-agreements", "--accept-source-agreements" },
            progress, cancellationToken);

    public Task<bool> TryLaunchAsync(string appName, CancellationToken cancellationToken = default)
    {
        // Best effort only: on Windows the shell resolves App Paths and
        // PATH entries for many installed apps. If it cannot be resolved,
        // the caller tells the user to open the app from the Start menu.
        // There is no reliable generic "launch by winget id" command.
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(appName))
        {
            return Task.FromResult(false);
        }
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = appName,
                UseShellExecute = true,
            };
            using var process = Process.Start(startInfo);
            return Task.FromResult(process is not null);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException)
        {
            return Task.FromResult(false);
        }
    }

    private async Task<InstallOutcome> RunInstallCommandAsync(
        string[] args,
        IProgress<InstallProgress>? progress,
        CancellationToken cancellationToken)
    {
        var lines = new List<string>();
        CommandResult result;
        try
        {
            result = await RunAsync(args, line =>
            {
                lines.Add(line);
                var percent = ParsePercent(line);
                progress?.Report(new InstallProgress(percent, Truncate(line, 200)));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return new InstallOutcome(false, true, false, "Installation cancelled.", lines);
        }

        var text = result.Text;
        if (text.Contains("already installed", StringComparison.OrdinalIgnoreCase)
            || text.Contains("is already installed", StringComparison.OrdinalIgnoreCase))
        {
            return new InstallOutcome(false, false, true, "This package is already installed.", lines);
        }
        if (result.ExitCode == 0)
        {
            return new InstallOutcome(true, false, false, "Install completed successfully.", lines);
        }

        var lastMeaningful = lines.LastOrDefault(line => !string.IsNullOrWhiteSpace(line))
            ?? $"winget exited with code {result.ExitCode}.";
        return new InstallOutcome(
            false, false, false,
            $"Install failed (winget exit code {result.ExitCode}): {Truncate(lastMeaningful, 200)}",
            lines);
    }

    private static int? ParsePercent(string line)
    {
        var match = PercentRegex.Match(line);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var value))
        {
            return null;
        }
        return Math.Clamp(value, 0, 100);
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "…";

    /// <summary>
    /// Runs one winget command, optionally streaming output lines as they
    /// arrive. Cancellation kills the whole process tree.
    /// </summary>
    private static async Task<CommandResult> RunAsync(
        string[] args,
        Action<string>? onLine,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "winget",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException)
        {
            throw new WingetNotAvailableException(
                "winget was not found on this computer. Install 'App Installer' from the Microsoft Store, or install winget first, then try again.",
                ex);
        }

        if (process is null)
        {
            throw new WingetNotAvailableException("winget could not be started.");
        }

        using (process)
        {
            var lines = new List<string>();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    lock (lines) { lines.Add(e.Data); }
                    onLine?.Invoke(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    lock (lines) { lines.Add(e.Data); }
                    onLine?.Invoke(e.Data);
                }
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var registration = cancellationToken.Register(() =>
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }
            });

            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }
                throw;
            }

            // Give the async readers a moment to flush the final lines.
            await Task.Delay(50, CancellationToken.None).ConfigureAwait(false);
            lock (lines)
            {
                return new CommandResult(process.ExitCode, lines.ToArray());
            }
        }
    }
}
