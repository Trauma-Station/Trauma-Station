// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Client.Shaders;
using Content.Goobstation.Common.Shaders;
using Content.Trauma.Client.Heretic.SpriteOverlay;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Systems.Side;
using Content.Trauma.Shared.Wizard.Traps;
using Robust.Client.GameObjects;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed class ShadowCloakSystem : SharedShadowCloakSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowCloakedComponent, SpriteOverlayUpdatedEvent<EntropicPlumeAffectedComponent>>(UpdateOverlay);
        SubscribeLocalEvent<ShadowCloakedComponent, SpriteOverlayUpdatedEvent<FireBlastedComponent>>(UpdateOverlay);
        SubscribeLocalEvent<ShadowCloakedComponent, SpriteOverlayUpdatedEvent<HereticCombatMarkComponent>>(UpdateOverlay);
        SubscribeLocalEvent<ShadowCloakedComponent, SpriteOverlayUpdatedEvent<HereticEyeOverlayComponent>>(UpdateOverlay);
        SubscribeLocalEvent<ShadowCloakedComponent, SpriteOverlayUpdatedEvent<IceCubeComponent>>(UpdateOverlay);
        SubscribeLocalEvent<ShadowCloakedComponent, SpriteOverlayUpdatedEvent<SacramentsOfPowerComponent>>(UpdateOverlay);
        SubscribeLocalEvent<ShadowCloakedComponent, SpriteOverlayUpdatedEvent<StarMarkComponent>>(UpdateOverlay);
        SubscribeLocalEvent<ShadowCloakedComponent, SpriteOverlayUpdatedEvent<VoidCurseComponent>>(UpdateOverlay);
        SubscribeLocalEvent<ShadowCloakedComponent, SpriteOverlayUpdatedEvent<HereticArenaParticipantComponent>>(UpdateOverlay);

        SubscribeLocalEvent<ShadowCloakedComponent, SetMultiShaderEvent>(OnShader);
        SubscribeLocalEvent<ShadowCloakedComponent, SetMultiShadersEvent>(OnShaders);

        SubscribeLocalEvent<ShadowCloakEntityComponent, ComponentStartup>(OnEntityStartup);
        SubscribeLocalEvent<ShadowCloakEntityComponent, BeforePostMultiShaderRenderEvent>(RelayShader);
    }

    private void RelayShader(Entity<ShadowCloakEntityComponent> ent, ref BeforePostMultiShaderRenderEvent args)
    {
        if (!Exists(ent.Comp.User))
            return;

        RaiseLocalEvent(ent.Comp.User.Value, ref args);
    }

    private void OnEntityStartup(Entity<ShadowCloakEntityComponent> ent, ref ComponentStartup args)
    {
        if (!Exists(ent.Comp.User))
            return;

        // Update visual appearance - overlay layers and shaders
        if (TryComp(ent.Comp.User.Value, out SpriteComponent? sprite))
            _appearance.OnChangeData(ent.Comp.User.Value, sprite);

        var ev = new GetMultiShadersEvent();
        RaiseLocalEvent(ent.Comp.User.Value, ref ev);
        if (ev.PostShaders == null)
            return;

        var setEv = new SetMultiShadersEvent(ev.PostShaders, true);
        RaiseLocalEvent(ent, ref setEv);
    }

    private void OnShader(Entity<ShadowCloakedComponent> ent, ref SetMultiShaderEvent args)
    {
        if (GetShadowCloakEntity(ent) is not { } cloak)
            return;

        RaiseLocalEvent(cloak, ref args);
    }

    private void OnShaders(Entity<ShadowCloakedComponent> ent, ref SetMultiShadersEvent args)
    {
        if (GetShadowCloakEntity(ent) is not { } cloak)
            return;

        RaiseLocalEvent(cloak, ref args);
    }

    private void UpdateOverlay<T>(Entity<ShadowCloakedComponent> ent, ref SpriteOverlayUpdatedEvent<T> args)
        where T : BaseSpriteOverlayComponent
    {
        if (GetShadowCloakEntity(ent) is not { } cloak)
            return;

        if (args.Added)
            args.Sys.AddOverlay(cloak.Owner, args.Comp, ent);
        else
            args.Sys.RemoveOverlay(cloak.Owner, args.Comp);
    }

    protected override void Startup(Entity<ShadowCloakedComponent> ent)
    {
        base.Startup(ent);

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        ent.Comp.WasVisible = sprite.Visible;
        _sprite.SetVisible((ent, sprite), false);
    }

    protected override void Shutdown(Entity<ShadowCloakedComponent> ent)
    {
        base.Shutdown(ent);

        if (TryComp(ent, out SpriteComponent? sprite))
            _sprite.SetVisible((ent, sprite), ent.Comp.WasVisible);
    }
}
