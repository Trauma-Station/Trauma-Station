// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Alerts;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content._Trauma.Shared.CosmicCult.Components.Examine;
using Content._Trauma.Shared.CosmicCult.Components;
using Content._Trauma.Shared.CosmicCult;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.SSDIndicator;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Numerics;
using Timer = Robust.Shared.Timing.Timer;

namespace Content._Trauma.Client.CosmicCult;

public sealed partial class CosmicCultSystem : SharedCosmicCultSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly ResPath _rsiPath = new("/Textures/_DV/CosmicCult/Effects/ability_siphonvfx.rsi");
    private readonly ProtoId<FactionIconPrototype> StatusIcon = "CosmicCultIcon";
    private readonly SoundSpecifier _siphonSFX = new SoundPathSpecifier("/Audio/_DV/CosmicCult/ability_siphon.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicSubtleMarkComponent, DidEquipEvent>((uid, _, _) => UpdateSubtleMarkVisibility(uid));
        SubscribeLocalEvent<CosmicSubtleMarkComponent, DidEquipHandEvent>((uid, _, _) => UpdateSubtleMarkVisibility(uid));
        SubscribeLocalEvent<CosmicSubtleMarkComponent, DidUnequipEvent>((uid, _, _) => UpdateSubtleMarkVisibility(uid));
        SubscribeLocalEvent<CosmicSubtleMarkComponent, DidUnequipHandEvent>((uid, _, _) => UpdateSubtleMarkVisibility(uid));
        SubscribeLocalEvent<CosmicSubtleMarkComponent, WearerMaskToggledEvent>((uid, _, _) => UpdateSubtleMarkVisibility(uid));

        SubscribeLocalEvent<CosmicStarMarkComponent, ComponentStartup>(OnCosmicStarMarkAdded);
        SubscribeLocalEvent<CosmicStarMarkComponent, ComponentShutdown>(OnCosmicStarMarkRemoved);

        SubscribeLocalEvent<CosmicSubtleMarkComponent, ComponentStartup>(OnCosmicSubtleMarkAdded);
        SubscribeLocalEvent<CosmicSubtleMarkComponent, ComponentShutdown>(OnCosmicSubtleMarkRemoved);

        SubscribeLocalEvent<CosmicBlankComponent, ComponentStartup>(OnBlankAdded);
        SubscribeLocalEvent<CosmicBlankComponent, ComponentShutdown>(OnBlankRemoved);

        SubscribeLocalEvent<CosmicImposingComponent, ComponentStartup>(OnCosmicImpositionAdded);
        SubscribeLocalEvent<CosmicImposingComponent, ComponentShutdown>(OnCosmicImpositionRemoved);

        SubscribeLocalEvent<CosmicCultistComponent, GetStatusIconsEvent>(GetCosmicCultIcon);
        SubscribeLocalEvent<CosmicBlankComponent, GetStatusIconsEvent>(GetCosmicSSDIcon);

        SubscribeLocalEvent<HumanoidProfileComponent, CosmicSiphonIndicatorEvent>(OnSiphon);
    }

    #region Siphon Visuals
    private void OnSiphon(Entity<HumanoidProfileComponent> ent, ref CosmicSiphonIndicatorEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;
        var layer = _sprite.AddLayer((ent, sprite), new SpriteSpecifier.Rsi(_rsiPath, "vfx"));
        _sprite.LayerMapSet((ent, sprite), CultSiphonedVisuals.Key, layer);
        _sprite.LayerSetOffset((ent, sprite), layer, new Vector2(0, 0.8f));
        _sprite.LayerSetScale((ent, sprite), layer, new Vector2(0.65f, 0.65f));
        sprite.LayerSetShader(layer, "unshaded");

        Timer.Spawn(TimeSpan.FromSeconds(2), () =>
        {
            if (Exists(ent) && _sprite.LayerMapTryGet((ent, sprite), CultSiphonedVisuals.Key, out var layer, false))
                _sprite.RemoveLayer((ent, sprite), layer);
        });
        _audio.PlayLocal(_siphonSFX, ent, ent, AudioParams.Default.WithVariation(0.1f));
    }
    #endregion

    #region Layer Additions
    private void OnCosmicStarMarkAdded(Entity<CosmicStarMarkComponent> uid, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || _sprite.LayerMapTryGet((uid, sprite), CosmicRevealedKey.Key, out _, logMissing: false))
            return;

        var layer = _sprite.AddLayer((uid, sprite), uid.Comp.Sprite);
        _sprite.LayerMapSet((uid, sprite), CosmicRevealedKey.Key, layer);
        sprite.LayerSetShader(layer, "unshaded");

        if (TryComp<CosmicMarkVisualsComponent>(uid, out var offset))
        {
            _sprite.LayerSetOffset((uid, sprite), CosmicRevealedKey.Key, offset.Offset);
            _sprite.LayerSetRsiState((uid, sprite), layer, offset.StarState);
        }
    }

    private void OnCosmicSubtleMarkAdded(Entity<CosmicSubtleMarkComponent> uid, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || _sprite.LayerMapTryGet((uid, sprite), CosmicRevealedKey.Key, out _, logMissing: false))
            return;

        var layer = _sprite.AddLayer((uid, sprite), uid.Comp.Sprite);
        _sprite.LayerMapSet((uid, sprite), CosmicRevealedKey.Key, layer);
        sprite.LayerSetShader(layer, "unshaded");

        UpdateSubtleMarkVisibility(uid);

        if (TryComp<CosmicMarkVisualsComponent>(uid, out var offset))
        {
            _sprite.LayerSetOffset((uid, sprite), CosmicRevealedKey.Key, offset.Offset);
            _sprite.LayerSetRsiState((uid, sprite), layer, offset.SubtleState);
        }
    }

    private void OnCosmicImpositionAdded(Entity<CosmicImposingComponent> uid, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || _sprite.LayerMapTryGet((uid, sprite), CosmicImposingKey.Key, out _, logMissing: false))
            return;

        var layer = _sprite.AddLayer((uid, sprite), uid.Comp.Sprite);

        _sprite.LayerMapSet((uid, sprite), CosmicImposingKey.Key, layer);
        sprite.LayerSetShader(layer, "unshaded");
    }

    private void OnBlankAdded(Entity<CosmicMalignEchoComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<SSDIndicatorComponent>(ent, out var comp))
            comp.HideIcon = true; // It adds it's own icon so we hide this one to reduce visual clutter
    }
    #endregion

    #region Layer Removals
    private void OnCosmicStarMarkRemoved(Entity<CosmicStarMarkComponent> uid, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.RemoveLayer((uid, sprite), CosmicRevealedKey.Key);
    }

    private void OnCosmicSubtleMarkRemoved(Entity<CosmicSubtleMarkComponent> uid, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.RemoveLayer((uid, sprite), CosmicRevealedKey.Key);
    }

    private void OnCosmicImpositionRemoved(Entity<CosmicImposingComponent> uid, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.RemoveLayer((uid, sprite), CosmicImposingKey.Key);
    }

    private void OnBlankRemoved(Entity<CosmicMalignEchoComponent> uid, ref ComponentShutdown args)
    {
        if (TryComp<SSDIndicatorComponent>(ent, out var comp))
            comp.HideIcon = false;
    }
    #endregion

    #region Icons
    private void GetCosmicCultIcon(Entity<CosmicCultistComponent> ent, ref GetStatusIconsEvent args)
    {
        args.StatusIcons.Add(StatusIcon);
    }


    private void GetCosmicSSDIcon(Entity<CosmicBlankComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
    #endregion

    #region Mark updates
    private void UpdateSubtleMarkVisibility(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !_sprite.LayerMapTryGet((uid, sprite), CosmicRevealedKey.Key, out var layer, logMissing: false)) return;
        if (!TryComp<CosmicSubtleMarkComponent>(uid, out var markComp)) return;
        var ev = new SeeIdentityAttemptEvent();
        RaiseLocalEvent(uid, ev);
        var eyesCovered = ev.TotalCoverage.HasFlag(IdentityBlockerCoverage.EYES);
        _sprite.LayerSetVisible((uid, sprite), layer, !eyesCovered);
    }
    #endregion
}

public enum CultSiphonedVisuals : byte
{
    Key
}
