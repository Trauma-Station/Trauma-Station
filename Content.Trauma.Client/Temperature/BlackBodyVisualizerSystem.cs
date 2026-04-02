// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Trauma.Shared.Temperature;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.Temperature;

public sealed class BlackBodyVisualizerSystem : VisualizerSystem<BlackBodyComponent>
{
    [Dependency] private readonly SharedItemSystem _item = default!;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlackBodyComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<BlackBodyComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
    }

    protected override void OnAppearanceChange(EntityUid uid, BlackBodyComponent comp, ref AppearanceChangeEvent args)
    {
        if (!_spriteQuery.TryComp(uid, out var sprite) ||
            !AppearanceSystem.TryGetData<float>(uid, BlackBodyVisuals.Temperature, out var temperature, args.Component))
            return;

        var color = GetEmissiveColor(temperature);
        if (comp.Color == color)
            return; // no change

        comp.Color = color;
        foreach (var layer in sprite.AllLayers)
        {
            SetLayerEmissive((SpriteComponent.Layer) layer, color);
        }

        _item.VisualsChanged(uid); // update inhands and clothing sprites too

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

    private void OnHeldVisualsUpdated(Entity<BlackBodyComponent> ent, ref HeldVisualsUpdatedEvent args)
    {
        UpdateLayers(args.User, ent.Comp.Color, args.RevealedLayers);
    }

    private void OnEquipmentVisualsUpdated(Entity<BlackBodyComponent> ent, ref EquipmentVisualsUpdatedEvent args)
    {
        UpdateLayers(args.Equipee, ent.Comp.Color, args.RevealedLayers);
    }

    private void UpdateLayers(EntityUid uid, Color color, HashSet<string> keys)
    {
        if (!_spriteQuery.TryComp(uid, out var sprite))
            return;

        var ent = (uid, sprite);
        foreach (var key in keys)
        {
            if (SpriteSystem.LayerMapTryGet(ent, key, out var index, true) &&
                SpriteSystem.TryGetLayer(ent, index, out var layer, true))
            {
                SetLayerEmissive(layer, color);
            }
        }
    }

    private void SetLayerEmissive(SpriteComponent.Layer layer, Color color)
    {
        if (layer.ShaderPrototype != EmissiveShader || layer.Shader is not {} shader)
            return;

        // ensure it is mutable before modifying it
        if (!shader.Mutable)
        {
            shader = shader.Duplicate();
            layer.Shader = shader;
        }

        shader.SetParameter("emissive", color);
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

        // alpha curve that looks nice, no physical basis at all
        // y = ln1.347x - 0.118x + 0.312 where x is in kK
        // above 1800K it is purely glow, no base texture
        var x = t * 0.001f;
        var a = Math.Clamp(MathF.Log(1.347f * x) - 0.118f * x + 0.313f, 0f, 1f);
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
