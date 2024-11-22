// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Trauma.Shared.BloodCult.Runes.Revive;

public abstract partial class CultRuneReviveSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] protected ISharedPlayerManager Player = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] protected SharedMindSystem Mind = default!;

    [SubscribeLocalEvent]
    private void OnReviveRuneInvoked(Entity<CultRuneReviveComponent> ent, ref RuneInvokeEvent args)
    {
        if (EnsureChargesProvider(ent) is not {} provider || provider.Comp.Charges <= 0)
        {
            args.Popup = Loc.GetString("cult-revive-rune-no-charges");
            return;
        }

        var targets = _cult.GetTargetsNearRune(ent, ent.Comp.ReviveRange);
        targets.RemoveWhere(uid =>
            !HasComp<DamageableComponent>(uid) ||
            !HasComp<MobThresholdsComponent>(uid) ||
            !HasComp<MobStateComponent>(uid) ||
            _mob.IsAlive(uid));

        if (targets.Count == 0)
        {
            args.Popup = Loc.GetString("cult-rune-no-targets");
            return;
        }

        var victim = targets.First();

        Revive(victim, args.User, ent);
        args.Handled = true;
    }

    public void AddCharges(EntityUid ent, int charges)
    {
        if (EnsureChargesProvider(ent) is not {} provider)
            return;

        provider.Comp.Charges += charges;
        Dirty(provider);
    }

    private void Revive(EntityUid target, EntityUid user, Entity<CultRuneReviveComponent> rune)
    {
        if (EnsureChargesProvider(rune) is not {} provider)
            return;

        provider.Comp.Charges--;
        Dirty(provider);

        var deadThreshold = _threshold.GetThresholdForState(target, MobState.Dead);
        _damage.TryChangeDamage(target, rune.Comp.Healing);

        if (_damage.GetTotalDamage(target) > deadThreshold)
            return;

        // yet another system bypassing Unrevivable etc :face_holding_back_tears:
        _mob.ChangeMobState(target, MobState.Critical, origin: user);
        if (!Mind.TryGetMind(target, out var mindId, out var mind) ||
            mind.CurrentEntity == target || // don't need a return to body prompt if you are in it already
            !Player.TryGetSessionById(mind.UserId, out var session))
            return;

        // notify them they're being revived.
        OpenReturnEui((mindId, mind), session);
    }

    private Entity<ReviveRuneChargesProviderComponent>? EnsureChargesProvider(EntityUid ent)
        // TODO: why the FUCK is this on the map and not gamerule or something ?!
        => Transform(ent).MapUid is {} map
            ? (map, EnsureComp<ReviveRuneChargesProviderComponent>(map))
            : null;

    protected virtual void OpenReturnEui(Entity<MindComponent> mind, ICommonSession session)
    {
    }
}
