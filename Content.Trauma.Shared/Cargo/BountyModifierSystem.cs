// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Cargo;

namespace Content.Trauma.Shared.Cargo;

public sealed class BountyModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BountyModifierComponent, ModifyBountyRewardEvent>(OnModifyBountyReward);
    }

    private void OnModifyBountyReward(Entity<BountyModifierComponent> ent, ref ModifyBountyRewardEvent args)
    {
        args.Reward = (int) (ent.Comp.Modifier * args.Reward);
    }
}
