// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Humanoid;
using Content.Shared.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.StatusEffects;

public sealed partial class HysteriaOverlay : Overlay
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;
    private readonly EntityLookupSystem _lookup;
    private EntityQuery<SpriteComponent> _spriteQuery;

    /// <summary>
    /// Attaches an entity to a random entity prototype, and draws the sprite of the entity prototype on top of the entity.
    /// </summary>
    private readonly Dictionary<EntityUid, EntProtoId> _entityDisguise = new();

    /// <summary>
    /// The entities that are currently hidden by the overlay and masked with fake disguises.
    /// </summary>
    private readonly HashSet<EntityUid> _hiddenEntities = new();

    /// <summary>
    /// The entities that are near us.
    /// </summary>
    private HashSet<EntityUid> _nearbyEntities = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    /// <summary>
    /// A disguise is just a fake texture, taken from an existing entity prototype, and applied to another entity,
    /// in order to create the hysteria illusion.
    ///
    /// Of course, it is not that advanced because it doesn't include RSI directions, however it calculates for rotations like camera.
    /// TODO: Support rsi directions
    /// </summary>
    public List<EntProtoId>? Disguises = new();

    /// <summary>
    /// The range to look for nearby entities.
    /// </summary>
    private readonly float _lookupRange = 15f;

    public HysteriaOverlay()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _entMan.System<SpriteSystem>();
        _transform = _entMan.System<SharedTransformSystem>();
        _lookup = _entMan.System<EntityLookupSystem>();

        _spriteQuery = _entMan.GetEntityQuery<SpriteComponent>();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var player = _player.LocalEntity;
        if (player is null)
            return;

        var xform = _entMan.GetComponent<TransformComponent>(player.Value);

        _nearbyEntities.Clear();
        _lookup.GetEntitiesInRange(xform.Coordinates, _lookupRange, _nearbyEntities);

        foreach (var uid in _hiddenEntities)
        {
            if (!_nearbyEntities.Contains(uid))
            {
                if (_spriteQuery.TryComp(uid, out var sprite))
                    _sprite.SetVisible((uid, sprite), true);
            }
        }

        _hiddenEntities.IntersectWith(_nearbyEntities);
        foreach (var uid in _nearbyEntities)
        {
            if (player == uid || !_entMan.HasComponent<HumanoidProfileComponent>(uid))
                continue;

            if (_spriteQuery.TryComp(uid, out var sprite) && sprite.Visible)
            {
                _sprite.SetVisible((uid, sprite), false);
                _hiddenEntities.Add(uid);
            }
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _player.LocalEntity;
        if (player is null)
            return;

        var handle = args.WorldHandle;

        foreach (var uid in _hiddenEntities)
        {
            if (!_entityDisguise.TryGetValue(uid, out var disguiseProto))
            {
                if (Disguises == null || Disguises.Count == 0)
                    continue;

                disguiseProto = _random.Pick(Disguises);
                _entityDisguise[uid] = disguiseProto;
            }

            var xform = _entMan.GetComponent<TransformComponent>(uid);
            var worldPos = _transform.GetWorldPosition(xform);
            var eyeRot = args.Viewport.Eye?.Rotation ?? Angle.Zero;

            if (!_proto.TryIndex(disguiseProto, out var disguise)
                || !disguise.HasComponent<SpriteComponent>())
                continue;

            var disguiseTexture = _sprite.Frame0(disguise);

            var halfWidth = disguiseTexture.Width / 2f / EyeManager.PixelsPerMeter;
            var halfHeight = disguiseTexture.Height / 2f / EyeManager.PixelsPerMeter;
            var box = new Box2(-halfWidth, -halfHeight, halfWidth, halfHeight);

            var matrix = Matrix3x2.CreateRotation((float)-eyeRot.Theta) * Matrix3x2.CreateTranslation(worldPos);
            handle.SetTransform(matrix);
            handle.DrawTextureRect(disguiseTexture, box);

            handle.SetTransform(Matrix3x2.Identity);
        }
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entMan.TryGetComponent(_player.LocalEntity, out EyeComponent? eyeComp))
            return false;

        return args.Viewport.Eye == eyeComp.Eye;
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();
        RevertHiddenSprites();
    }

    /// <summary>
    /// Used by status effect system to revert the hidden sprites.
    /// </summary>
    public void RevertHiddenSprites()
    {
        foreach (var uid in _hiddenEntities)
        {
            if (_spriteQuery.TryComp(uid, out var sprite))
                _sprite.SetVisible((uid, sprite), true);
        }

        _hiddenEntities.Clear();
        _entityDisguise.Clear();
    }
}
