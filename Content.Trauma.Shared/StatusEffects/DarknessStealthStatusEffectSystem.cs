// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.LightDetection.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stealth;

namespace Content.Trauma.Shared.StatusEffects;

public sealed class DarknessStealthStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, LightLevelUpdated>(_status.RelayEvent);

        SubscribeLocalEvent<DarknessStealthStatusEffectComponent, StatusEffectRelayedEvent<LightLevelUpdated>>(OnLightUpdated);

        SubscribeLocalEvent<DarknessStealthStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<DarknessStealthStatusEffectComponent, StatusEffectRemovedEvent>(OnRemove);
    }

    private void OnLightUpdated(Entity<DarknessStealthStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LightLevelUpdated> args)
    {
        var newLevel = args.Args.NewLightLevel;
        var target = ent.Comp.StatusOwner;
        if (target is not { } statusOwner)
            return;

        // We are in darkness here
        if (newLevel < ent.Comp.TriggerAt)
        {
            _stealth.SetVisibility(statusOwner, ent.Comp.Visibility);
            return;
        }

        _stealth.SetVisibility(statusOwner, 1f);
    }

    private void OnApplied(Entity<DarknessStealthStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.StatusOwner = args.Target;
        Dirty(ent);
    }

    private void OnRemove(Entity<DarknessStealthStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _stealth.SetVisibility(args.Target, 1f);

        ent.Comp.StatusOwner = null;
        Dirty(ent);
    }
}
