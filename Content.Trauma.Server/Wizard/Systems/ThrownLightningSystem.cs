// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Effects;
using Content.Server.Electrocution;
using Content.Shared.Throwing;
using Content.Trauma.Common.Wizard;
using Content.Trauma.Common.Wizard.Projectile;
using Content.Trauma.Server.Wizard.Components;

namespace Content.Trauma.Server.Wizard.Systems;

public sealed class ThrownLightningSystem : EntitySystem
{
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly SpellsSystem _spells = default!;
    [Dependency] private readonly SparksSystem _sparks = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrownLightningComponent, ThrowDoHitEvent>(OnHit);
        SubscribeLocalEvent<ThrownLightningComponent, ThrownEvent>(OnThrown);
        SubscribeLocalEvent<ThrownLightningComponent, StopThrowEvent>(OnStopThrow);
    }

    private void OnStopThrow(Entity<ThrownLightningComponent> ent, ref StopThrowEvent args)
    {
        if (Deleting(ent))
            return;

        if (!TryComp(ent, out TrailComponent? trail))
            return;

        trail.ParticleAmount = 0;
        Dirty(ent.Owner, trail);
    }

    private void OnThrown(Entity<ThrownLightningComponent> ent, ref ThrownEvent args)
    {
        if (TryComp(ent, out TrailComponent? trail))
        {
            trail.ParticleAmount = 1;
            Dirty(ent.Owner, trail);
        }

        if (args.User == null)
            return;

        var speech = ent.Comp.Speech == null ? string.Empty : Loc.GetString(ent.Comp.Speech);
        _spells.SpeakSpell(args.User.Value, args.User.Value, speech, MagicSchool.Conjuration);
    }

    private void OnHit(Entity<ThrownLightningComponent> ent, ref ThrowDoHitEvent args)
    {
        if (Deleting(ent))
            return;

        if (_electrocution.TryDoElectrocution(args.Target, ent, 1, ent.Comp.StunTime, true, 1f, ignoreInsulation: true))
            _sparks.DoSparks(Transform(ent).Coordinates);
    }

    private bool Deleting(EntityUid ent)
    {
        return EntityManager.IsQueuedForDeletion(ent) || TerminatingOrDeleted(ent);
    }
}
