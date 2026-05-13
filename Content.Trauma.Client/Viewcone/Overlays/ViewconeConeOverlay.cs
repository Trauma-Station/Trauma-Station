// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.MouseRotator;
using Content.Trauma.Shared.Viewcone.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Trauma.Client.Viewcone.Overlays;

/// <summary>
/// Renders the actual "cone" part of the viewcone, no alpha modulation
/// </summary>
public sealed class ViewconeConeOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private readonly SharedTransformSystem _xform;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public static ProtoId<ShaderPrototype> ShaderPrototype = "Viewcone";
    private readonly ShaderInstance _viewconeShader;

    private Entity<ViewconeComponent, EyeComponent, TransformComponent>? _eyeEntity;
    private float _coneAngle;
    private float _coneFeather;
    private float _coneIgnoreRadius;
    private float _coneIgnoreFeather;

    public ViewconeConeOverlay()
    {
        IoCManager.InjectDependencies(this);

        _xform = _ent.System<SharedTransformSystem>();

        _viewconeShader = _proto.Index(ShaderPrototype).InstanceUnique();
        ZIndex = -6;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _eyeEntity = null;

        if (args.Viewport.Eye == null)
            return false;

        // TODO: engine thing
        // This is really stupid but there isn't another way to reverse an eye entity from just an IEye afaict
        // It's not really inefficient though. theres barely any of those fuckin things anyway (? verify that) (maybe this scales with players in view) (shit)
        var enumerator = _ent.AllEntityQueryEnumerator<ViewconeComponent, EyeComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var viewcone, out var eye, out var xform))
        {
            if (args.Viewport.Eye != eye.Eye)
                continue;

            _coneAngle = viewcone.CurrentConeAngle;
            _coneFeather = viewcone.ConeFeather;
            _coneIgnoreRadius = (viewcone.ConeIgnoreRadius - viewcone.ConeIgnoreFeather) * 50f;
            _coneIgnoreFeather = Math.Max(viewcone.ConeIgnoreFeather * 200f, 8f);
            _eyeEntity = (uid, viewcone, eye, xform);
            break;
        }

        return _eyeEntity != null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        // don't need to do anything if you have full vision
        if (ScreenTexture == null || _eyeEntity == null || _coneAngle >= 360f)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        var (uid, viewcone, eye, xform) = _eyeEntity.Value;
        var zoom = eye.Zoom.X;
        var viewAngle = (float) viewcone.ViewAngle.Theta;

        _viewconeShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _viewconeShader.SetParameter("Zoom", zoom);
        _viewconeShader.SetParameter("ViewAngle", viewAngle);
        _viewconeShader.SetParameter("ConeAngle", _coneAngle);
        _viewconeShader.SetParameter("ConeFeather", _coneFeather);
        _viewconeShader.SetParameter("ConeIgnoreRadius", _coneIgnoreRadius);
        _viewconeShader.SetParameter("ConeIgnoreFeather", _coneIgnoreFeather);

        worldHandle.UseShader(_viewconeShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
        _eyeEntity = null;
    }
}
