// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Heretic.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed partial class EntityEffectContactsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedEntityConditionsSystem _condition = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;
    [Dependency] private EntityQuery<EntityEffectContactsAffectedComponent> _affectedQuery = default!;
    [Dependency] private EntityQuery<EntityEffectContactsComponent> _contactsQuery = default!;

    private readonly HashSet<EntityUid> _toUpdate = new();
    private readonly HashSet<EntityUid> _toRemove = new();

    private static readonly TimeSpan UpdateTime = TimeSpan.FromSeconds(1);
    private TimeSpan _updateTimer;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _toRemove.Clear();

        foreach (var ent in _toUpdate)
        {
            Refresh(ent);
        }

        foreach (var ent in _toRemove)
        {
            RemComp<EntityEffectContactsAffectedComponent>(ent);
        }

        _toUpdate.Clear();

        UpdateAffected();
    }

    private void UpdateAffected()
    {
        if (_net.IsClient)
            return;

        var now = _timing.CurTime;

        if (now < _updateTimer)
            return;

        _updateTimer = now + UpdateTime;

        var query = EntityQueryEnumerator<EntityEffectContactsAffectedComponent>();
        while (query.MoveNext(out var uid, out var affected))
        {
            if (affected.Contacts.Count == 0)
            {
                _toUpdate.Add(uid);
                continue;
            }

            foreach (var ent in affected.Contacts.Values)
            {
                if (!_contactsQuery.TryComp(ent, out var contacts) ||
                    !_condition.TryConditions(uid, contacts.Conditions, ent))
                {
                    _toUpdate.Add(uid);
                    continue;
                }

                _effects.ApplyEffects(uid, contacts.Effects, predicted: false);
            }
        }
    }

    private void Refresh(EntityUid uid)
    {
        if (!_physicsQuery.TryComp(uid, out var body) || !_affectedQuery.TryComp(uid, out var affected))
            return;

        var entries = 0;
        foreach (var ent in _physics.GetContactingEntities(uid, body))
        {
            if (!_contactsQuery.TryComp(ent, out var contacts))
                continue;

            affected.Contacts.TryAdd(contacts.Id, ent);
            entries++;
        }

        if (entries == 0)
            _toRemove.Add(uid);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<EntityEffectContactsComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp(ent, out PhysicsComponent? phys))
            return;

        _toUpdate.UnionWith(_physics.GetContactingEntities(ent, phys));
    }

    [SubscribeLocalEvent]
    private void OnEntityExit(Entity<EntityEffectContactsComponent> ent, ref EndCollideEvent args)
    {
        if (!_affectedQuery.TryComp(args.OtherEntity, out var comp) ||
            comp.Contacts.GetValueOrDefault(ent.Comp.Id) != ent.Owner)
            return;

        _toUpdate.Add(args.OtherEntity);
    }

    [SubscribeLocalEvent]
    private void OnEntityEnter(Entity<EntityEffectContactsComponent> ent, ref StartCollideEvent args)
    {
        if (!_condition.TryConditions(args.OtherEntity, ent.Comp.Conditions, ent))
            return;

        var comp = EnsureComp<EntityEffectContactsAffectedComponent>(args.OtherEntity);
        comp.Contacts[ent.Comp.Id] = ent;

        _toUpdate.Add(args.OtherEntity);
    }
}
