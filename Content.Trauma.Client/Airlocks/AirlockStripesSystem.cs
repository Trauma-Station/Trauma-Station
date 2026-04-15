using Content.Client.Doors;
using Content.Shared.Doors.Components;
using Content.Trauma.Common.Airlocks;
using Content.Trauma.Shared.Airlocks;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Trauma.Client.Airlocks;

public sealed class AirlockStripesSystem : SharedAirlockStripesSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpriteComponent, UpdateDoorSpritesEvent>(OnSpriteUpdate);

        SubscribeLocalEvent<AirlockStripesComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AirlockStripesComponent, AppearanceChangeEvent>(OnAppearanceChange,
            after: new[] { typeof(DoorSystem) });
    }

    private void OnInit(Entity<AirlockStripesComponent> ent, ref ComponentInit args)
    {
        if (!TryComp(ent, out DoorComponent? door))
            return;

        ent.Comp.OpeningAnimation = new Animation
        {
            Length = door.OpeningAnimationTime,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = AirlockStripesLayers.Stripes,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.OpeningSpriteState, 0f),
                    },
                },
            },
        };

        ent.Comp.ClosingAnimation = new Animation
        {
            Length = door.ClosingAnimationTime,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = AirlockStripesLayers.Stripes,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.ClosingSpriteState, 0f),
                    },
                },
            },
        };
    }

    private void OnAppearanceChange(Entity<AirlockStripesComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!Appearance.TryGetData<DoorState>(ent, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        if (_animation.HasRunningAnimation(ent, AirlockStripesComponent.AnimationKey))
            _animation.Stop(ent.Owner, AirlockStripesComponent.AnimationKey);

        UpdateStripesAppearance((ent, args.Sprite, null, ent.Comp), state);
    }

    private void OnSpriteUpdate(Entity<SpriteComponent> ent, ref UpdateDoorSpritesEvent args)
    {
        if (!_sprite.LayerMapTryGet(ent.AsNullable(), AirlockStripesLayers.Stripes, out var index, false) ||
            !args.Proto.TryGetComponent(out AirlockStripesComponent? stripes, Factory))
            return;

        _sprite.LayerSetColor(ent.AsNullable(), index, stripes.Color);
        args.Handled = true;
    }

    private void UpdateStripesAppearance(Entity<SpriteComponent?, DoorComponent?, AirlockStripesComponent> ent, DoorState state)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false) ||
            !_sprite.LayerMapTryGet(ent, AirlockStripesLayers.Stripes, out var index, false))
            return;

        switch (state)
        {
            case DoorState.Open:
                _sprite.LayerSetVisible(ent, index, false);

                return;
            case DoorState.Closed:
                _sprite.LayerSetVisible(ent, index, true);
                _sprite.LayerSetRsiState(ent, index, ent.Comp3.ClosedSpriteState);

                return;
            case DoorState.Opening:
                if (ent.Comp2.OpeningAnimationTime == TimeSpan.Zero)
                    return;

                _sprite.LayerSetVisible(ent, index, true);
                _animation.Play(ent, (Animation)ent.Comp3.OpeningAnimation, AirlockStripesComponent.AnimationKey);

                return;
            case DoorState.Closing:
                if (ent.Comp2.ClosingAnimationTime == TimeSpan.Zero || ent.Comp2.CurrentlyCrushing.Count != 0)
                    return;

                _sprite.LayerSetVisible(ent, index, true);
                _animation.Play(ent, (Animation)ent.Comp3.ClosingAnimation, AirlockStripesComponent.AnimationKey);

                return;
        }
    }
}
