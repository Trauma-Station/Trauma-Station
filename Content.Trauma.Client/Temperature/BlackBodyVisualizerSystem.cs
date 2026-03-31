// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Temperature;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.Temperature;

public sealed class BlackBodyVisualizerSystem : VisualizerSystem<BlackBodyComponent>
{
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly EntityQuery<PointLightComponent> _lightQuery = default!;
    [Dependency] private readonly EntityQuery<SpriteComponent> _spriteQuery = default!;

    public static readonly ProtoId<ShaderPrototype> EmissiveShader = "Emissive";
    public const float MinGlowTemp = 600f;
    public const float Planck = 6.62607004e-34f; // J.s
    public const float StephanBoltzmann = 5.670373e-8f; // W/m^2.K^4
    public const float Boltzmann = 1.3806485279e-23f; // J/K
    public const float SpeedOfLight = 299792458f; // m/s
    public const float Gamma = 1f / 2.2f;

    protected override void OnAppearanceChange(EntityUid uid, BlackBodyComponent comp, ref AppearanceChangeEvent args)
    {
        if (!_spriteQuery.TryComp(uid, out var sprite) ||
            !AppearanceSystem.TryGetData<float>(uid, BlackBodyVisuals.Temperature, out var temperature, args.Component))
            return;

        var color = GetEmissiveColor(temperature);
        foreach (var ilayer in sprite.AllLayers)
        {
            var layer = (SpriteComponent.Layer) ilayer;
            if (layer.ShaderPrototype != EmissiveShader || layer.Shader is not {} shader)
                continue;

            // ensure it is mutable before modifying it
            if (!shader.Mutable)
            {
                shader = shader.Duplicate();
                layer.Shader = shader;
            }

            shader.SetParameter("emissive", color);
        }

        var light = _lightQuery.Comp(uid);
        var glowing = temperature > MinGlowTemp;
        _light.SetEnabled(uid, glowing, light);
        if (!glowing)
            return;

        // looks nice
        var energy = MathF.Pow(temperature / 400f, 1.5f);
        var radius = 1.25f + color.A * comp.MaxLightRadius;

        // clientside only since shadowlings and stuff don't need to worry about forged items
        // also don't want to waste any cpu/net networking this when only 1 value is needed
        _light.SetColor(uid, color, light);
        _light.SetEnergy(uid, energy, light);
        _light.SetRadius(uid, radius, light);
    }

    // rgb is the black body visible radiation color
    // a is the intensity, how much of the black body color to use instead of the base sprite texture
    private Color GetEmissiveColor(float t)
    {
        // no visible glow until reasonably hot
        if (t < MinGlowTemp)
            return new Color(0, 0, 0, 0);

        // clamp it incase some lavabeaker level shit has an infinitely hot item
        t = Math.Clamp(t, MinGlowTemp, 6000f);
        var flux = BlackBodyFlux(t);
        var rUpper = WavelengthValue(700e-9f, t);
        var rLower = WavelengthValue(600e-9f, t);
        var gUpper = rLower;
        var gLower = WavelengthValue(500e-9f, t);
        var bUpper = gLower;
        var bLower = WavelengthValue(400e-9f, t);

        var r = flux * (rUpper - rLower);
        var g = flux * (gUpper - gLower);
        var b = flux * (bUpper - bLower);

        // tonemapping + gamma correction
        void Correct(ref float c)
        {
            c = 1f - MathF.Pow(2, -c);
            c = MathF.Pow(c, Gamma);
        }
        Correct(ref r);
        Correct(ref g);
        Correct(ref b);

        // at 1200K it is purely glow, no base texture
        var a = Math.Clamp(MathF.Pow(t / 1200f, 2f), 0f, 1f);
        return new Color(r, g, b, a);
    }

    private float ChannelValue(float waveA, float waveB, float t)
        => WavelengthValue(waveB, t) - WavelengthValue(waveA, t);

    private float BlackBodyFlux(float t)
        => StephanBoltzmann * MathF.Pow(t, 4);

    private float WavelengthValue(float wave, float t)
    {
        const float Scale = 15f / (MathF.PI*MathF.PI*MathF.PI*MathF.PI);
        const float C2 = Planck * SpeedOfLight / Boltzmann;
        var z = C2 / (wave * t);
        var z2 = z * z;
        return Scale * (z*z2 + 3f*z2 + 6f*z + 6f) * MathF.Exp(-z);
    }
}
