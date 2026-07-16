using Content.Shared.Nyanotrasen.Holograms;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Nyanotrasen.Holograms;

// TODO: kill or move to modules
public sealed partial class HologramVisualizerSystem : EntitySystem
{
    private readonly ProtoId<ShaderPrototype> _shaderId = "Holographic";
    private ShaderPrototype? _shaderProto;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HologramVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HologramVisualsComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentInit(EntityUid uid, HologramVisualsComponent component, ComponentInit args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
            sprite.PostShader = (_shaderProto ??= ProtoMan.Index(_shaderId)).InstanceUnique();
    }

    private void OnComponentShutdown(EntityUid uid, HologramVisualsComponent component, ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
            sprite.PostShader = null;
    }
}
