// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Trauma.Shared.DeepFryer.Components;

namespace Content.Trauma.Client.DeepFryer;

public sealed partial class DeepFriedSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly ProtoId<ShaderPrototype> ShaderName = "Fried";
    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = ProtoMan.Index(ShaderName).InstanceUnique();
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<DeepFriedComponent> ent, ref ComponentShutdown args)
    {
        if (!Terminating(ent.Owner))
            SetShader(ent, false);
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<DeepFriedComponent> ent, ref ComponentStartup args)
    {
        SetShader(ent, true);
    }

    private void SetShader(EntityUid uid, bool enabled)
    {
        if (!enabled)
        {
            _sprite.RemovePostShader(uid, ShaderName);
            return;
        }

        var data = new SpriteComponent.PostShaderArgs(ShaderName, _shader)
        {
            GetScreenTexture = true,
            RaiseShaderEvent = true
        };
        _sprite.SetPostShader(uid, data);
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnEquipmentVisualsUpdated(Entity<DeepFriedComponent> ent, ref EquipmentVisualsUpdatedEvent args)
    {
        if (args.RevealedLayers.Count == 0)
        {
            return;
        }

        if (!TryComp(args.Equipee, out SpriteComponent? sprite))
            return;

        // TODO: is this really needed
        foreach (var key in args.RevealedLayers)
        {
            if (!_sprite.LayerMapTryGet((args.Equipee, sprite), key, out var index, true) || sprite[index] is not SpriteComponent.Layer)
                continue;

            sprite.LayerSetShader(index, ShaderName);
        }
    }

    [SubscribeLocalEvent]
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
