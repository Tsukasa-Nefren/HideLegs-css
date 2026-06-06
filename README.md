# HideLegs Plugin for CounterStrikeSharp

[![Build](https://github.com/Tsukasa-Nefren/Hidelegs-css/actions/workflows/build.yml/badge.svg)](https://github.com/Tsukasa-Nefren/Hidelegs-css/actions/workflows/build.yml)
[![Release](https://github.com/Tsukasa-Nefren/Hidelegs-css/actions/workflows/release.yml/badge.svg)](https://github.com/Tsukasa-Nefren/Hidelegs-css/actions/workflows/release.yml)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/Tsukasa-Nefren/Hidelegs-css)](https://github.com/Tsukasa-Nefren/Hidelegs-css/releases/latest)
[![GitHub downloads](https://img.shields.io/github/downloads/Tsukasa-Nefren/Hidelegs-css/total)](https://github.com/Tsukasa-Nefren/Hidelegs-css/releases)

A CounterStrikeSharp plugin that allows players to hide their legs in first-person view for Counter-Strike 2.

## Features

- Toggle leg visibility in first-person view
- Persistent per-player settings
- Command aliases: `css_hidelegs`, `css_hideleg`, `css_legs`
- Automatic restoration after spawn, round start, and hot reload
- Uses model alpha `254` so the local first-person legs are hidden while normal visibility is preserved for other players

## Requirements

- Counter-Strike 2 server
- CounterStrikeSharp API 1.0.369 or newer compatible build
- .NET 10 runtime

## Installation

1. Download the latest `HideLegs-vX.X.X.zip` from Releases.
2. Extract the archive into the server `csgo` folder.
3. Confirm the plugin files are under:

```text
csgo/addons/counterstrikesharp/plugins/HideLegs/
```

4. Restart the server or reload the plugin.

## Commands

- `css_hidelegs` - Toggle hide legs in first person
- `css_hideleg` - Toggle hide legs in first person
- `css_legs` - Toggle hide legs in first person

Players must be alive and not spectating to use the command.

## Configuration

The plugin automatically creates and manages:

```text
csgo/addons/counterstrikesharp/configs/plugins/HideLegs/player_settings.json
```

The file stores player preferences by SteamID64.

## Building

```bash
dotnet restore
dotnet build --configuration Release
dotnet publish --configuration Release --output ./publish
```

The compiled plugin files are emitted under `bin/Release/net10.0/` and `publish/`.

## Technical Details

- Version: 1.1.0
- Target Framework: .NET 10
- Minimum CounterStrikeSharp API: 369
- Package Reference: CounterStrikeSharp.API 1.0.369

## Version History

### v1.1.0

- Updated to .NET 10 and CounterStrikeSharp.API 1.0.369
- Switched player authorization/disconnect tracking to current CSSSharp listeners
- Made settings saves snapshot-based and race-safe
- Updated GitHub Actions for the current `master` branch and full publish-folder release packaging

### v1.0.1

- Previous public release

### v1.0.0

- Initial release
- Basic leg hiding functionality
- Persistent player settings
- Multiple command aliases
- Automatic state restoration

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Acknowledgments

- CS2KZ community for the original leg hiding technique
- CounterStrikeSharp team
