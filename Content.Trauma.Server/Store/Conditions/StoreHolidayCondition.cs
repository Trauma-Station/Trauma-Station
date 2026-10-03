// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Holiday;
using Content.Shared.Store;

namespace Content.Trauma.Server.Store.Conditions;

/// <summary>
/// Stroe condition that requires a holiday be active.
/// </summary>
public sealed partial class StoreHolidayCondition : ListingCondition
{
    [DataField(required: true)]
    public ProtoId<HolidayPrototype> Holiday;

    private HolidaySystem? _holiday;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        _holiday ??= ent.System<HolidaySystem>();
        return _holiday.IsCurrentlyHoliday(Holiday);
    }
}
