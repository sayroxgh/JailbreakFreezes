using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace JailbreakFreezes;

public class JailbreakFreezes : BasePlugin
{
    public override string ModuleName => "Jailbreak Freezes";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "Sayrox";

    private CounterStrikeSharp.API.Modules.Timers.Timer? _freezeTimer;
    private int _remainingSeconds = 0;
    
    private readonly List<uint> _frozenTPlayers = new();

    private string Prefix => " \x01[\x04 Sayrox Freezes \x01] ";

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(OnTick);
    }
    
    private void OnTick()
    {
        if (_remainingSeconds <= 0) return;
        
        string hudMessage = "<font color='#00FFFF'>T Oyuncularinin Donmasina Son: </font><font color='#FF4500' class='fontSize-xl'>" + _remainingSeconds + "</font>";
        foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot))
        {
            player.PrintToCenterHtml(hudMessage);
        }
    }

    [ConsoleCommand("css_fz")]
    [RequiresPermissions("@css/generic")]
    public void Command_StartFreezeTimer(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        if (info.ArgCount < 2 || !int.TryParse(info.GetArg(1), out int seconds) || seconds <= 0)
        {
            player.PrintToChat($"{Prefix}\x07Kullanim: \x04!fz <saniye>");
            return;
        }

        _freezeTimer?.Kill();
        _freezeTimer = null;
        _remainingSeconds = seconds;
        Server.PrintToChatAll($"{Prefix}\x04T Oyuncuları \x07{seconds} \x04saniye sonra dondurulacak!");

        _freezeTimer = AddTimer(1.0f, () =>
        {
            _remainingSeconds--;
            if (_remainingSeconds <= 0)
            {
                FreezeAllT();
                PlayGlobalSound("sounds/jailbreakextras/freeze.vsnd");
                Server.PrintToChatAll($"{Prefix}\x02 T Oyuncuları donduruldu!");
                _freezeTimer?.Kill();
                _freezeTimer = null;
            }
        }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
    }

    [ConsoleCommand("css_fz0")]
    [RequiresPermissions("@css/generic")]
    public void Command_StopFreezeTimer(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        if (_freezeTimer == null)
        {
            player.PrintToChat($"{Prefix}\x07Aktif bir dondurma geri sayimi yok.");
            return;
        }
        _freezeTimer.Kill();
        _freezeTimer = null;
        _remainingSeconds = 0;
        Server.PrintToChatAll($"{Prefix}\x04Dondurma geri sayimi \x07{player.PlayerName} \x04tarafindan iptal edildi.");
    }

    [ConsoleCommand("css_td")]
    [RequiresPermissions("@css/generic")]
    public void Command_FreezeAllT(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        FreezeAllT();
        PlayGlobalSound("sounds/jailbreakextras/freeze.vsnd");
        Server.PrintToChatAll($"{Prefix}\x02T Oyuncuları \x07{player.PlayerName} \x04tarafindan donduruldu!");
    }

    [ConsoleCommand("css_tdb")]
    [RequiresPermissions("@css/generic")]
    public void Command_UnfreezeAllT(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;
        if (_frozenTPlayers.Count == 0)
        {
            player.PrintToChat($"{Prefix}\x07 Dondurulmus T oyuncusu bulunmuyor.");
            return;
        }
        UnfreezeAllT();
        Server.PrintToChatAll($"{Prefix}\x04T oyuncularinin donu \x07{player.PlayerName} \x04tarafindan çözüldü!");
    }

    private void FreezeAllT()
    {
        var tPlayers = Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive && p.TeamNum == 2);
        foreach (var tPlayer in tPlayers)
        {
            FreezePlayer(tPlayer);
        }
    }

    private void UnfreezeAllT()
    {
        foreach (var index in _frozenTPlayers.ToList())
        {
            var p = Utilities.GetPlayerFromIndex((int)index);
            if (p != null && p.IsValid)
            {
                UnfreezePlayer(p);
            }
        }
        _frozenTPlayers.Clear();
    }

    private void FreezePlayer(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        if (!_frozenTPlayers.Contains(player.Index))
        {
            ChangeMovetype(pawn, MoveType_t.MOVETYPE_NONE, Color.SkyBlue);
            _frozenTPlayers.Add(player.Index);
        }
    }

    private void UnfreezePlayer(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;
        
        ChangeMovetype(pawn, MoveType_t.MOVETYPE_WALK, Color.White);
    }

    private void ChangeMovetype(CBasePlayerPawn pawn, MoveType_t movetype, Color? glow)
    {
        pawn.MoveType = movetype;
        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (byte)movetype);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        
        if (glow.HasValue)
        {
            Glow(pawn, glow.Value);
        }
    }

    private void Glow(CBasePlayerPawn playerPawn, Color color)
    {
        playerPawn.RenderMode = (RenderMode_t)1;
        playerPawn.Render = color;
        Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");
    }

    private void PlayGlobalSound(string soundPath)
    {
        foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot))
        {
            player.ExecuteClientCommand($"play {soundPath}");
        }
    }
}