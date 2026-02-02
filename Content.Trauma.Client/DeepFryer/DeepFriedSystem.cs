using Content.Trauma.Shared.DeepFryer.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.DeepFryer;

public sealed class DeepFriedSystem : EntitySystem
{
    private static readonly ProtoId<ShaderPrototype> Shader = "Fried";

    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = _protoMan.Index(Shader).InstanceUnique();

        SubscribeLocalEvent<DeepFriedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DeepFriedComponent, ComponentShutdown>(OnShutdown);
    }

    private void SetShader(Entity<DeepFriedComponent?, SpriteComponent?> ent, bool enabled)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2, false))
            return;

        ent.Comp2.PostShader = enabled ? _shader : null;
        ent.Comp2.RaiseShaderEvent = enabled;
    }

    private void OnStartup(Entity<DeepFriedComponent> ent, ref ComponentStartup args)
    {
        SetShader(ent.AsNullable(), true);
    }

    private void OnShutdown(Entity<DeepFriedComponent> ent, ref ComponentShutdown args)
    {
        if (!Terminating(ent.Owner))
            SetShader(ent.AsNullable(), false);
    }
}
