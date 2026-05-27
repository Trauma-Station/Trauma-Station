// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Silicons.StationAi;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class KillAi : EntityEffectBase<KillAi>;

public sealed class KillAiEffectSystem : EntityEffectSystem<StationAiHolderComponent, KillAi>
{
    protected override void Effect(Entity<StationAiHolderComponent> entity, ref EntityEffectEvent<KillAi> args)
    {
        PredictedQueueDel(entity.Comp.Slot.ContainerSlot?.ContainedEntity);
    }
}
