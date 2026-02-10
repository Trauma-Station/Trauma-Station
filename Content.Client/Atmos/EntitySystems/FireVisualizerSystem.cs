// <Trauma>
using Content.Trauma.Common.Chaplain;
// </Trauma>
using Content.Client.Atmos.Components;
using Content.Shared.Atmos;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// This handles the display of fire effects on flammable entities.
/// </summary>
public sealed class FireVisualizerSystem : VisualizerSystem<FireVisualsComponent>
{
    [Dependency] private readonly PointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireVisualsComponent, ComponentStartup>(OnComponentStartup); // Goob edit
        SubscribeLocalEvent<FireVisualsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(EntityUid uid, FireVisualsComponent component, ComponentShutdown args)
    {
        if (component.LightEntity != null)
        {
            Del(component.LightEntity.Value);
            component.LightEntity = null;
        }

        // Need LayerMapTryGet because Init fails if there's no existing sprite / appearancecomp
        // which means in some setups (most frequently no AppearanceComp) the layer never exists.
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            SpriteSystem.LayerMapTryGet((uid, sprite), FireVisualLayers.Fire, out var layer, false))
        {
            SpriteSystem.RemoveLayer((uid, sprite), layer);
        }

        // <Trauma>
        if (component.LightEntityHoly != null)
        {
            Del(component.LightEntityHoly.Value);
            component.LightEntityHoly = null;
        }

        if (sprite != null && SpriteSystem.LayerMapTryGet((uid, sprite), HolyFireVisuals.HolyFire, out var alternateLayer, false))
        {
            SpriteSystem.RemoveLayer((uid, sprite), alternateLayer);
        }
        // </Trauma>
    }

    private void OnComponentStartup(EntityUid uid, FireVisualsComponent component, ComponentStartup args) // Goob edit
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp(uid, out AppearanceComponent? appearance))
            return;

        SpriteSystem.LayerMapReserve((uid, sprite), FireVisualLayers.Fire);
        SpriteSystem.LayerSetVisible((uid, sprite), FireVisualLayers.Fire, false);
        sprite.LayerSetShader(FireVisualLayers.Fire, "unshaded");
        if (component.Sprite != null)
            SpriteSystem.LayerSetRsi((uid, sprite), FireVisualLayers.Fire, new ResPath(component.Sprite));

        // <Trauma>
        // This checks if the resource file for the Holy Fire sprite exists.
        if (component.Sprite != null)
        {
            int lastIndex = component.Sprite.LastIndexOf('/');

            // Only proceed if the Sprite file ends with "onfire.rsi" to ensure it's the correct sprite kind.
            if (lastIndex != -1)
            {
                string endOfBitRSI = component.Sprite.Substring(lastIndex + 1);
                if (endOfBitRSI == "onfire.rsi")
                {
                    component.SpriteHoly = "_Trauma/Mobs/Effects/onholyfire.rsi";
                    // This adds an additional layer for Holy Fire effects. Don't need it if it's not a person.
                    SpriteSystem.LayerMapReserve((uid, sprite), HolyFireVisuals.HolyFire);
                    SpriteSystem.LayerSetVisible((uid, sprite), HolyFireVisuals.HolyFire, false);
                    sprite.LayerSetShader(HolyFireVisuals.HolyFire, "unshaded");
                    SpriteSystem.LayerSetRsi((uid, sprite), HolyFireVisuals.HolyFire, new ResPath(component.SpriteHoly));
                }
            }
        }
        // </Trauma>

        UpdateAppearance(uid, component, sprite, appearance);
    }

    protected override void OnAppearanceChange(EntityUid uid, FireVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            UpdateAppearance(uid, component, args.Sprite, args.Component);
    }

    private void UpdateAppearance(EntityUid uid, FireVisualsComponent component, SpriteComponent sprite, AppearanceComponent appearance)
    {
        // <Trauma>
        if (SpriteSystem.LayerMapTryGet((uid, sprite), HolyFireVisuals.HolyFire, out var indexHoly, false))
        {
            // This gets the data we passed in from HolyFlammableSystem.cs to process Holy Fire effects.
            AppearanceSystem.TryGetData<bool>(uid, HolyFireVisuals.OnFire, out var onFireHoly, appearance);
            AppearanceSystem.TryGetData<float>(uid, HolyFireVisuals.FireStacks, out var fireStacksHoly, appearance);
            SpriteSystem.LayerSetVisible((uid, sprite), indexHoly, onFireHoly);

            // If entity is not on fire, no need for light effects.
            if (!onFireHoly)
            {
                if (component.LightEntityHoly != null)
                {
                    Del(component.LightEntityHoly.Value);
                    component.LightEntityHoly = null;
                }
            }
            else
            {
                // Set the sprite state and light properties based on fire stacks.
                if (fireStacksHoly > component.FireStackAlternateState && !string.IsNullOrEmpty(component.AlternateState))
                    SpriteSystem.LayerSetRsiState((uid, sprite), indexHoly, component.AlternateState);
                else
                    SpriteSystem.LayerSetRsiState((uid, sprite), indexHoly, component.NormalState);

                component.LightEntityHoly ??= Spawn(null, new EntityCoordinates(uid, default));
                var lightHoly = EnsureComp<PointLightComponent>(component.LightEntityHoly.Value);

                _lights.SetColor(component.LightEntityHoly.Value, component.LightColorHoly, lightHoly);

                // light needs a minimum radius to be visible at all, hence the + 1.5f
                _lights.SetRadius(component.LightEntityHoly.Value, Math.Clamp(1.5f + component.LightRadiusPerStack * fireStacksHoly, 0f, component.MaxLightRadius), lightHoly);
                _lights.SetEnergy(component.LightEntityHoly.Value, Math.Clamp(1 + component.LightEnergyPerStack * fireStacksHoly, 0f, component.MaxLightEnergy), lightHoly);
            }
        }
        // </Trauma>

        if (!SpriteSystem.LayerMapTryGet((uid, sprite), FireVisualLayers.Fire, out var index, false))
            return;

        AppearanceSystem.TryGetData<bool>(uid, FireVisuals.OnFire, out var onFire, appearance);
        AppearanceSystem.TryGetData<float>(uid, FireVisuals.FireStacks, out var fireStacks, appearance);
        SpriteSystem.LayerSetVisible((uid, sprite), index, onFire);

        if (!onFire)
        {
            if (component.LightEntity != null)
            {
                Del(component.LightEntity.Value);
                component.LightEntity = null;
            }

            return;
        }

        if (fireStacks > component.FireStackAlternateState && !string.IsNullOrEmpty(component.AlternateState))
            SpriteSystem.LayerSetRsiState((uid, sprite), index, component.AlternateState);
        else
            SpriteSystem.LayerSetRsiState((uid, sprite), index, component.NormalState);

        component.LightEntity ??= Spawn(null, new EntityCoordinates(uid, default));
        var light = EnsureComp<PointLightComponent>(component.LightEntity.Value);

        _lights.SetColor(component.LightEntity.Value, component.LightColor, light);

        // light needs a minimum radius to be visible at all, hence the + 1.5f
        _lights.SetRadius(component.LightEntity.Value, Math.Clamp(1.5f + component.LightRadiusPerStack * fireStacks, 0f, component.MaxLightRadius), light);
        _lights.SetEnergy(component.LightEntity.Value, Math.Clamp(1 + component.LightEnergyPerStack * fireStacks, 0f, component.MaxLightEnergy), light);

        // TODO flickering animation? Or just add a noise mask to the light? But that requires an engine PR.
    }
}

public enum FireVisualLayers : byte
{
    Fire
}
