using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Trauma.Shared.DeepFryer.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.DeepFryer;

public sealed class DeepFriedSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private static readonly ProtoId<ShaderPrototype> ShaderName = "Fried";
    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = _protoMan.Index(ShaderName).InstanceUnique();

        SubscribeLocalEvent<DeepFriedComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<DeepFriedComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<DeepFriedComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
        SubscribeLocalEvent<DeepFriedComponent, ComponentStartup>(OnStartUp);
        SubscribeLocalEvent<DeepFriedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<DeepFriedComponent> ent, ref ComponentShutdown args)
    {
        if (!Terminating(ent.Owner))
            SetShader(ent, false);
    }

    private void OnStartUp(Entity<DeepFriedComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); ++i)
        {
            sprite.LayerSetShader(i, ShaderName);
        }

        SetShader(ent, true);
    }

    private void SetShader(Entity<DeepFriedComponent> ent, bool enabled)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        sprite.PostShader = enabled ? _shader : null;
        sprite.GetScreenTexture = enabled;
        sprite.RaiseShaderEvent = enabled;
    }

    private void OnHeldVisualsUpdated(Entity<DeepFriedComponent> ent, ref HeldVisualsUpdatedEvent args)
    {
        if (args.RevealedLayers.Count == 0)
        {
            return;
        }

        if (!TryComp(args.User, out SpriteComponent? sprite))
            return;

        foreach (var key in args.RevealedLayers)
        {
            if (!_sprite.LayerMapTryGet((args.User, sprite), key, out var index, true) || sprite[index] is not SpriteComponent.Layer layer)
                continue;

            sprite.LayerSetShader(index, ShaderName);
        }
    }

    private void OnEquipmentVisualsUpdated(Entity<DeepFriedComponent> ent, ref EquipmentVisualsUpdatedEvent args)
    {
        if (args.RevealedLayers.Count == 0)
        {
            return;
        }

        if (!TryComp(args.Equipee, out SpriteComponent? sprite))
            return;

        foreach (var key in args.RevealedLayers)
        {
            if (!_sprite.LayerMapTryGet((args.Equipee, sprite), key, out var index, true) || sprite[index] is not SpriteComponent.Layer)
                continue;

            sprite.LayerSetShader(index, ShaderName);
        }
    }


    private void OnAppearanceChange(Entity<DeepFriedComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        for (var i = 0; i < args.Sprite.AllLayers.Count(); ++i)
        {
            args.Sprite.LayerSetShader(i, ShaderName);
        }
    }
}
