// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Electrocution;
using Content.Shared.Throwing;
using Content.Trauma.Common.Wizard;
using Content.Trauma.Common.Wizard.Projectile;
using Content.Trauma.Server.Wizard.Components;
using Content.Trauma.Shared.Effects;

namespace Content.Trauma.Server.Wizard.Systems;

public sealed partial class ThrownLightningSystem : EntitySystem
{
    [Dependency] private ElectrocutionSystem _electrocution = default!;
    [Dependency] private SpellsSystem _spells = default!;
    [Dependency] private SparksSystem _sparks = default!;

    [SubscribeLocalEvent]
    private void OnStopThrow(Entity<ThrownLightningComponent> ent, ref StopThrowEvent args)
    {
        if (Deleting(ent))
            return;

        if (!TryComp(ent, out TrailComponent? trail))
            return;

        trail.ParticleAmount = 0;
        Dirty(ent.Owner, trail);
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnHit(Entity<ThrownLightningComponent> ent, ref ThrowDoHitEvent args)
    {
        if (Deleting(ent))
            return;

        if (_electrocution.TryDoElectrocution(args.Target, ent, 1, ent.Comp.StunTime, true, 1f, ignoreInsulation: true))
            _sparks.DoSparks(ent);
    }

    private bool Deleting(EntityUid ent)
        => EntityManager.IsQueuedForDeletion(ent) || TerminatingOrDeleted(ent);
}
