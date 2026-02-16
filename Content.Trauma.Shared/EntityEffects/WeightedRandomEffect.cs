// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Like <c>WeightedRandomPrototype</c> but for <see cref="EntityEffect"/>
/// When ran it will activate a random effect.
/// </summary>
/// <remarks>
/// NOT predicted until predicted random is in stable?
/// </remarks>
public sealed partial class WeightedRandomEffect : EntityEffectBase<WeightedRandomEffect>
{
    [DataField(required: true)]
    public List<WeightedEffect> Children;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // none of this is loc but this is only used by mutations rn
        // if you add some chud ymlmaxxer reagent using this make this use loc!!!
        var builder = new StringBuilder("Randomly chooses 1 of the following effects:");
        var totalPercent = 100f / GetTotalWeights();
        foreach (var child in Children)
        {
            var percent = child.Weight * totalPercent;
            builder.Append("- ");
            builder.Append((int) percent);
            builder.Append("%: ");
            if (child.Effect.EntityEffectGuidebookText(prototype, entSys) is not {} text)
            {
                builder.Append("???,");
                continue;
            }

            builder.Append(text);
            builder.Append(","); // and you also have to add logic for this being hidden at the end
        }

        return builder.ToString();
    }

    public float GetTotalWeights()
    {
        var total = 0f;
        foreach (var child in Children)
        {
            total += child.Weight;
        }
        return total;
    }
}

public sealed class WeightedRandomEffectSystem : EntityEffectSystem<MetaDataComponent, WeightedRandomEffect>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<WeightedRandomEffect> args)
    {
        var total = 0f;
        // TODO: PredictedRandom when it's real
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent, ent.Comp).Id);
        var rand = new Random(seed);
        var effect = args.Effect;
        var target = rand.NextFloat() * effect.GetTotalWeights();
        foreach (var child in effect.Children)
        {
            total += child.Weight;
            if (total >= target)
            {
                _effects.TryApplyEffect(ent, child.Effect, args.Scale, args.User);
                return;
            }
        }
    }
}

[DataRecord]
public partial record struct WeightedEffect(float Weight, EntityEffect Effect);
