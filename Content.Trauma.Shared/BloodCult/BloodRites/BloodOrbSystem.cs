// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Trauma.Shared.BloodCult.Spells;

namespace Content.Trauma.Shared.BloodCult.BloodRites;

public sealed partial class BloodOrbSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;

    [SubscribeLocalEvent]
    private void OnExamine(Entity<BloodOrbComponent> ent, ref ExaminedEvent args)
    {
        if (!_cult.IsCultist(args.Examiner))
            return;

        args.PushMarkup($"It has [color=darkred]{ent.Comp.StoredBlood}u of blood[/color] stored.");
        args.PushMarkup($"Can be used to recharge [color=red]Blood Rites[/color].");
    }

    [SubscribeLocalEvent]
    private void OnRitesUsed(Entity<BloodOrbComponent> ent, ref BloodRitesItemCreatedEvent args)
    {
        ent.Comp.StoredBlood = args.Cost + args.RemainingBlood;
        Dirty(ent);
    }
}
