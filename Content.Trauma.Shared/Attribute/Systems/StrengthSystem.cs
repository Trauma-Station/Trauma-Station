// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Content.Trauma.Common.Cuffs;
using Content.Trauma.Shared.Attribute.Components;

namespace Content.Trauma.Shared.Attribute.Systems;

/// <summary>
/// Handles all strength related bullshit.
/// </summary>
public sealed class StrengthSystem : EntitySystem
{
    [Dependency] private readonly SharedAttributeSystem _attribute = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Actual Gameplay Methods
        SubscribeLocalEvent<AttributeHolderComponent, InstantUncuffEvent>(OnUncuff);
    }

    private void OnUncuff(Entity<AttributeHolderComponent> ent, ref InstantUncuffEvent args)
    {
        var selfEv = new GetStrengthFeatEvent();
        var cuffsEv = new GetStrengthFeatEvent();

        var ev = new OnAttributeOpposedContest(uid => RaiseLocalEvent(uid, ref selfEv), uid => RaiseLocalEvent(uid, ref cuffsEv), () => (selfEv.Mod, cuffsEv.Mod), args.Cuff);

        RaiseLocalEvent(ent, ref ev);
        if (ev.Failed)
            return;

        args.CuffsBroken = true;
    }
}
