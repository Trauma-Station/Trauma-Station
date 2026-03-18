// i do NOT know what iam doing PLEASE remake this if you want to do something like add skillchips to non antags (even though thats what im trying to do)
using Content.Server.Ghost.Roles.Events;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Server.GhostRole;

public sealed class GhostRolePlayerEffectsSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostRolePlayerEffectsComponent, GhostRoleSpawnerUsedEvent>(OnSpawnerUsed);
    }

    private void OnSpawnerUsed(Entity<GhostRolePlayerEffectsComponent> ent, ref GhostRoleSpawnerUsedEvent args)
    {
        _effects.ApplyEffects(args.Spawned, ent.Comp.Effects);
    }
}
