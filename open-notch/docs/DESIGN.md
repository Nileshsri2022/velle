# OpenNotch v1 — superpowers design doc

## Brainstorm (what, why, what not)
- Goal: an original, open-source notch for Windows, free forever. No trial,
  no licence server, no payment, no analytics, no network calls in v1.
- V1 scope, and only v1: a top-centre notch window + searchable clipboard
  history (text only). Everything else from the Vayli feature list
  (timers, file shelf, weather, notes, screen time, media, Drop) is OUT
  of v1, to be added one feature at a time, with user approval each time.
- Explicitly NOT Vayli: no Vayli code, name, logo, assets, publisher
  identity, Store signature, licence/trial system, or Drop server code
  is reused. Feature ideas (notch, clipboard) are general concepts,
  implemented here from scratch. QRCoder/Geist-style open licences are
  not needed in v1.
- Stack choice: Avalonia UI 11.1.4 on .NET 8, same stack as OpenInstaller,
  because it builds and verifies on this Linux VM and runs on Windows.
  Real Vayli is WPF; WPF cannot be build-verified here, so v1 is Avalonia
  and Windows clipboard watching is by polling (1s) plus manual Refresh,
  not by a Windows-only AddClipboardFormatListener hook in v1.

## Design
- Core (no UI): `ClipboardHistory` — in-memory, newest first.
  - Add: ignore null/whitespace; ignore a text identical to the newest
    item (consecutive duplicate); cap at 50, trimming oldest UNPINNED
    first; pinned items are never trimmed.
  - Search: case-insensitive substring; pinned first, then newest first.
  - Pin/Unpin, Remove, ClearUnpinned, ClearAll.
- UI: one top-centre window, Topmost, no system decorations, dark.
  - Collapsed pill: "OpenNotch" + item count + Expand/Collapse button.
  - Expanded panel: search box, list (pinned first), selected item
    preview, buttons Copy / Pin-Unpin / Delete / Clear unpinned /
    Refresh clipboard, status line, footer "Free forever. No trial.
    No account. No network."
  - Copy-back: sets the system clipboard to the selected text; the
    poller ignores the text it just set, so it does not duplicate.
  - Manual "Add test item" is NOT in the shipped UI; tests cover core.

## Plan (TDD)
1. Write ClipboardHistoryTests first (red): add, dedupe, cap/trim,
   pinned-exempt, search order, pin/unpin, remove, clear.
2. Run tests -> fail (no core project yet). Implement core -> green.
3. Build Avalonia UI on top of core, wire polling + copy-back.
4. `dotnet build -c Release` (0 warnings target) + `dotnet test`.
5. Report honestly: Windows GUI + live clipboard polling NOT yet
   run end-to-end on a real Windows PC in v1, same limit as OpenInstaller v1.

## Step 1 (done 2026-10-31, after user video)
- Filters All/Text/Images/Links with counts, time-ago, word+char counts.
- Link = single URL with no spaces; Text = everything else; Image = 0 in v1.
- Browser preview updated with the same Step 1 UI. Push to velle still
  blocked until user approves this preview + push layout.
