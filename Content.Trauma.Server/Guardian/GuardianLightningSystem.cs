// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Lightning;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Guardian.Components;
using Content.Shared.Physics;
using Content.Trauma.Shared.Genetics.Mutations;
using Content.Trauma.Shared.Guardian;
using Content.Trauma.Shared.Guardian.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Guardian;

/// <summary>
/// Server-side half of the lightning guardian: runs the passive arcs around the manifested
/// guardian, applies the targeted bolt (damage + stun + chaining) and keeps the host's
/// electricity resistance gene and Shock modifier set alive while the guardian exists.
/// </summary>
public sealed partial class GuardianLightningSystem : SharedGuardianLightningSystem
{
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private MutationSystem _mutation = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GuardianLightningComponent, ComponentShutdown>(OnGuardianShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GuardianLightningComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            GuardianComponent? guardian = null;
            if (!Resolve(uid, ref guardian) || guardian.Host is not { } host || Deleted(host))
                continue;

            KeepHostProtection(uid, comp, host);

            if (!guardian.GuardianLoose || _timing.CurTime < comp.NextPassive)
                continue;

            comp.NextPassive = _timing.CurTime + comp.PassiveTick;

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
    /// resistance gene and insulated enemies are actually protected.
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

            Entity<PreventCollideComponent, ElectrifiedComponent> ent = (beam, preventCollide, electrified);
            Dirty(ent);
        };
    }

    /// <summary>
    /// Grants the host the resistance gene and Shock modifier set, moving them over when the
    /// host changes (e.g. the host is polymorphed).
    /// </summary>
    private void KeepHostProtection(EntityUid guardian, GuardianLightningComponent comp, EntityUid host)
    {
        if (comp.ProtectedHost == host)
            return;

        if (comp.ProtectedHost is { } oldHost && oldHost != host)
            RemoveHostProtection(oldHost, comp);

        _mutation.AddMutation(host, comp.GeneId, automatic: true);

        if (comp.HostDamageModifierSet != null)
            _damage.SetDamageModifierSetId(host, comp.HostDamageModifierSet);

        comp.ProtectedHost = host;
    }

    private void OnGuardianShutdown(Entity<GuardianLightningComponent> ent, ref ComponentShutdown args)
    {
        var host = ent.Comp.ProtectedHost ??
                   (TryComp<GuardianComponent>(ent, out var guardian) ? guardian.Host : null);

        if (host is { } h) // Inshallah
            RemoveHostProtection(h, ent.Comp);
    }

    private void RemoveHostProtection(EntityUid host, GuardianLightningComponent comp)
    {
        _mutation.RemoveMutation(host, comp.GeneId, automatic: true);

        if (comp.HostDamageModifierSet != null)
            _damage.SetDamageModifierSetId(host, null);
    }
}
