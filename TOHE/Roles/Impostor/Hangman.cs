﻿using AmongUs.GameOptions;

using TOHE.Roles.Core;
using TOHE.Roles.Core.Interfaces;

namespace TOHE.Roles.Impostor;
public sealed class Hangman : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Hangman),
            player => new Hangman(player),
            CustomRoles.Hangman,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            3800,
            SetupOptionItem,
            "ha"
        );
    public Hangman(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionShapeshiftCooldown;
    static OptionItem OptionShapeshiftDuration;

    public bool IsKiller { get; private set; } = false;
    private static void SetupOptionItem()
    {
        OptionShapeshiftCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.ShapeshiftCooldown, new(2.5f, 180f, 2.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShapeshiftDuration = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.ShapeshiftDuration, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = OptionShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = OptionShapeshiftDuration.GetFloat();
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (Main.CheckShapeshift.TryGetValue(killer.PlayerId, out var s) && s)
        {

            Utils.TP(killer.NetTransform, target.GetTruePosition());

            target.Data.IsDead = true;
            target.SetRealKiller(killer);
            target.SetDeathReason(CustomDeathReason.LossOfHead);
            target.RpcExileV2();
            PlayerState.GetByPlayerId(target.PlayerId)?.SetDead();

            killer.SetKillCooldownV2();
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);

            return false;
        }
        return true;
    }
    public override bool OverrideAbilityButtonSprite(out string buttonName)
    {
        buttonName = "Hangman";
        return true;
    }
}