// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Surgery.Components;
using Content.Shared.Body;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Surgery;

/// <summary>
/// Methods for component effects.
/// </summary>
public sealed partial class SurgeryEffectsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BleedingWoundComponent, ComponentStartup>(OnBleedBegin);
        SubscribeLocalEvent<BleedingWoundComponent, ComponentShutdown>(OnBleedStop);

        SubscribeLocalEvent<BreakBoneComponent, ComponentStartup>(OnBoneBreak);
        SubscribeLocalEvent<BreakBoneComponent, ComponentShutdown>(OnBoneMend);

        SubscribeLocalEvent<BleedingWoundComponent, GetSurgeryExamine>(OnExamineBleedingWound);
        SubscribeLocalEvent<BreakBoneComponent, GetSurgeryExamine>(OnExamineBreakBone);
        SubscribeLocalEvent<RetractedSkinComponent, GetSurgeryExamine>(OnExamineSkinComponent);
        SubscribeLocalEvent<OpenIncisionComponent, GetSurgeryExamine>(OnExamineOpenIncision);

        SubscribeLocalEvent<BodyComponent, ExaminedEvent>(OnExamineBody);
    }

    private void OnBleedBegin(Entity<BleedingWoundComponent> ent, ref ComponentStartup args)
    {

    }

    private void OnBleedStop(Entity<BleedingWoundComponent> ent, ref ComponentShutdown args)
    {

    }

    private void OnBoneBreak(Entity<BreakBoneComponent> ent, ref ComponentStartup args)
    {

    }

    private void OnBoneMend(Entity<BreakBoneComponent> ent, ref ComponentShutdown args)
    {

    }

    private void OnExamineBleedingWound(Entity<BleedingWoundComponent> ent, ref GetSurgeryExamine args)
    {
        args.Text.Add($"{Name(args.Body)}'s {Name(ent.Owner)} has open blood vessels.");
    }

    private void OnExamineBreakBone(Entity<BreakBoneComponent> ent, ref GetSurgeryExamine args)
    {
        args.Text.Add($"{Name(args.Body)}'s {Name(ent.Owner)} has bones cut open.");
    }

    private void OnExamineSkinComponent(Entity<RetractedSkinComponent> ent, ref GetSurgeryExamine args)
    {
        args.Text.Add($"{Name(args.Body)}'s {Name(ent.Owner)} has skin peeled back.");
    }

    private void OnExamineOpenIncision(Entity<OpenIncisionComponent> ent, ref GetSurgeryExamine args)
    {
        args.Text.Add($"{Name(args.Body)}'s {Name(ent.Owner)} has an open incision.");
    }

    private void OnExamineBody(Entity<BodyComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || ent.Comp.Organs?.ContainedEntities is not { } ents)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        var ev = new GetSurgeryExamine(ent.Owner, new());
        foreach (var part in ents)
        {
            ev.Text.Clear();
            RaiseLocalEvent(part, ref ev);

            foreach (var text in ev.Text)
            {
                args.PushMarkup(text);
            }
        }
    }
}

[ByRefEvent]
public record struct GetSurgeryExamine(EntityUid Body, List<string> Text);
