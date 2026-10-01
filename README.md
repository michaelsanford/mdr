# mdr

[![CI](https://github.com/michaelsanford/mdr/actions/workflows/ci.yml/badge.svg)](https://github.com/michaelsanford/mdr/actions/workflows/ci.yml)
[![Release](https://github.com/michaelsanford/mdr/actions/workflows/release.yml/badge.svg)](https://github.com/michaelsanford/mdr/actions/workflows/release.yml)
[![License: MIT](https://img.shields.io/github/license/michaelsanford/mdr)](LICENSE)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
[![Security](https://img.shields.io/badge/security-aikido-blueviolet?logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0id2hpdGUiIGQ9Ik0xMiAyTDMgN3Y1YzAgNS41NSAzLjg0IDEwLjc0IDkgMTIgNS4xNi0xLjI2IDktNi40NSA5LTEyVjdMMTIgMnoiLz48L3N2Zz4=)](https://app.aikido.dev/)

[![GitHub release](https://img.shields.io/github/v/release/michaelsanford/mdr)](https://github.com/michaelsanford/mdr/releases/latest)

![Windows](https://img.shields.io/badge/Windows-x64%20|%20ARM64-0078D4?logo=windows)
[![Winget](https://github.com/michaelsanford/mdr/actions/workflows/winget.yml/badge.svg)](https://github.com/michaelsanford/mdr/actions/workflows/winget.yml)

![macOS](https://img.shields.io/badge/macOS-x64%20|%20ARM64-000000?logo=apple)
![Linux](https://img.shields.io/badge/Linux-x64%20|%20ARM64-FCC624?logo=linux&logoColor=black)

A cross-platform terminal-based markdown renderer for .NET 10.

Renders markdown with full ANSI formatting — bold, italic, colors, syntax-highlighted code blocks, tables, and more — adapted to your terminal width. Includes an interactive pager with vim-style navigation and multiple color schemes.

## Features

- **Headings** — color-coded by level
- **Bold / Italic** — native terminal formatting
- **Code blocks** — bordered panels with language label and keyword highlighting (C#, JS/TS, Python, Rust, Go)
- **Inline code** — distinct background
- **Tables** — full-width with rounded borders
- **Lists** — ordered, unordered, nested
- **Blockquotes** — vertical bar + italic
- **Links** — underlined with URL displayed
- **Horizontal rules**
- **Interactive pager** — scrollable output with status bar
- **Color schemes** — cycle through Monokai, Dracula, Nord, GitHub Dark, Solarized, Catppuccin

## Install

### winget (Windows)

```powershell
winget install michaelsanford.mdr
```

### From source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
dotnet build -c Release
```

Or publish a native binary for your platform. Releases are compiled with
[NativeAOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/) — a
self-contained native executable with no runtime dependency — so publishing
requires the platform's C/C++ toolchain
([prerequisites](https://learn.microsoft.com/dotnet/core/deploying/native-aot/#prerequisites)):
the "Desktop development with C++" workload on Windows, `clang` + `zlib1g-dev`
on Linux, and Xcode command line tools on macOS.

```powershell
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r osx-arm64
dotnet publish -c Release -r win-x64
```

## Usage

```powershell
# Render a file
mdr README.md

# Pipe from stdin (bash)
cat README.md | mdr -

# Pipe from stdin (PowerShell)
Get-Content README.md | mdr -
```

When output exceeds the terminal height, mdr enters an interactive pager. If output is piped or fits on screen, it prints directly.

## Keybindings

| Key | Action |
|-----|--------|
| ↑ / k | Scroll up |
| ↓ / j | Scroll down |
| PgUp / b | Page up |
| PgDn / Space | Page down |
| Home / g | Jump to top |
| End / G | Jump to bottom |
| t | Cycle color scheme |
| q / Esc | Quit |

## Color Schemes

Press `t` to cycle through:

- **Monokai** — warm pinks, yellows, greens
- **Dracula** — purples, pinks, cyans
- **Nord** — cool blues, teals
- **GitHub Dark** — blues, oranges, greens
- **Solarized** — classic amber, teal
- **Catppuccin** — soft pastels

## Dependencies

- [Markdig](https://github.com/xoofx/markdig) — markdown parsing
- [Spectre.Console](https://spectreconsole.net/) — terminal capability detection

## License

MIT
