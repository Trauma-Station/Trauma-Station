// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Graphics;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class ItemOverlaySystem : EntitySystem
{
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new ItemOverlayOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<ItemOverlayOverlay>();
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<ItemOverlayComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.RT = _clyde.CreateRenderTarget(ent.Comp.RenderTargetSize, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: $"item_overlay_{ent.Owner}");

        var instance = ProtoMan.Index(ent.Comp.Shader).InstanceUnique();
        _sprite.SetPostShader(ent.Owner, new(ent.Comp.Shader, instance) { RaiseShaderEvent = true, Before = ContentPostShaderIds.BeforeOutlines });
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ItemOverlayComponent> ent, ref ComponentShutdown args)
    {
        _sprite.RemovePostShader(ent.Owner, ent.Comp.Shader);
        ent.Comp.RT?.Dispose();
    }

    [SubscribeLocalEvent]
    private void OnShaderRender(Entity<ItemOverlayComponent> ent, ref BeforePostShaderRenderEvent args)
    {
        if (args.Id != ent.Comp.Shader)
            return;

        if (ent.Comp.RT?.Texture is not { } texture)
            return;

        var eyeRot = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var rot = _transform.GetWorldRotation(ent) + eyeRot;

        args.Shader.SetParameter("overlayTexture", texture);
        args.Shader.SetParameter("ratio", ent.Comp.Ratio);
        args.Shader.SetParameter("scale", args.Viewport.Eye?.Scale ?? Vector2.One);
        args.Shader.SetParameter("rot", (float) rot.Theta);
    }
}
