# Privacy Policy

**mdr** is a fully offline, local-only command-line tool. It is designed with privacy as a default.

## Data Collection

mdr does **not**:

- Collect, transmit, or store any personal data
- Send telemetry, analytics, or usage statistics
- Make any network requests
- Phone home, check for updates, or contact any server
- Read or access any files other than the markdown file you explicitly pass to it

## How It Works

mdr reads a markdown file (or stdin) on your local machine, renders it in your terminal, and exits. All processing happens entirely in-process on your device. Nothing leaves your computer.

## Third-Party Dependencies

mdr depends on [Markdig](https://github.com/xoofx/markdig) (markdown parsing) and [Spectre.Console](https://spectreconsole.net/) (terminal detection). Neither library performs any network activity at runtime.

## Updates

mdr does not auto-update. You control when and how you update by downloading releases from [GitHub](https://github.com/michaelsanford/mdr/releases).

## Contact

If you have questions about this policy, open an issue at [github.com/michaelsanford/mdr](https://github.com/michaelsanford/mdr/issues).
