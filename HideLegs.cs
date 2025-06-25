using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Text.Json;

namespace HideLegs;

[MinimumApiVersion(276)]
public class HideLegsPlugin : BasePlugin
{
    public override string ModuleName => "Hide Legs Plugin";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "CS2KZ Port";
    public override string ModuleDescription => "Hide your legs in first person view";

    private readonly Dictionary<int, bool> _playerHideLegsState = new();
    private readonly Dictionary<string, bool> _playerSettings = new();
    private readonly string _configPath = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "configs", "plugins", "HideLegs", "player_settings.json");    public override void Load(bool hotReload)
    {
        LoadPlayerSettings();
    }

    public override void Unload(bool hotReload)
    {
        SavePlayerSettings();
    }

    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        // Load player's saved setting
        bool savedSetting = GetPlayerSetting(player);
        _playerHideLegsState[player.Slot] = savedSetting;
        
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // Clean up player state
        _playerHideLegsState.Remove(player.Slot);
        return HookResult.Continue;
    }

    [ConsoleCommand("css_hidelegs", "Toggle hide legs in first person")]
    [ConsoleCommand("css_hideleg", "Toggle hide legs in first person")]
    [ConsoleCommand("css_legs", "Toggle hide legs in first person")]
    public void OnHideLegsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        // Check if player is alive and not spectating
        if (!IsPlayerAliveAndNotSpectating(player))
        {
            player.PrintToChat($" \x07[Hide Legs]\x01 You must be alive to use this command.");
            return;
        }

        ToggleHideLegs(player);
    }

    private void ToggleHideLegs(CCSPlayerController player)
    {
        if (!_playerHideLegsState.ContainsKey(player.Slot))
            _playerHideLegsState[player.Slot] = GetPlayerSetting(player);

        _playerHideLegsState[player.Slot] = !_playerHideLegsState[player.Slot];
        bool hideLegs = _playerHideLegsState[player.Slot];

        UpdatePlayerModelAlpha(player, hideLegs);
        
        // Save the setting permanently
        SetPlayerSetting(player, hideLegs);

        // Send feedback message
        string message = hideLegs 
            ? $" \x04[Hide Legs]\x01 Legs are now hidden."
            : $" \x04[Hide Legs]\x01 Legs are now visible.";
        
        player.PrintToChat(message);
    }

    private void UpdatePlayerModelAlpha(CCSPlayerController player, bool hideLegs)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
            return;

        // Only apply effects if player is alive and not spectating
        if (!IsPlayerAliveAndNotSpectating(player))
            return;

        if (hideLegs)
        {
            // CS2KZ method: Set alpha to 254 to hide legs in first person view only
            // This makes legs invisible to the player but keeps the body visible to others
            playerPawn.RenderMode = RenderMode_t.kRenderTransAlpha;
            playerPawn.Render = Color.FromArgb(254, 255, 255, 255); // Alpha 254 instead of 1
        }
        else
        {
            // Restore normal rendering
            playerPawn.RenderMode = RenderMode_t.kRenderNormal;
            playerPawn.Render = Color.FromArgb(255, 255, 255, 255);
        }

        // Send changes to client
        Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;        // Apply existing settings after spawn
        Server.NextFrame(() =>
        {
            if (_playerHideLegsState.ContainsKey(player.Slot) && _playerHideLegsState[player.Slot])
            {
                UpdatePlayerModelAlpha(player, true);
            }
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {        // Restore hide legs settings for all players at round start
        Server.NextFrame(() =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.IsBot)
                    continue;

                if (_playerHideLegsState.ContainsKey(player.Slot) && _playerHideLegsState[player.Slot])
                {
                    UpdatePlayerModelAlpha(player, true);
                }
            }
        });

        return HookResult.Continue;
    }

    private void LoadPlayerSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            
            if (File.Exists(_configPath))
            {
                string json = File.ReadAllText(_configPath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                if (settings != null)
                {
                    _playerSettings.Clear();
                    foreach (var kvp in settings)
                    {
                        _playerSettings[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HideLegs] Error loading player settings: {ex.Message}");
        }
    }

    private void SavePlayerSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            
            string json = JsonSerializer.Serialize(_playerSettings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HideLegs] Error saving player settings: {ex.Message}");
        }
    }

    private string GetPlayerIdentifier(CCSPlayerController player)
    {
        // Use SteamID as unique identifier
        return player.SteamID.ToString();
    }

    private bool GetPlayerSetting(CCSPlayerController player)
    {
        string steamId = GetPlayerIdentifier(player);
        return _playerSettings.GetValueOrDefault(steamId, false);
    }

    private void SetPlayerSetting(CCSPlayerController player, bool hideLegs)
    {
        string steamId = GetPlayerIdentifier(player);
        _playerSettings[steamId] = hideLegs;
        
        // Save immediately to prevent data loss
        Task.Run(SavePlayerSettings);
    }

    private bool IsPlayerAliveAndNotSpectating(CCSPlayerController player)
    {
        // Check if player is valid
        if (player == null || !player.IsValid)
            return false;

        // Check if player pawn exists and is valid
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
            return false;

        // Check if player is alive
        if (playerPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return false;

        // Check if player is not spectating
        if (player.TeamNum == (int)CsTeam.Spectator || player.TeamNum == (int)CsTeam.None)
            return false;

        return true;
    }
}
