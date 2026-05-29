// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.LightDetection.Components;
using Content.Goobstation.Shared.LightDetection.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;

namespace Content.Trauma.Shared.StatusEffects;

public sealed partial class DarknessStealthStatusEffectSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedStealthSystem _stealth = default!;

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
        var target = args.Container.Owner;

        // We are in darkness here
        if (newLevel < ent.Comp.TriggerAt)
        {
            _stealth.SetVisibility(target, ent.Comp.Visibility);
            return;
        }

        _stealth.SetVisibility(target, 1f);
    }

    private void OnApplied(Entity<DarknessStealthStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        var target = args.Target;
        EnsureComp<LightDetectionComponent>(target);
        EnsureComp<StealthComponent>(target);
    }

    private void OnRemove(Entity<DarknessStealthStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        var target = args.Target;
        RemCompDeferred<LightDetectionComponent>(target);
        RemCompDeferred<StealthComponent>(target);
    }
}
