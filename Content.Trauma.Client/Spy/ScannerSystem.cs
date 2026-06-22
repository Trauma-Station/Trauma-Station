using Content.Goobstation.Client.Shaders;
using Content.Goobstation.Common.Shaders;
using Content.Trauma.Shared.Spy;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Spy;

public sealed class ScannerSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IGameTiming _timing = default!;

    public static readonly ProtoId<ShaderPrototype> ScanShader = "Scan";

    public override void Initialize()
    {
        base.Initialize();

        _overlayMan.AddOverlay(new ScannerOverlay());

        SubscribeLocalEvent<ActiveScannerComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<ActiveScannerComponent, ComponentShutdown>(OnScannerShutdown);

        SubscribeLocalEvent<BeingScannedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BeingScannedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BeingScannedComponent, BeforePostMultiShaderRenderEvent>(OnBeforeRender);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayMan.RemoveOverlay<ScannerOverlay>();
    }

    private void OnBeforeRender(Entity<BeingScannedComponent> ent, ref BeforePostMultiShaderRenderEvent args)
    {
        if (args.Shader != ScanShader)
            return;

        if (!Exists(ent.Comp.Scanner) || !TryComp(ent.Comp.Scanner, out ActiveScannerComponent? scanner))
            return;

        var factor = InverseLerp(scanner.ScanStartTime, scanner.ScanEndTime, _timing.CurTime);
        args.Instance.SetParameter("factor", factor);
        args.Instance.SetParameter("scanColor", scanner.ScanColor);
        ent.Comp.Shader = args.Instance;
    }

    private void OnShutdown(Entity<BeingScannedComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var ev = new SetMultiShaderEvent(ScanShader,
            false,
            ent.Comp.MultiShaderOrder,
            Mutable: false,
            RaiseEvent: true);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnStartup(Entity<BeingScannedComponent> ent, ref ComponentStartup args)
    {
        if (!Exists(ent.Comp.Scanner))
            RemCompDeferred(ent, ent.Comp);

        var ev = new SetMultiShaderEvent(ScanShader,
            true,
            ent.Comp.MultiShaderOrder,
            Mutable: false,
            RaiseEvent: true);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnState(Entity<ActiveScannerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!Exists(ent.Comp.ScannedObject))
            return;

        EnsureComp<BeingScannedComponent>(ent.Comp.ScannedObject).Scanner = ent;
    }

    private void OnScannerShutdown(Entity<ActiveScannerComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent.Comp.ScannedObject))
            return;

        RemCompDeferred<BeingScannedComponent>(ent.Comp.ScannedObject);
    }

    private float InverseLerp(TimeSpan min, TimeSpan max, TimeSpan value)
    {
        return max <= min ? 1f : (float) Math.Clamp((value - min) / (max - min), 0f, 1f);
    }
}
