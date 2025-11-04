using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Makes the target throw a random item in a random direction.
/// </summary>
public sealed partial class ThrowRandomItem : EntityEffectBase<ThrowRandomItem>
{
    [DataField]
    public float Force = 10f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-throw-random-item", ("chance", Probability));
}

public sealed class ThrowRandomItemEffectSystem : EntityEffectSystem<HandsComponent, ThrowRandomItem>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private List<EntityUid> _items = new();

    protected override void Effect(Entity<HandsComponent> ent, ref EntityEffectEvent<ThrowRandomItem> args)
    {
        _items.Clear();
        foreach (var held in _hands.EnumerateHeld((ent, ent.Comp)))
        {
            _items.Add(held);
        }

        if (_items.Count == 0)
            return;

        // TODO: PredictedRandom when it's real
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new Random(seed);
        var item = rand.Pick(_items);
        var angle = rand.NextAngle();
        var direction = angle.ToVec();
        _throwing.TryThrow(item,
            direction,
            args.Effect.Force,
            user: ent);
    }
}
