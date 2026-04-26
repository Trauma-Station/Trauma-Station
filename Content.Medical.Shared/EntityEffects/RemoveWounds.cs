// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Wounds;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Removes a wound from the target body part.
/// </summary>
public sealed partial class RemoveWounds : EntityEffectBase<RemoveWounds>
{
    [DataField(required: true)]
    public List<EntProtoId> Wounds;
}

public sealed class RemoveWoundPartEffectSystem : EntityEffectSystem<WoundableComponent, RemoveWounds>
{
    [Dependency] private readonly WoundSystem _wound = default!;

    protected override void Effect(Entity<WoundableComponent> ent, ref EntityEffectEvent<RemoveWounds> args)
    {
        var wounds = _wound.GetAllWounds(ent.Owner);
        foreach (var addWounds in args.Effect.Wounds)
        {
            foreach (var wound in wounds)
            {
                if (Prototype(wound) is { } proto && proto.ID == addWounds)
                {
                    if (!TryComp<WoundComponent>(wound, out var woundComp))
                        break;

                    woundComp.CanBeHealed = true;
                    _wound.ApplyWoundSeverity(wound, -100, woundComp);

                    _wound.UpdateWoundableIntegrity(ent.Owner, ent.Comp);
                    break;
                }
            }
        }
    }
}
