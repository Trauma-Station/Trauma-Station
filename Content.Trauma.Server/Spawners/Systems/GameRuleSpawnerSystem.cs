using Content.Server.GameTicking.Rules;
using Content.Shared.EntityEffects;
using Content.Trauma.Server.Spawners.Components;

namespace Content.Trauma.Server.Spawners.Systems;

public sealed partial class GameRuleSpawnerSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnLoaded(Entity<ConditionalSpawnRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        var query = EntityQueryEnumerator<GameRuleSpawnerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (xform.MapID != args.Map)
                continue;

            if (!comp.ToSpawn.TryGetValue(ent.Comp.Key, out var effect))
            {
                if (comp.Fallback is not { } fallback)
                    continue;

                effect = fallback;
            }

            _effects.TryApplyEffect(uid, effect, predicted: false);
        }
    }
}
