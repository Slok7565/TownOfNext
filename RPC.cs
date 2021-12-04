using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using Hazel;
using System.Threading;
using System.Threading.Tasks;

namespace TownOfHost {
    enum CustomRPC {
        SyncCustomSettings = 80,
        JesterExiled,
        EndGame
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    class RPCHandlerPatch {
        public static void Postfix([HarmonyArgument(0)]byte callId, [HarmonyArgument(1)]MessageReader reader) {
            byte packetID = callId;
            switch(packetID) {
                case (byte)CustomRPC.SyncCustomSettings:
                    bool jester = reader.ReadBoolean();
                    bool madmate = reader.ReadBoolean();
                    RPCProcedure.SyncCustomSettings(jester,madmate);
                    break;
                case (byte)CustomRPC.JesterExiled:
                    byte exiledJester = reader.ReadByte();
                    RPCProcedure.JesterExiled(exiledJester);
                    break;
                case (byte)CustomRPC.EndGame:
                    RPCProcedure.EndGame();
                    break;
            }
        }
    }
    class RPCProcedure {
        public static void SyncCustomSettings(bool jester, bool madmate) {
            main.JesterEnabled = jester;
            main.MadmateEnabled = madmate;
            main.currentWinner = CustomWinner.Default;
            main.JesterWinTrigger = false;
        }
        public static void JesterExiled(byte jesterID) {
            main.ExiledJesterID = jesterID;
            main.currentWinner = CustomWinner.Jester;
            PlayerControl Jester = null;
            PlayerControl Imp = null;
            List<PlayerControl> otherImpostors = new List<PlayerControl>();
            foreach(var p in PlayerControl.AllPlayerControls) {
                if(p.PlayerId == jesterID) Jester = p;
                if(p.Data.Role.IsImpostor) {
                    if(Imp == null) Imp = p;
                    else otherImpostors.Add(p);
                }
            }
            if(AmongUsClient.Instance.AmHost){
                Imp.RpcSetColor((byte)Jester.Data.Outfits[PlayerOutfitType.Default].ColorId);
                Imp.RpcSetHat(Jester.Data.Outfits[PlayerOutfitType.Default].HatId);
                Imp.RpcSetVisor(Jester.Data.Outfits[PlayerOutfitType.Default].VisorId);
                Imp.RpcSetSkin(Jester.Data.Outfits[PlayerOutfitType.Default].SkinId);
                Imp.RpcSetPet(Jester.Data.Outfits[PlayerOutfitType.Default].PetId);
                Imp.RpcSetName(Jester.Data.Outfits[PlayerOutfitType.Default].PlayerName + "\r\nJester wins");
            }
            if(AmongUsClient.Instance.AmHost){
                Task task = Task.Run(() => {
                    Thread.Sleep(500);
                    main.JesterWinTrigger = true;
                });
            }
        }
        public static void EndGame() {
            main.currentWinner = CustomWinner.Draw;
            if(AmongUsClient.Instance.AmHost){
                ShipStatus.Instance.enabled = false;
                ShipStatus.RpcEndGame(GameOverReason.ImpostorByKill, false);
            }
        }
    }
}