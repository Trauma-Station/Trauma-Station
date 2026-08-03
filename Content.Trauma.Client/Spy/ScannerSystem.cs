using Content.Trauma.Shared.Spy;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Spy;

public sealed partial class ScannerSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;

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

        if (!Exists(ent.Comp.Scanner) || !TryComp(ent.Comp.Scanner, out ActiveScannerComponent? scanner))
            return;

        var ratio = InverseLerp(scanner.ScanStartTime, scanner.ScanEndTime, _timing.CurTime);
        args.Shader.SetParameter("ratio", ratio);
        ent.Comp.Ratio = ratio;
        var zoom = 1f;

        if (args.Viewport.Eye is { } eye)
            zoom = eye.Zoom.X;

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
                RaiseShaderEvent = true
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
