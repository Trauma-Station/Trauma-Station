using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared._Shitcode.Heretic.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client._Shitcode.Heretic.SpriteOverlay;

public sealed class SacramentsOverlaySystem : SpriteOverlaySystem<SacramentsOfPowerComponent>
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly ShadowCloakSystem _shadow = default!;

    private const string AnimationKey = "eye_flash";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SacramentsOfPowerComponent, AnimationCompletedEvent>(OnAnimation);
        SubscribeNetworkEvent<SacramentsPulseEvent>(OnPulseEvent);
    }

    public override void RemoveOverlay(Entity<SpriteComponent?> ent, SacramentsOfPowerComponent comp)
    {
        base.RemoveOverlay(ent, comp);

        _animation.Stop(ent.Owner, AnimationKey);
    }

    private void OnAnimation(Entity<SacramentsOfPowerComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey || !TryComp(ent, out SpriteComponent? sprite))
            return;

        _appearance.OnChangeData(ent, sprite);
    }

    protected override void UpdateOverlayLayer(Entity<SpriteComponent> ent,
        SacramentsOfPowerComponent comp,
        int layer,
        EntityUid? source = null)
    {
        base.UpdateOverlayLayer(ent, comp, layer, source);

        var uid = source ?? ent;

        if (!_appearance.TryGetData(uid, SacramentsKey.Key, out SacramentsState state))
            return;

        _animation.Stop(ent.Owner, AnimationKey);

        var spriteState = comp.SpriteStates[state];

        Sprite.LayerSetRsiState(ent.AsNullable(), comp.Key, spriteState);
        Sprite.LayerSetAutoAnimated(ent.AsNullable(), layer, true);

        if (state != SacramentsState.Open && Sprite.TryGetLayer(ent.AsNullable(), layer, out var spriteLayer, true))
            spriteLayer.Loop = false;
    }

    private void OnPulseEvent(SacramentsPulseEvent ev)
    {
        var uid = GetEntity(ev.Entity);
        if (HasComp<ShadowCloakedComponent>(uid) && _shadow.GetShadowCloakEntity(uid) is { } cloak)
            uid = cloak;
        PlayPulseAnimation(uid);
    }

    private void PlayPulseAnimation(EntityUid uid)
    {
        if (_animation.HasRunningAnimation(uid, AnimationKey))
            return;

        var a = GetAnimation();
        _animation.Play(uid, a, AnimationKey);
    }

    private static Animation GetAnimation()
    {
        return new Animation
        {
            Length = TimeSpan.FromMilliseconds(400),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = SacramentsKey.Key,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame("eye_flash", 0f)
                    }
                }
            }
        };
    }
}
