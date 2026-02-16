// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Removes bolas from the target entity.
/// </summary>
public sealed partial class RemoveSnares : EntityEffectBase<RemoveSnares>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-remove-snares", ("chance", Probability));
}

public sealed class RemoveSnaresEffectSystem : EntityEffectSystem<EnsnareableComponent, RemoveSnares>
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<EnsnareableComponent> ent, ref EntityEffectEvent<RemoveSnares> args)
    {
        var user = args.User ?? ent.Owner;

        // snare api is dogshit and i cbf to improve it
        foreach (var bola in ent.Comp.Container.ContainedEntities)
        {
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, 0, new EnsnareableDoAfterEvent(), user, user, bola));
            _transform.DropNextTo(bola, user);
        }
    }
}
