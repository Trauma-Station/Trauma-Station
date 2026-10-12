// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Enums;

namespace Content.Trauma.Client.Radiation;

public sealed partial class RadiationOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> RadiationShader = "RadiationNoise";
    [Dependency] private IPrototypeManager _proto = default!;

    public float Radiation;
    private readonly ShaderInstance _shader;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public RadiationOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(RadiationShader).InstanceUnique().Duplicate();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var intensity = Math.Clamp(Radiation, 0f, 20f);
        _shader.SetParameter("intensity", intensity);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
