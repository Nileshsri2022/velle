# OpenNotch

An original, open-source notch for Windows. Free forever: **no trial,
no account, no licence server, no network in v1.**

OpenNotch is **not** a copy of any other notch application. It does not
use another app's code, name, logo, assets, publisher identity, or
licence/trial system. It is built from scratch, in the same general
spirit: a small top-centre notch you can reach for all day.

## UI redesign — like in the video (after v1 push)

- Large dark panel, header "Clipboard — count" + Settings, filters +
  list + preview, **bottom nav** (not top tabs)
- Orange **Copy** button, **✓ Copied** + top-centre toast, then panel collapses
- **Open link** button + domain for Link items only
- Original code/icons/name only; no other app's assets or code reused.

## Step 1 — Full UI (done first, step by step)

- Collapsed top-centre notch pill: OpenNotch + clipboard count + Expand/Collapse + Close
- Expanded panel with tabs: **Clipboard, Timers, Notes, Shelf, Weather,
  Screen Time, Media**
- In v1, only **Clipboard** works. The other tabs are UI placeholders
  saying "coming next, step by step" — they do not run, track, or call
  the network yet.

## Clipboard (v1, working)

- Copy text in any app, then press **Refresh clipboard** — polling also
  checks once a second.
- Search clipboard history (case-insensitive, pinned first).
- Click an item to preview it, **Copy** it back, **Pin/Unpin** it,
  **Delete** it, or **Clear unpinned**.
- History is capped at 50 items; pinned items are never trimmed.
- Text only in v1. Images and files are not stored yet.

## Build and run on Windows

Prerequisites: Windows 10 / 11, .NET 8 SDK.

```powershell
dotnet build OpenNotch.sln -c Release
dotnet run --project src/OpenNotch/OpenNotch.csproj -c Release
```

On first run the window positions itself top-centre and stays on top.
Press Close (✕) in the pill to quit.

## Project layout

```
OpenNotch.sln
docs/DESIGN.md              superpowers brainstorm / design / plan
src/OpenNotch.Core/         clipboard history (no UI code, tested)
src/OpenNotch/              Avalonia UI: notch window + tabs
tests/OpenNotch.Tests/      xUnit tests for clipboard history
```

## Next steps (one at a time, with approval)

Timers, then Notes, then Shelf, then Weather, then Screen Time,
then Media — each as its own step, not all at once.
