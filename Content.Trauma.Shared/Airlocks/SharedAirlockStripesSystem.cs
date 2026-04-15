using Content.Shared.SprayPainter.Prototypes;

namespace Content.Trauma.Shared.Airlocks;

public abstract class SharedAirlockStripesSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockStripesComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<AirlockStripesComponent> ent, ref ComponentStartup args)
    {
        if (Appearance.TryGetData(ent, PaintableVisuals.Prototype, out _) || Prototype(ent) is not { } proto)
            return;

        Appearance.SetData(ent, PaintableVisuals.Prototype, proto.ID);
    }
}
