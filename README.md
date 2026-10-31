# OpenInstaller

An original, open-source Windows installer helper.

OpenInstaller gives you a simple, dark-themed window to find and install
Windows applications. It does **not** bundle, copy, or re-implement any
other installer program. It is inspired only by the *general flow* of a
modern installer window — search, check, install with progress, handle
errors, launch — and every line of code, layout, and text in this
repository was written for this project.

## What it does

- **Search** — type an app name or package id (for example `VideoLAN.VLC`)
  and search the winget catalog.
- **Details** — shows the selected package's name, id, publisher,
  description, and latest version.
- **Check if already installed** — runs `winget list` for the selected
  package and shows the installed version, or "Not installed."
- **Install** — runs `winget install` for the selected package, with a
  progress bar, live status text, and a log of winget's output.
- **Update check** — compares the installed version with the latest
  version and says "Update available" or "Up to date."
- **Update** — runs `winget upgrade` for the selected package.
- **Cancel** — stops the running winget process.
- **Retry** — re-runs the last install/update attempt after a failure.
- **Launch** — best-effort launch of the installed app by name through
  the Windows shell. Some apps cannot be launched this way; in that case
  OpenInstaller tells you to open the app from the Start menu.

## Error messages in plain language

| Situation | What you see |
|---|---|
| winget is not installed | "winget was not found on this computer. Install 'App Installer' first…" |
| No match for your search | "Package not found. Check the spelling, or try a broader name." |
| Package already installed | "Already installed. Use Update if a newer version is available." |
| Install fails | "Install failed (winget exit code …): …" plus the log |
| You press Cancel | "Installation cancelled." |

## How it works

OpenInstaller is a thin GUI over the **public `winget` command-line
interface** — `winget search`, `winget show`, `winget list`,
`winget install`, and `winget upgrade`. It does not use any private
store API, does not upload anything, and does not run any other
installer binary. All winget output is treated as text and parsed
locally (see `WingetOutputParser` in `OpenInstaller.Core`).

The UI is built with [Avalonia UI](https://avaloniaui.net/) 11.1.4 on
.NET 8, so the same project builds on Windows and on Linux/macOS for
development, while the install features themselves require Windows +
winget at run time.

## Project layout

```
OpenInstaller.sln
src/
  OpenInstaller/            Avalonia GUI app (MainWindow, MainViewModel)
  OpenInstaller.Core/       winget wrapper + output parser (no UI code)
tests/
  OpenInstaller.Tests/      xUnit tests for the winget output parser
```

## Build and run on Windows

Prerequisites:

1. Windows 10 / 11.
2. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
3. winget — it ships with Windows as part of "App Installer". Check with:

```powershell
winget --version
```

If that fails, install **App Installer** from the Microsoft Store first.

Then, from this folder:

```powershell
dotnet build OpenInstaller.sln -c Release
dotnet run --project src/OpenInstaller/OpenInstaller.csproj -c Release
```

Or run the built executable directly:

```powershell
.\src\OpenInstaller\bin\Release\net8.0\OpenInstaller.exe
```

## Build and test on Linux (development check)

The GUI builds on Linux too, but winget is Windows-only, so on Linux
the app will start and correctly report "winget: not installed". The
unit tests do not need winget at all:

```bash
dotnet build OpenInstaller.sln -c Release
dotnet test tests/OpenInstaller.Tests/OpenInstaller.Tests.csproj -c Release
```

## What is NOT implemented yet

- No package browsing by category, no curated "app store" catalogue —
  search is the way in.
- No silent/forced install options beyond winget's package and source
  agreement flags.
- No per-package icon or screenshot download.
- Launch is best-effort (see above); there is no generic
  "launch by winget id" command in winget itself.
- No auto-update of OpenInstaller itself.

## Origin note

This project was written from scratch after a static, read-only look
at the *behaviour* of a small .NET installer helper (find a package,
check installed state, download/install with progress, launch or
redirect). No code, strings, assets, branding, or trademarks from that
program — or from any Microsoft Store program — were copied, reused,
or included here, and the uploaded installer file itself is not part
of this repository.

## Licence

MIT — see [LICENSE](LICENSE).
