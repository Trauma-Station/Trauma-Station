// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Examine;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Trauma.Common.CardboardBox;
using Content.Trauma.Shared.Mobs;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.CardboardBox;

public sealed class JustABoxSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    /// <summary>
    /// Flags to look for witnesses.
    /// Does not have contained, so being in the box won't trigger an alert.
    /// </summary>
    public const LookupFlags Flags = LookupFlags.Dynamic | LookupFlags.Static;

    private HashSet<Entity<AwakeMobComponent>> _witnesses = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JustABoxComponent, BoxAlertAttemptEvent>(OnAlertAttempt);
        SubscribeLocalEvent<JustABoxComponent, BoxAlertedEvent>(OnAlerted);

        SubscribeLocalEvent<DisabledBoxComponent, BoxAlertAttemptEvent>(OnDisabledAlertAttempt);
        SubscribeLocalEvent<DisabledBoxComponent, BoxStealthAttemptEvent>(OnDisabledStealthAttempt);
        SubscribeLocalEvent<DisabledBoxComponent, ComponentRemove>(OnDisabledRemove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DisabledBoxComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextStealth > now)
                continue;

            RemCompDeferred(uid, comp);
        }
    }

    private void OnAlertAttempt(Entity<JustABoxComponent> ent, ref BoxAlertAttemptEvent args)
    {
        // no alert if you open it unseen
        args.Cancelled |= WasUnseen(ent);
    }

    private void OnAlerted(Entity<JustABoxComponent> ent, ref BoxAlertedEvent args)
    {
        var disabled = EnsureComp<DisabledBoxComponent>(ent);
        disabled.NextStealth = _timing.CurTime + ent.Comp.StealthCooldown;
        Dirty(ent, disabled);
    }

    private void OnDisabledAlertAttempt(Entity<DisabledBoxComponent> ent, ref BoxAlertAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnDisabledStealthAttempt(Entity<DisabledBoxComponent> ent, ref BoxStealthAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnDisabledRemove(Entity<DisabledBoxComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent) || // don't care
            _entityStorage.IsOpen(ent.Owner) || // it will re-stealth when closed later
            !TryComp<StealthComponent>(ent, out var stealth))
            return;

        _stealth.SetVisibility(ent.Owner, stealth.MaxVisibility, stealth);
        _stealth.SetEnabled(ent.Owner, true, stealth);
    }

    public bool WasUnseen(Entity<JustABoxComponent> ent)
    {
        var coords = Transform(ent).Coordinates;
        var mapCoords = _transform.ToMapCoordinates(coords);
        var range = ent.Comp.WitnessRange;
        _witnesses.Clear();
        _lookup.GetEntitiesInRange(coords, range, _witnesses, Flags);
        foreach (var witness in _witnesses)
        {
            var pos = _transform.GetMapCoordinates(witness.Owner);
            if (_examine.InRangeUnOccluded(mapCoords, pos, range, predicate: null))
                return false; // !
        }

        return true;
    }
}
