// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Heretic.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed partial class EntityEffectContactsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedEntityConditionsSystem _condition = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [Dependency] private EntityQuery<EntityEffectContactsAffectedComponent> _affectedQuery = default!;
    [Dependency] private EntityQuery<EntityEffectContactsComponent> _contactsQuery = default!;

    private static readonly TimeSpan UpdateTime = TimeSpan.FromSeconds(1);
    private TimeSpan _updateTimer;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

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
                RemCompDeferred(uid, affected);
                continue;
            }

            foreach (var (id, ent) in affected.Contacts)
            {
                if (!_contactsQuery.TryComp(ent, out var contacts) ||
                    !_condition.TryConditions(uid, contacts.Conditions, ent))
                {
                    RemoveAffectedId((uid, affected), id);
                    continue;
                }

                _effects.ApplyEffects(uid, contacts.Effects, predicted: false);
            }
        }
    }

    private void RemoveAffectedId(Entity<EntityEffectContactsAffectedComponent> ent, string id)
    {
        ent.Comp.Contacts.Remove(id);
        if (ent.Comp.Contacts.Count == 0)
            RemCompDeferred(ent, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnEntityExit(Entity<EntityEffectContactsComponent> ent, ref EndCollideEvent args)
    {
        if (!_affectedQuery.TryComp(args.OtherEntity, out var comp) ||
            comp.Contacts.GetValueOrDefault(ent.Comp.Id) != ent.Owner)
            return;

        RemoveAffectedId((args.OtherEntity, comp), ent.Comp.Id);
    }

    [SubscribeLocalEvent]
    private void OnEntityEnter(Entity<EntityEffectContactsComponent> ent, ref StartCollideEvent args)
    {
        if (!_condition.TryConditions(args.OtherEntity, ent.Comp.Conditions, ent))
            return;

        var comp = EnsureComp<EntityEffectContactsAffectedComponent>(args.OtherEntity);
        comp.Contacts[ent.Comp.Id] = ent;
    }
}
