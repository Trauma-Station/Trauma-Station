// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Ash;

public sealed partial class BlindnessImmunitySystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnBeforeBlur(Entity<BlurryVisionImmunityComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Effect == ent.Comp.Key)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnBeforeBlindness(Entity<BlindnessImmunityComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Effect == ent.Comp.Key)
            args.Cancelled = true;
    }
}
