// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;

namespace Content.Lavaland.Shared.Pressure;

public abstract class SharedPressureEfficiencyChangeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PressureDamageChangeComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<PressureArmorChangeComponent, ExaminedEvent>(OnArmorExamined);
    }

    private void OnExamined(Entity<PressureDamageChangeComponent> ent, ref ExaminedEvent args)
    {
        var localeKey = "lavaland-examine-pressure-";
        localeKey += ent.Comp.ApplyWhenInRange ? "in-range-" : "out-range-";

        ExamineHelper(Math.Round(ent.Comp.LowerBound),
            Math.Round(ent.Comp.UpperBound),
            Math.Round(ent.Comp.AppliedModifier, 2),
            localeKey,
            ref args);
    }

    private void OnArmorExamined(Entity<PressureArmorChangeComponent> ent, ref ExaminedEvent args)
    {
        var localeKey = "lavaland-examine-pressure-armor-";
        localeKey += ent.Comp.ApplyWhenInRange ? "in-range-" : "out-range-";

        ExamineHelper(Math.Round(ent.Comp.LowerBound),
            Math.Round(ent.Comp.UpperBound),
            Math.Round(ent.Comp.ExtraPenetrationModifier * 100),
            localeKey,
            ref args);
    }

    private void ExamineHelper(double min, double max, double modifier, string localeKey, ref ExaminedEvent args)
    {
        localeKey += modifier > 1f ? "debuff" : "buff";
        modifier = Math.Abs(modifier);
        args.PushMarkup(Loc.GetString(localeKey, ("min", min), ("max", max), ("modifier", modifier)));
    }
}
