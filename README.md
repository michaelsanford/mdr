# mdr

[![CI](https://github.com/michaelsanford/mdr/actions/workflows/ci.yml/badge.svg)](https://github.com/michaelsanford/mdr/actions/workflows/ci.yml)
[![Release](https://github.com/michaelsanford/mdr/actions/workflows/release.yml/badge.svg)](https://github.com/michaelsanford/mdr/actions/workflows/release.yml)
[![GitHub release](https://img.shields.io/github/v/release/michaelsanford/mdr)](https://github.com/michaelsanford/mdr/releases/latest)
[![License: MIT](https://img.shields.io/github/license/michaelsanford/mdr)](LICENSE)

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![Windows](https://img.shields.io/badge/Windows-x64%20|%20ARM64-0078D4?logo=windows)
![macOS](https://img.shields.io/badge/macOS-x64%20|%20ARM64-000000?logo=apple)
![Linux](https://img.shields.io/badge/Linux-x64%20|%20ARM64-FCC624?logo=linux&logoColor=black)

A cross-platform terminal-based markdown renderer for .NET 9.

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

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```sh
dotnet build -c Release
```

Or publish a self-contained binary for your platform:

```sh
dotnet publish -c Release -r linux-x64 --self-contained
dotnet publish -c Release -r osx-arm64 --self-contained
dotnet publish -c Release -r win-x64 --self-contained
```

## Usage

```sh
# Render a file
mdr README.md

# Pipe from stdin
cat README.md | mdr -
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
