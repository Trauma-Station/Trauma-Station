// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Systems;
using Content.Shared.Inventory.Events;
using Content.Trauma.Shared.ClockworkCult.Components;
using Content.Trauma.Shared.ClockworkCult.Scripture;
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
    [Dependency] private readonly SharedChargesSystem _charges = default!;
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

        // TODO: Should be seperate component
        // TODO: Should have an active variant so it doesn't run for 4noraisin
        var eqe = EntityQueryEnumerator<ClockworkSlabComponent>();
        while (eqe.MoveNext(out var uid, out var comp))
        {
            if (comp.Holder is not { } holder)
                continue;

            if (now < comp.NextUpdate)
                continue;

            if (_charges.GetCurrentCharges(uid) <= comp.Charge)
                continue;

            // Remove charge from slab, and transfer it to cultist
            _charges.TryUseCharges(uid, comp.Charge);
            _charges.AddCharges(holder, comp.Charge);

            comp.NextUpdate = now + comp.Update;
            Dirty(uid, comp);
        }
    }

    private void OnMapInit(Entity<ClockworkSlabComponent> ent, ref MapInitEvent args)
    {
        // Add all scripture prototypes that are available
        // TODO: Scriptures shouldn't be stored like that on the slab, rather one entity should hold them (not gamerule cuz its in server)
        // TODO: and said entity should sync scriptures between all slabs (entity should be smth in-game, not nullspace, not destroyable!)
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
