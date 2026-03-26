// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.ChildEntities;

/// <summary>
/// Handles spawning child entities on an entity
/// </summary>
public sealed class ChildEntitiesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChildEntitiesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChildEntitiesComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<ChildEntitiesComponent> ent, ref MapInitEvent args)
    {
        // We iterate over all the stored child prototypes and spawn them attached to the owner of this component,
        // accounting for offset and rotation.
        foreach (var child in ent.Comp.ChildPrototypes)
        {
            var coords = Transform(ent).Coordinates;
            var rotation = Transform(ent).LocalRotation;

            coords = coords.WithPosition(coords.Position + child.Offset);

            var childEnt = PredictedSpawnAttachedTo(child.Prototype, coords, null, rotation);
            ent.Comp.Children.Add(childEnt);
        }

        Dirty(ent);
    }

    private void OnShutdown(Entity<ChildEntitiesComponent> ent, ref ComponentShutdown args)
    {
        foreach (var child in ent.Comp.Children)
        {
            if (TerminatingOrDeleted(child))
                continue;

            PredictedQueueDel(child);
        }
    }
}
