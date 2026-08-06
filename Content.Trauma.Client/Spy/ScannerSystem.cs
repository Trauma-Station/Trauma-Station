// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Graphics;
using Content.Trauma.Shared.Spy;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Spy;

public sealed partial class ScannerSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TransformSystem _transform = default!;

    [Dependency] private EntityQuery<ActiveScannerComponent> _scannerQuery = default!;

    public static readonly ProtoId<ShaderPrototype> ScanShader = "Scan";
    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlayMan.AddOverlay(new ScannerOverlay());
        _shader = ProtoMan.Index(ScanShader).InstanceUnique();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayMan.RemoveOverlay<ScannerOverlay>();
    }

    [SubscribeLocalEvent]
    private void OnBeforeRender(Entity<BeingScannedComponent> ent, ref BeforePostShaderRenderEvent args)
    {
        if (args.Id != ScanShader)
            return;

        if (!Exists(ent.Comp.Scanner) || !_scannerQuery.TryComp(ent.Comp.Scanner, out var scanner))
            return;

        var ratio = InverseLerp(scanner.ScanStartTime, scanner.ScanEndTime, _timing.CurTime);
        args.Shader.SetParameter("ratio", ratio);
        ent.Comp.Ratio = ratio;
        var zoom = 1f;
        var eyeRot = Angle.Zero;

        if (args.Viewport.Eye is { } eye)
        {
            eyeRot = eye.Rotation;
            zoom = eye.Zoom.X;
        }

        var rot = args.Sprite.Rotation - eyeRot;
        if (!args.Sprite.NoRotation)
            rot -= _transform.GetWorldRotation(ent);

        args.Shader.SetParameter("angle", (float) rot.Theta);
        args.Shader.SetParameter("zoom", zoom);
    }

    [SubscribeLocalEvent]
    private void OnScannedShutdown(Entity<BeingScannedComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _sprite.RemovePostShader(ent.Owner, ScanShader);
    }

    [SubscribeLocalEvent]
    private void OnScannedStartup(Entity<BeingScannedComponent> ent, ref ComponentStartup args)
    {
        _sprite.SetPostShader(ent.Owner,
            new(ScanShader, _shader)
            {
                RaiseShaderEvent = true,
                Before = ContentPostShaderIds.BeforeOutlines,
            });
    }

    [SubscribeLocalEvent]
    private void OnScannerShutdown(Entity<ActiveScannerComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent.Comp.ScannedObject))
            return;

        RemCompDeferred<BeingScannedComponent>(ent.Comp.ScannedObject);
    }

    [SubscribeLocalEvent]
    private void OnState(Entity<ActiveScannerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!Exists(ent.Comp.ScannedObject))
            return;

        EnsureComp<BeingScannedComponent>(ent.Comp.ScannedObject).Scanner = ent;
    }

    private float InverseLerp(TimeSpan min, TimeSpan max, TimeSpan value)
    {
        return max <= min ? 1f : (float) Math.Clamp((value - min) / (max - min), 0f, 1f);
    }
}
