# HideLegs Plugin for CounterStrikeSharp

[![Build](https://github.com/Tsukasa-Nefren/Hidelegs-css/actions/workflows/build.yml/badge.svg)](https://github.com/Tsukasa-Nefren/Hidelegs-css/actions/workflows/build.yml)
[![Release](https://github.com/Tsukasa-Nefren/Hidelegs-css/actions/workflows/release.yml/badge.svg)](https://github.com/Tsukasa-Nefren/Hidelegs-css/actions/workflows/release.yml)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/Tsukasa-Nefren/Hidelegs-css)](https://github.com/Tsukasa-Nefren/Hidelegs-css/releases/latest)
[![GitHub downloads](https://img.shields.io/github/downloads/Tsukasa-Nefren/Hidelegs-css/total)](https://github.com/Tsukasa-Nefren/Hidelegs-css/releases)

A CounterStrikeSharp plugin that allows players to hide their legs in first-person view for Counter-Strike 2.

## 🎯 Features

- **Toggle leg visibility** in first-person view
- **Persistent settings** - player preferences are saved and restored
- **Multiple commands** for easy access (`css_hidelegs`, `css_hideleg`, `css_legs`)
- **Automatic restoration** after spawn and round start
- **Safe implementation** - only affects first-person view, other players see you normally

## 📋 Requirements

- **Counter-Strike 2** server
- **CounterStrikeSharp** (minimum API version 276)
- **.NET 8.0** runtime

## 🚀 Installation

### Option 1: Download from Releases (Recommended)
1. Go to the [Releases](../../releases) page
2. Download the latest `HideLegs-vX.X.X.zip` file
3. Extract the zip file
4. Place `HideLegs.dll` in your CounterStrikeSharp plugins folder:
   ```
   csgo/addons/counterstrikesharp/plugins/HideLegs/
   ```
5. Restart your server or use `css_plugins reload`

### Option 2: Build from Source
1. Clone this repository
2. Follow the [Building from Source](#-building-from-source) instructions below

## 🎮 Usage

### Commands

- `css_hidelegs` - Toggle hide legs in first person
- `css_hideleg` - Toggle hide legs in first person  
- `css_legs` - Toggle hide legs in first person

### Requirements
- You must be **alive** and **not spectating** to use the command
- Settings are automatically saved per player (using SteamID)

## ⚙️ Configuration

The plugin automatically creates a configuration file at:
```
csgo/addons/counterstrikesharp/configs/plugins/HideLegs/player_settings.json
```

This file stores individual player preferences and is automatically managed by the plugin.

## 🔧 Building from Source

### Prerequisites
- .NET 8.0 SDK
- CounterStrikeSharp.API NuGet package

### Build Steps
```bash
git clone <your-repo-url>
cd HideLegs
dotnet restore
dotnet build
```

The compiled `HideLegs.dll` will be in `bin/Debug/net8.0/` or `bin/Release/net8.0/`

## 📝 How it Works

The plugin uses the CS2KZ method by setting the player model's alpha value to 254 (instead of 255) which makes the legs invisible to the player in first-person view while keeping the body visible to other players.

### Key Features:
- **Alpha transparency**: Uses `RenderMode_t.kRenderTransAlpha` with alpha value 254
- **State management**: Tracks player preferences in memory and saves to JSON
- **Event handling**: Automatically restores settings on player spawn and round start
- **Validation**: Ensures players are alive and not spectating before applying effects

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **CS2KZ** community for the original leg hiding technique
- **CounterStrikeSharp** team for the excellent API
- **Counter-Strike 2** modding community

## 📊 Version History

### v1.0.0
- Initial release
- Basic leg hiding functionality
- Persistent player settings
- Multiple command aliases
- Automatic state restoration

## 🐛 Issues & Support

If you encounter any issues or have suggestions, please:
1. Check the [Issues](../../issues) page
2. Create a new issue with detailed information
3. Include server logs if applicable

## 🔗 Related Projects

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
- [CS2KZ](https://github.com/KZGlobalTeam/cs2kz-metamod)
