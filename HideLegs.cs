using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Text.Json;
using static CounterStrikeSharp.API.Core.Listeners;

namespace HideLegs;

[MinimumApiVersion(369)]
public class HideLegsPlugin : BasePlugin
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public override string ModuleName => "Hide Legs Plugin";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "CS2KZ Port";
    public override string ModuleDescription => "Hide your legs in first person view";

    private readonly Dictionary<int, bool> _playerHideLegsState = [];
    private readonly Dictionary<string, bool> _playerSettings = [];
    private readonly object _settingsLock = new();
    private readonly string _configPath = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "configs", "plugins", "HideLegs", "player_settings.json");

    public override void Load(bool hotReload)
    {
        LoadPlayerSettings();
        RegisterListener<OnClientAuthorized>(OnClientAuthorized);
        RegisterListener<OnClientDisconnect>(OnClientDisconnect);

        if (hotReload)
        {
            Server.NextFrame(RestoreConnectedPlayers);
        }
    }

    public override void Unload(bool hotReload)
    {
        SavePlayerSettings();
    }

    private void OnClientAuthorized(int playerSlot, SteamID steamId)
    {
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        if (!IsValidHumanPlayer(player)) return;

        _playerHideLegsState[player.Slot] = GetPlayerSetting(GetPlayerIdentifier(steamId));
    }

    private void OnClientDisconnect(int playerSlot)
    {
        _playerHideLegsState.Remove(playerSlot);
    }

    [ConsoleCommand("css_hidelegs", "Toggle hide legs in first person")]
    [ConsoleCommand("css_hideleg", "Toggle hide legs in first person")]
    [ConsoleCommand("css_legs", "Toggle hide legs in first person")]
    public void OnHideLegsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsValidHumanPlayer(player)) return;

        if (!IsPlayerAliveAndNotSpectating(player))
        {
            player.PrintToChat(" \x07[Hide Legs]\x01 You must be alive to use this command.");
            return;
        }

        ToggleHideLegs(player);
    }

    private void ToggleHideLegs(CCSPlayerController player)
    {
        if (!_playerHideLegsState.TryGetValue(player.Slot, out var currentState))
        {
            currentState = GetPlayerSetting(player);
        }

        var hideLegs = !currentState;
        _playerHideLegsState[player.Slot] = hideLegs;

        UpdatePlayerModelAlpha(player, hideLegs);
        SetPlayerSetting(player, hideLegs);

        var message = hideLegs
            ? " \x04[Hide Legs]\x01 Legs are now hidden."
            : " \x04[Hide Legs]\x01 Legs are now visible.";
        player.PrintToChat(message);
    }

    private void UpdatePlayerModelAlpha(CCSPlayerController player, bool hideLegs)
    {
        if (!IsPlayerAliveAndNotSpectating(player)) return;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid) return;

        if (hideLegs)
        {
            playerPawn.RenderMode = RenderMode_t.kRenderTransAlpha;
            playerPawn.Render = Color.FromArgb(254, 255, 255, 255);
        }
        else
        {
            playerPawn.RenderMode = RenderMode_t.kRenderNormal;
            playerPawn.Render = Color.FromArgb(255, 255, 255, 255);
        }

        Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!IsValidHumanPlayer(player)) return HookResult.Continue;

        ScheduleApplySavedHideLegs(player);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Server.NextFrame(RestoreConnectedPlayers);
        return HookResult.Continue;
    }

    private void ScheduleApplySavedHideLegs(CCSPlayerController player)
    {
        Server.NextFrame(() =>
        {
            ApplySavedHideLegs(player);
            Server.NextFrame(() =>
            {
                ApplySavedHideLegs(player);
                Server.NextFrame(() => ApplySavedHideLegs(player));
            });
            ApplySavedHideLegsAfterFrames(player, 12);
        });
    }

    private void ApplySavedHideLegsAfterFrames(CCSPlayerController player, int frames)
    {
        if (frames <= 0)
        {
            ApplySavedHideLegs(player);
            return;
        }

        Server.NextFrame(() => ApplySavedHideLegsAfterFrames(player, frames - 1));
    }

    private void RestoreConnectedPlayers()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (IsValidHumanPlayer(player))
            {
                ApplySavedHideLegs(player);
            }
        }
    }

    private void ApplySavedHideLegs(CCSPlayerController player)
    {
        if (!IsValidHumanPlayer(player)) return;

        var hideLegs = GetPlayerSetting(player);
        _playerHideLegsState[player.Slot] = hideLegs;
        UpdatePlayerModelAlpha(player, hideLegs);
    }

    private void LoadPlayerSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            if (!File.Exists(_configPath)) return;

            var settings = JsonSerializer.Deserialize<Dictionary<string, bool>>(File.ReadAllText(_configPath), JsonOptions);
            if (settings == null) return;

            lock (_settingsLock)
            {
                _playerSettings.Clear();
                foreach (var (steamId, hideLegs) in settings)
                {
                    _playerSettings[steamId] = hideLegs;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load HideLegs player settings from {ConfigPath}", _configPath);
        }
    }

    private void SavePlayerSettings()
    {
        try
        {
            Dictionary<string, bool> snapshot;
            lock (_settingsLock)
            {
                snapshot = new(_playerSettings);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save HideLegs player settings to {ConfigPath}", _configPath);
        }
    }

    private static string GetPlayerIdentifier(SteamID steamId)
    {
        return steamId.SteamId64.ToString(CultureInfo.InvariantCulture);
    }

    private static string GetPlayerIdentifier(CCSPlayerController player)
    {
        var steamId = player.AuthorizedSteamID?.SteamId64 ?? player.SteamID;
        return steamId.ToString(CultureInfo.InvariantCulture);
    }

    private bool GetPlayerSetting(CCSPlayerController player)
    {
        return GetPlayerSetting(GetPlayerIdentifier(player));
    }

    private bool GetPlayerSetting(string steamId)
    {
        lock (_settingsLock)
        {
            return _playerSettings.GetValueOrDefault(steamId, false);
        }
    }

    private void SetPlayerSetting(CCSPlayerController player, bool hideLegs)
    {
        var steamId = GetPlayerIdentifier(player);
        lock (_settingsLock)
        {
            _playerSettings[steamId] = hideLegs;
        }
        SavePlayerSettings();
    }

    private static bool IsValidHumanPlayer([NotNullWhen(true)] CCSPlayerController? player)
    {
        return player is { IsValid: true } && !player.IsBot;
    }

    private static bool IsPlayerAliveAndNotSpectating(CCSPlayerController player)
    {
        if (!IsValidHumanPlayer(player)) return false;

        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid) return false;
        if (playerPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return false;
        return player.Team is not CsTeam.Spectator and not CsTeam.None;
    }
}
