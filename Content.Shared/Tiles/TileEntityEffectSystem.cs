// <Trauma>
using Content.Shared.EntityConditions;
// </Trauma>
using Content.Shared.StepTrigger.Systems;
using Content.Shared.EntityEffects;

namespace Content.Shared.Tiles; // Trauma - moved to shared

public sealed partial class TileEntityEffectSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private SharedEntityConditionsSystem _condition = default!;
    // </Trauma>
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TileEntityEffectComponent, StepTriggeredOffEvent>(OnTileStepTriggered);
        SubscribeLocalEvent<TileEntityEffectComponent, StepTriggerAttemptEvent>(OnTileStepTriggerAttempt);
    }

    private void OnTileStepTriggerAttempt(Entity<TileEntityEffectComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnTileStepTriggered(Entity<TileEntityEffectComponent> ent, ref StepTriggeredOffEvent args)
    {
        var otherUid = args.Tripper;

        // <Trauma>
        if (!_condition.TryConditions(otherUid, ent.Comp.Conditions, user: otherUid))
            return;
        // </Trauma>

        _entityEffects.ApplyEffects(otherUid, ent.Comp.Effects.ToArray(), user: otherUid);
    }
}
