// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.Ghost.Components;

namespace Content.Trauma.Shared.BloodCult.Examine;

public sealed partial class BloodCultExamineSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;

    [SubscribeLocalEvent]
    private void OnCosmicCultExamined(Entity<BloodCultExamineComponent> ent, ref ExaminedEvent args)
    {
        PushExamine(ent.Comp.Text, ref args);
    }

    public void PushExamine(string text, ref ExaminedEvent args)
    {
        if (_cult.IsCultist(args.Examiner) || _ghostQuery.HasComp(args.Examiner))
            args.PushMarkup($"[color=#dc143c]{text}[/color]");
    }
}
