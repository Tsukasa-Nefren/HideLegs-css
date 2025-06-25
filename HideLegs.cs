using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

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
    private readonly string _configPath = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "configs", "plugins", "HideLegs", "player_settings.json");

    public override void Load(bool hotReload)
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
        SetPlayerSetting(player, hideLegs);
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
        if (!IsPlayerAliveAndNotSpectating(player))
            return;
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
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        void ApplyHideLegs()
        {
            bool hideLegs = GetPlayerSetting(player);
            _playerHideLegsState[player.Slot] = hideLegs;
            if (hideLegs)
                UpdatePlayerModelAlpha(player, true);
        }

        // 여러 프레임 + 0.2초 후에도 한 번 더 적용
        Server.NextFrame(() => {
            ApplyHideLegs();
            Server.NextFrame(() => {
                ApplyHideLegs();
                Server.NextFrame(ApplyHideLegs);
            });
            // 약 0.2초(12프레임) 후에도 한 번 더 적용
            int delayFrames = 12;
            void DelayedApply(int count)
            {
                if (count <= 0) { ApplyHideLegs(); return; }
                Server.NextFrame(() => DelayedApply(count - 1));
            }
            DelayedApply(delayFrames);
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
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
            string json = JsonSerializer.Serialize(_playerSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HideLegs] Error saving player settings: {ex.Message}");
        }
    }

    private string GetPlayerIdentifier(CCSPlayerController player)
    {
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
        Task.Run(SavePlayerSettings);
    }

    private bool IsPlayerAliveAndNotSpectating(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return false;
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid)
            return false;
        if (playerPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return false;
        if (player.TeamNum == (int)CsTeam.Spectator || player.TeamNum == (int)CsTeam.None)
            return false;
        return true;
    }
}
