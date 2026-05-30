// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
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
    [Dependency] private SharedHandsSystem _hands = default!;

    private EntityQuery<CuffableComponent> _cuffQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _cuffQuery = GetEntityQuery<CuffableComponent>();

        SubscribeLocalEvent<CuffSpawnerComponent, UserActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<CuffSpawnerComponent, CuffSpawnerDoAfterEvent>(OnCuff);
        SubscribeLocalEvent<CuffSpawnerComponent, GotEmaggedEvent>(OnEmag);
        SubscribeLocalEvent<CuffSpawnerComponent, DoAfterAttemptEvent<CuffSpawnerDoAfterEvent>>(OnWait);
    }

    private void OnInteract(Entity<CuffSpawnerComponent> beepsky, ref UserActivateInWorldEvent args)
    {
        if (!CheckCuffs(beepsky!, args.Target, true))
            return;

        var target = args.Target;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(2), new CuffSpawnerDoAfterEvent(), args.User, args.Target)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
        });
    }

    private void OnCuff(Entity<CuffSpawnerComponent> ent, ref CuffSpawnerDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target is { } target)
            TryCuff(ent.Owner, target);
    }

    private void OnEmag(Entity<CuffSpawnerComponent> ent, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }

    private void OnWait(Entity<CuffSpawnerComponent> ent, ref DoAfterAttemptEvent<CuffSpawnerDoAfterEvent> args)
    {
        if (args.Event.Target is not { } target || !CheckCuffs(args.Event.User, target))
            args.Cancel();
    }

    /// <summary>
    /// Checks if the target can be cuffed.
    /// </summary>
    public bool CheckCuffs(Entity<CuffSpawnerComponent?> beepsky, EntityUid target, bool manual = false)
    {
        if (!Resolve(beepsky, ref beepsky.Comp, false))
            return false;

        if (!_cuffQuery.TryComp(target, out var cuffed))
            return false;

        if (_cuff.IsCuffed((target, cuffed)))
            return false;

        if (_hands.CountFreeHands(target) <= 0)
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
