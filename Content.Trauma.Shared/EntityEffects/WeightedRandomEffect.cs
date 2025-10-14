using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Text;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Like <c>WeightedRandomPrototype</c> but for <see cref="EntityEffect"/>
/// When ran it will activate a random effect.
/// </summary>
/// <remarks>
/// NOT predicted until chuden cherry picks predicted random
/// </remarks>
public sealed partial class WeightedRandomEffect : EntityEffect
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [DataField(required: true)]
    public List<WeightedEffect> Children;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var total = 0f;
        var target = _random.NextFloat() * GetTotalWeights();
        foreach (var child in Children)
        {
            total += child.Weight;
            // if the first one can't apply it should pick the first working one after that
            if (total >= target && child.Effect.ShouldApply(args, _random))
            {
                child.Effect.Effect(args);
                return;
            }
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
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
            if (child.Effect.GuidebookEffectDescription(prototype, entSys) is not {} text)
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

[DataRecord]
public record struct WeightedEffect(float Weight, EntityEffect Effect);
