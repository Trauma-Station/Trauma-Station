using Content.Trauma.Client.Knowledge.UI;
using Content.Trauma.Shared.Knowledge;
using Content.Trauma.Shared.Knowledge.Skills.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Knowledge;

public sealed partial class KnowledgeGrantSystem : SharedKnowledgeGrantSystem
{
    [Dependency] private IGameTiming _timing = default!;

    protected override void OnActivate(Entity<KnowledgeGrantOnUseComponent> ent, EntityUid user, BoundUserInterface window)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (window is not GymBoundUserInterface gymWindow)
            return;

        gymWindow.UpdateTime(ent.Comp.IdealRhythmInterval);
        HandleRep(ent, user, gymWindow.HandleRepInput());
    }
}
