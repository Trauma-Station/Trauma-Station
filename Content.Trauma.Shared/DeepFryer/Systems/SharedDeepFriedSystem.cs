using Content.Shared.Examine;
using Content.Shared.Nutrition.EntitySystems;
using Content.Trauma.Shared.DeepFryer.Components;

namespace Content.Trauma.Shared.DeepFryer.Systems;

public sealed class SharedDeepFriedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeepFriedComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DeepFriedComponent, FlavorProfileModificationEvent>(OnFlavorMod);
    }

    private void OnExamined(Entity<DeepFriedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("deep-fried-markup"));
    }

    private void OnFlavorMod(Entity<DeepFriedComponent> ent, ref FlavorProfileModificationEvent args)
    {
        args.Flavors.Add("DeepFried");
    }
}
