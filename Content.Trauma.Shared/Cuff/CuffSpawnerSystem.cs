// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;

namespace Content.Trauma.Shared.Cuff;

/// <summary>
/// Handles beepsky and provides api.
/// </summary>
public sealed partial class CuffSpawnerSystem : EntitySystem
{
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedCuffableSystem _cuff = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CuffSpawnerComponent, UserActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<CuffSpawnerComponent, CuffSpawnerDoAfterEvent>(OnCuff);
    }

    private void OnInteract(Entity<CuffSpawnerComponent> beepsky, ref UserActivateInWorldEvent args)
    {
        if (!CheckCuffs(beepsky!, args.Target, true))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, 2f, new CuffSpawnerDoAfterEvent(), args.User, args.Target)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
        });
    }

    private void OnCuff(EntityUid uid, CuffSpawnerComponent comp, ref CuffSpawnerDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target is { } target)
            TryCuff(uid, target);
    }

    /// <summary>
    /// Checks if the target can be cuffed.
    /// </summary>
    public bool CheckCuffs(Entity<CuffSpawnerComponent?> beepsky, EntityUid target, bool manual = false)
    {
        if (!Resolve(beepsky, ref beepsky.Comp, false))
            return false;

        if (!TryComp<CuffableComponent>(target, out var cuffed))
            return false;

        if (_cuff.IsCuffed((target, cuffed)))
            return false;

        if (!TryComp<HandsComponent>(target, out var hands))
            return false;

        if (hands.Count <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// Tries to cuff target.
    /// </summary>
    public bool TryCuff(Entity<CuffSpawnerComponent?> beepsky, EntityUid target)
    {
        if (!Resolve(beepsky, ref beepsky.Comp, false))
            return false;

        if (!CheckCuffs(beepsky, target))
            return false;

        if (!_interaction.InRangeUnobstructed(beepsky.Owner, target))
            return false;

        var handcuffs = PredictedSpawnAtPosition(beepsky.Comp.HandcuffId, beepsky.Owner.ToCoordinates());
        _cuff.TryAddNewCuffs(target, beepsky.Owner, handcuffs);

        return true;
    }
}

[Serializable, NetSerializable]
public sealed partial class CuffSpawnerDoAfterEvent : SimpleDoAfterEvent;
