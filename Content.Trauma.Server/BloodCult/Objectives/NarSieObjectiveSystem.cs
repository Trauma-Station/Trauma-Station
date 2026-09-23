// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Objectives.Components;
using Content.Trauma.Shared.BloodCult;

namespace Content.Trauma.Server.BloodCult.Objectives;

public sealed partial class NarSieObjectiveSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;

    [SubscribeLocalEvent]
    private void OnGetProgress(Entity<NarSieObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = (_cult.MindGetRule(args.MindId)?.Comp.NarSieSummoned ?? false) ? 1f : 0f;
    }
}
