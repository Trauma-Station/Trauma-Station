using Content.Shared.Examine;
using Content.Trauma.Shared.DeepFryer.Components;

namespace Content.Trauma.Shared.DeepFryer.Systems;

public sealed class SharedDeepFriedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeepFriedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<DeepFriedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("deep-fried-markup"));
    }
}
