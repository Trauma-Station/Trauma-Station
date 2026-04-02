using Content.Shared.Item;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// Trauma - code for anything burgers and increasing size with each item
/// </summary>
public sealed partial class FoodSequenceSystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;

    public static readonly ProtoId<FoodSequenceElementPrototype> FallbackElement = "ItemFallback";

    private void UpdateFoodSize(Entity<FoodSequenceStartPointComponent> start)
    {
        var increment = (start.Comp.FoodLayers.Count / 2);

        if (HasComp<ItemComponent>(start))
        {
            var sizeMap = new Dictionary<int, string>
            {
                { 1, "Small" },
                { 2, "Normal" },
                { 3, "Large" },
                { 4, "Huge" },
                { 5, "Ginormous" }
            };

            if (sizeMap.ContainsKey(increment))
            {
                _item.SetSize(start, sizeMap[increment]);
            }
            else if (increment == 6)
            {
                _transform.DropNextTo(start.Owner, start.Owner);
                RemComp<ItemComponent>(start);
            }

            _item.SetShape(start, new List<Box2i> { new Box2i(0, 0, 1, increment) });
        /* TODO: uncomment this if >15 item burgers are ever added again AND gravity well is moved to shared
        } else if (increment >= 8) {
            EnsureComp<GravityWellComponent>(start, out var gravityWell);
            gravityWell.MaxRange = (float)Math.Sqrt(increment/4);
            gravityWell.BaseRadialAcceleration = (float)Math.Sqrt(increment/4);
            Dirty(start, gravityWell);
        */
        }
    }
}
