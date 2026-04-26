// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Shared.Wounds;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds a wound to the target body part.
/// </summary>
public sealed partial class AddWounds : EntityEffectBase<AddWounds>
{
    [DataField(required: true)]
    public List<EntProtoId> Wounds;
}

public sealed class AddWoundPartEffectSystem : EntityEffectSystem<WoundableComponent, AddWounds>
{
    [Dependency] private readonly WoundSystem _wound = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    protected override void Effect(Entity<WoundableComponent> ent, ref EntityEffectEvent<AddWounds> args)
    {
        foreach (var addWound in args.Effect.Wounds)
        {
            var proto = _prototype.Index<EntityPrototype>(addWound);

            if (!proto.Components.TryGetComponent(Factory, out WoundComponent? comp))
            {
                Log.Error($"Tried to apply {addWound} as a wound, but it doesn't have a {nameof(WoundComponent)}.");
                continue;
            }
            var damageGroup = _prototype.EnumeratePrototypes<DamageGroupPrototype>().FirstOrDefault(g => g.DamageTypes.Contains(comp.DamageType));
            if (damageGroup is not { })
            {
                Log.Error($"Tried to apply {addWound} as a wound, but it doesn't have a valid damage group.");
                continue;
            }
            _wound.TryInduceWound(ent, addWound, args.Scale * 20, out _, damageGroup: damageGroup);
        }
    }
}
