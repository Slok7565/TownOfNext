using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public static class Mare
    {
        private static readonly int Id = 2300;
        public static List<byte> playerIdList = new();

        private static OptionItem KillCooldownInLightsOut;
        private static OptionItem SpeedInLightsOut;
        private static bool idAccelerated = false;  //加速済みかフラグ


        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Mare);
            SpeedInLightsOut = OptionItem.Create(Id + 10, TabGroup.ImpostorRoles, Color.white, "MareAddSpeedInLightsOut", 0.3f, 0.1f, 0.5f, 0.1f, Options.CustomRoleSpawnChances[CustomRoles.Mare], format: OptionFormat.Multiplier);
            KillCooldownInLightsOut = OptionItem.Create(Id + 11, TabGroup.ImpostorRoles, Color.white, "MareKillCooldownInLightsOut", 15f, 2.5f, 180f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.Mare], format: OptionFormat.Seconds);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte mare)
        {
            playerIdList.Add(mare);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static float GetKillCooldown => Utils.IsActive(SystemTypes.Electrical) ? KillCooldownInLightsOut.GetFloat() : Options.DefaultKillCooldown;
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = GetKillCooldown;
        public static void ApplyGameOptions(GameOptionsData opt, byte playerId)
        {
            if (Utils.IsActive(SystemTypes.Electrical) && !idAccelerated)
            { //停電中で加速済みでない場合
                idAccelerated = true;
                Main.AllPlayerSpeed[playerId] += SpeedInLightsOut.GetFloat();//Mareの速度を加算
            }
            else if (!Utils.IsActive(SystemTypes.Electrical) && idAccelerated)
            { //停電中ではなく加速済みになっている場合
                idAccelerated = false;
                Main.AllPlayerSpeed[playerId] -= SpeedInLightsOut.GetFloat();//Mareの速度を減算
            }
        }

        public static void OnCheckMurder(PlayerControl killer)
        {
        }
        public static void FixedUpdate(PlayerControl player)
        {
        }
    }
}