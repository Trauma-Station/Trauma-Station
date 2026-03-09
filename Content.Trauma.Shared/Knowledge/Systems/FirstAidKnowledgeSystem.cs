// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Content.Trauma.Common.Chemistry;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// Handles first aid knowledge interactions.
/// </summary>
public sealed class FirstAidKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, UserModifyInjectTimeEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<InjectTimeKnowledgeComponent, UserModifyInjectTimeEvent>(OnModifyInjectTime);
    }

    private void OnModifyInjectTime(Entity<InjectTimeKnowledgeComponent> ent, ref UserModifyInjectTimeEvent args)
    {
        var level = _knowledge.GetLevel(ent.Owner);
        if (args.Delay > TimeSpan.Zero)
        {
            args.Delay *= ent.Comp.Curve.GetCurve(level);
            return;
        }

        if (level >= ent.Comp.MinSkill)
            return;

        _popup.PopupClient(Loc.GetString("knowledge-inject-low-skill"), args.User, args.User);
        args.Delay += ent.Comp.MinTime;
    }
}
