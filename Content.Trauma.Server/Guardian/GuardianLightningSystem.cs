// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Lightning;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Guardian.Components;
using Content.Shared.Physics;
using Content.Trauma.Shared.Guardian;
using Content.Trauma.Shared.Guardian.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Guardian;

/// <summary>
/// Server-side half of the lightning guardian: runs the passive arcs around the manifested
/// guardian, applies the targeted bolt (damage + stun + chaining) and keeps the host's
/// electricity resistance (an <see cref="InsulatedComponent"/> and Shock modifier set) alive while the guardian exists.
/// </summary>
public sealed partial class GuardianLightningSystem : SharedGuardianLightningSystem
{
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<GuardianLightningComponent, GuardianComponent>();
        while (query.MoveNext(out var uid, out var comp, out var guardian))
        {
            if (guardian.Host is not { } host || Deleted(host))
                continue;

            KeepHostProtection((uid, comp), host);

            if (!guardian.GuardianLoose || curTime < comp.NextPassive)
                continue;

            comp.NextPassive = curTime + comp.PassiveTick;

            var action = MakeBoltAction(uid, comp.PassiveDamage, 0f);
            _lightning.ShootRandomLightnings(uid,
                comp.PassiveRange,
                comp.PassiveBoltCount,
                comp.PassiveProto,
                comp.PassiveArcDepth,
                triggerLightningEvents: false,
                ignoredEntity: uid,
                beamAction: action);
        }
    }

    public override void ShootLightning(EntityUid performer, EntityUid target, EntProtoId lightningPrototype, float damage)
    {
        var comp = Comp<GuardianLightningComponent>(performer);

        var action = MakeBoltAction(performer, damage, comp.BoltStunTime);
        _lightning.ShootLightning(performer, target, lightningPrototype, triggerLightningEvents: false, beamAction: action);

        // The bolt chains from the primary target to nearby enemies, keeping the stun per hop.
        _lightning.ShootRandomLightnings(target,
            comp.BoltChainRange,
            boltCount: 1,
            lightningPrototype,
            comp.BoltChainDepth,
            triggerLightningEvents: false,
            ignoredEntity: performer,
            beamAction: action);
    }

    /// <summary>
    /// Configures a lightning beam so it shocks anything it touches (except the guardian) with
    /// the given damage and optional paralyze time. Insulation is respected so the host's
    /// <see cref="InsulatedComponent"/> and insulated enemies are actually protected.
    /// </summary>
    private Action<EntityUid> MakeBoltAction(EntityUid performer, float damage, float stunTimeSeconds)
    {
        return beam =>
        {
            var preventCollide = EnsureComp<PreventCollideComponent>(beam);
            preventCollide.Uid = performer;

            var electrified = EnsureComp<ElectrifiedComponent>(beam);
            electrified.IgnoredEntity = performer;
            electrified.IgnoreInsulation = true;
            electrified.RequirePower = false;
            electrified.ShockDamage = damage;
            electrified.ShockTime = stunTimeSeconds;

            Dirty<PreventCollideComponent, ElectrifiedComponent>((beam, preventCollide, electrified));
        };
    }

    /// <summary>
    /// Grants the host the resistance gene and Shock modifier set, moving them over when the
    /// host changes (e.g. the host is polymorphed).
    /// </summary>
    private void KeepHostProtection(Entity<GuardianLightningComponent> guardian, EntityUid host)
    {
        if (guardian.Comp.ProtectedHost == host)
            return;

        if (guardian.Comp.ProtectedHost is { } oldHost && oldHost != host)
            RemoveHostProtection(oldHost, guardian.Comp);

        EnsureComp<InsulatedComponent>(host);

        if (guardian.Comp.HostDamageModifierSet != null)
            _damage.SetDamageModifierSetId(host, guardian.Comp.HostDamageModifierSet);

        guardian.Comp.ProtectedHost = host;
    }

    [SubscribeLocalEvent]
    private void OnGuardianShutdown(Entity<GuardianLightningComponent> ent, ref ComponentShutdown args)
    {
        var host = ent.Comp.ProtectedHost ?? CompOrNull<GuardianComponent>(ent)?.Host;

        if (host is { } h) // Inshallah
            RemoveHostProtection(h, ent.Comp);
    }

    private void RemoveHostProtection(EntityUid host, GuardianLightningComponent comp)
    {
        RemComp<InsulatedComponent>(host);

        if (comp.HostDamageModifierSet != null)
            _damage.SetDamageModifierSetId(host, null);
    }
}
