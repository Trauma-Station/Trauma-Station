// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffect;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Ash;

public sealed class BlindnessImmunitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemporaryBlindnessImmunityComponent, BeforeOldStatusEffectAddedEvent>(OnBeforeBlindness);
        SubscribeLocalEvent<BlurryVisionImmunityComponent, BeforeOldStatusEffectAddedEvent>(OnBeforeBlur);
    }

    private void OnBeforeBlur(Entity<BlurryVisionImmunityComponent> ent, ref BeforeOldStatusEffectAddedEvent args)
    {
        if (args.EffectKey == ent.Comp.Key)
            args.Cancelled = true;
    }

    private void OnBeforeBlindness(Entity<TemporaryBlindnessImmunityComponent> ent,
        ref BeforeOldStatusEffectAddedEvent args)
    {
        if (args.EffectKey == ent.Comp.Key)
            args.Cancelled = true;
    }
}
