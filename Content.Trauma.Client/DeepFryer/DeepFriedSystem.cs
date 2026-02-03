using Content.Trauma.Shared.DeepFryer.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.DeepFryer;

public sealed class DeepFriedSystem : EntitySystem
{
    private static readonly ProtoId<ShaderPrototype> Shader = "Fried";

    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = _protoMan.Index(Shader).InstanceUnique();

        SubscribeLocalEvent<DeepFriedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DeepFriedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<DeepFriedComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        ent.Comp.OriginalColor = sprite.Color;
        _sprite.SetColor(ent.Owner, ent.Comp.DeepFriedColor); // Don't use a shader for this cuz it won't appear in the icons for the items, and it gets rid of the green outline
    }

    private void OnShutdown(Entity<DeepFriedComponent> ent, ref ComponentShutdown args)
    {
        if (!Terminating(ent.Owner))
            _sprite.SetColor(ent.Owner, ent.Comp.OriginalColor);
    }
}
