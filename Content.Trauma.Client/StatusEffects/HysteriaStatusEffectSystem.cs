// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;
using Content.Trauma.Shared.StatusEffects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Trauma.Client.StatusEffects;

public sealed partial class HysteriaStatusEffectSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private HysteriaOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HysteriaStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<HysteriaStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);

        SubscribeLocalEvent<HysteriaStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerAttachedEvent>>(OnStatusEffectPlayerAttached);
        SubscribeLocalEvent<HysteriaStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerDetachedEvent>>(OnStatusEffectPlayerDetached);

        _overlay = new();
    }

    private void OnApplied(Entity<HysteriaStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity == args.Target)
        {
            _overlay.Disguises = ent.Comp.Disguises;
            _overlayMan.AddOverlay(_overlay);
        }
    }

    private void OnRemoved(Entity<HysteriaStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        if (!_statusEffects.HasEffectComp<HysteriaStatusEffectComponent>(_player.LocalEntity.Value))
        {
            _overlay.RevertHiddenSprites();

            _overlay.Disguises = null;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    private void OnStatusEffectPlayerAttached(Entity<HysteriaStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnStatusEffectPlayerDetached(Entity<HysteriaStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
    {
        if (_player.LocalEntity is null)
            return;

        _overlay.RevertHiddenSprites();

        if (!_statusEffects.HasEffectComp<HysteriaStatusEffectComponent>(_player.LocalEntity.Value))
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
