// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Trauma.Shared.ClockworkCult.Components;
using Content.Trauma.Shared.ClockworkCult.Scripture;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.ClockworkCult.Slab;

/// <summary>
/// Handles UI messages, and functions unique to the Clockwork Slab.
///
/// Holding the Clockwork Slab grants passive power generation to the Clockwork Cultist.
/// </summary>
public sealed class ClockworkSlabSystem : EntitySystem
{
    [Dependency] private readonly ScriptureSystem _scripture = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityQuery<ClockworkCultistComponent> _clockworkCultistQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClockworkSlabComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<ClockworkSlabComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ClockworkSlabComponent, GotUnequippedEvent>(OnUnequipped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var eqe = EntityQueryEnumerator<ClockworkSlabComponent>();
        while (eqe.MoveNext(out var uid, out var comp))
        {
            if (comp.Holder is not { } holder)
                continue;

            if (now < comp.NextUpdate)
                continue;

            if (_battery.GetCharge(uid) <= comp.Charge)
                continue;

            // Remove charge from slab, and transfer it to cultist
            _battery.UseCharge(uid, comp.Charge);
            _battery.ChangeCharge(holder, comp.Charge);

            comp.NextUpdate = now + comp.Update;
            Dirty(uid, comp);
        }
    }

    private void OnMapInit(Entity<ClockworkSlabComponent> ent, ref MapInitEvent args)
    {
        // Add all scripture prototypes that are available
        foreach (var scripture in _scripture.AllScriptures)
        {
            _scripture.TryAddScripture(ent.Owner, scripture);
        }

        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.NextUpdate;
        Dirty(ent);
    }

    /// <summary>
    ///  Stores the equipee of the slab, so we can transfer power from the slab to them
    /// </summary>
    private void OnEquipped(Entity<ClockworkSlabComponent> ent, ref GotEquippedEvent args)
    {
        var holder = args.Equipee;
        if (!_clockworkCultistQuery.HasComp(holder))
            return;

        ent.Comp.Holder = holder;
        Dirty(ent);
    }

    /// <summary>
    ///  Un-stores the equipee of the slab, so we stop transferring power to them
    /// </summary>
    private void OnUnequipped(Entity<ClockworkSlabComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.Holder = null;
        Dirty(ent);
    }
}
