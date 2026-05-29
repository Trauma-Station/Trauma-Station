// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.VendingMachines;
using Content.Shared.VendingMachines;
using Robust.Client.Animations;

namespace Content.Trauma.Client.VendingMachines;

public sealed partial class ShopVendorSystem : SharedShopVendorSystem
{
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShopVendorComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<ShopVendorComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    // copied from vending machines because its not reusable in other systems :)
    private void OnAnimationCompleted(Entity<ShopVendorComponent> ent, ref AnimationCompletedEvent args)
    {
        UpdateAppearance((ent, ent.Comp));
    }

    private void OnAppearanceChange(Entity<ShopVendorComponent> ent, ref AppearanceChangeEvent args)
    {
        UpdateAppearance((ent, ent.Comp, args.Sprite));
    }

    private void UpdateAppearance(Entity<ShopVendorComponent, SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        if (!_appearance.TryGetData<VendingMachineVisualState>(ent, VendingMachineVisuals.VisualState, out var state))
            state = VendingMachineVisualState.Normal;

        var sprite = (ent.Owner, ent.Comp2);
        SetLayerState(sprite, VendingMachineVisualLayers.Base, ent.Comp1.OffState);
        SetLayerState(sprite, VendingMachineVisualLayers.Screen, ent.Comp1.ScreenState);
        switch (state)
        {
            case VendingMachineVisualState.Normal:
                SetLayerState(sprite, VendingMachineVisualLayers.BaseUnshaded, ent.Comp1.NormalState);
                break;

            case VendingMachineVisualState.Deny:
                if (ent.Comp1.LoopDenyAnimation)
                    SetLayerState(sprite, VendingMachineVisualLayers.BaseUnshaded, ent.Comp1.DenyState);
                else
                    PlayAnimation(sprite, VendingMachineVisualLayers.BaseUnshaded, ent.Comp1.DenyState, ent.Comp1.DenyDelay);
                break;

            case VendingMachineVisualState.Eject:
                PlayAnimation(sprite, VendingMachineVisualLayers.BaseUnshaded, ent.Comp1.EjectState, ent.Comp1.EjectDelay);
                break;

            case VendingMachineVisualState.Broken:
                HideLayers(sprite);
                SetLayerState(sprite, VendingMachineVisualLayers.Base, ent.Comp1.BrokenState);
                break;

            case VendingMachineVisualState.Off:
                HideLayers(sprite);
                break;
        }
    }

    private void SetLayerState(Entity<SpriteComponent?> sprite, VendingMachineVisualLayers key, string? state)
    {
        if (state == null || !_sprite.TryGetLayer(sprite, key, out var layer, true))
            return;

        _sprite.LayerSetVisible(layer, true);
        _sprite.LayerSetAutoAnimated(layer, true);
        _sprite.LayerSetRsiState(layer, state);
    }

    private void PlayAnimation(Entity<SpriteComponent?> sprite, VendingMachineVisualLayers key, string? state, TimeSpan time)
    {
        if (state == null || _animationPlayer.HasRunningAnimation(sprite.Owner, state) ||
            !_sprite.TryGetLayer(sprite, key, out var layer, true))
            return;

        var animation = GetAnimation(key, state, time);
        _sprite.LayerSetVisible(layer, true);
        _animationPlayer.Play(sprite.Owner, animation, state);
    }

    private static Animation GetAnimation(VendingMachineVisualLayers layer, string state, TimeSpan time)
    {
        return new Animation
        {
            Length = time,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = layer,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(state, 0f)
                    }
                }
            }
        };
    }

    private void HideLayers(Entity<SpriteComponent?> ent)
    {
        HideLayer(ent, VendingMachineVisualLayers.BaseUnshaded);
        HideLayer(ent, VendingMachineVisualLayers.Screen);
    }

    private void HideLayer(Entity<SpriteComponent?> ent, VendingMachineVisualLayers key)
    {
        if (_sprite.TryGetLayer(ent, key, out var layer, true))
            _sprite.LayerSetVisible(layer, false);
    }
}
