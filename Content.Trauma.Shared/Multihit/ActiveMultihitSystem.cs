// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Trauma.Shared.Multihit;

public abstract partial class SharedActiveMultihitSystem : EntitySystem
{
    [Dependency] protected EntityQuery<ActiveMultihitComponent> ActiveQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveMultihitComponent, MeleeHitEvent>(OnHit, after: new[] { typeof(MultihitSystem) });

        SubscribeAllEvent<UpdateMultihitDirectionEvent>(OnUpdateDirection);
    }

    private void OnUpdateDirection(UpdateMultihitDirectionEvent args)
    {
        if (!TryGetEntity(args.Ent, out var ent) || !ActiveQuery.TryComp(ent.Value, out var active))
            return;

        if (active.LastAttack?.Direction != null)
            active.LastAttack.Direction = args.Direction;

        foreach (var queued in active.QueuedAttacks)
        {
            if (queued.Direction == null)
                continue;

            queued.Direction = args.Direction;
        }

        Dirty(ent.Value, active);
}

    private void OnHit(Entity<ActiveMultihitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (Math.Abs(ent.Comp.NextDamageMultiplier - 1f) < 0.01f)
            return;

        var modifierSet = new DamageModifierSet
        {
            Coefficients = args.BaseDamage.DamageDict
                .Select(x => new KeyValuePair<string, float>(x.Key, ent.Comp.NextDamageMultiplier))
                .ToDictionary(),
        };

        args.ModifiersList.Add(modifierSet);
    }
}

[Serializable, NetSerializable]
public sealed class UpdateMultihitDirectionEvent(NetEntity ent, Vector2 direction) : EntityEventArgs
{
    public NetEntity Ent = ent;

    public Vector2 Direction = direction;
}
