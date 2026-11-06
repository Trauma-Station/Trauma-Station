// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Interaction;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that rotates a target to a given angle
/// </summary>
public sealed partial class Rotate : EntityEffectBase<Rotate>
{
    /// <summary>
    /// The angle to rotate to
    /// </summary>
    [DataField(required: true)]
    public Angle Angle;

    /// <summary>
    /// What type of operation to do
    /// </summary>
    [DataField]
    public RotateToFace Type = RotateToFace.Set;

    /// <summary>
    /// Whether to force the rotation, without checking action blockers
    /// </summary>
    [DataField]
    public bool Force;
}

public sealed partial class RotateEffectSystem : EntityEffectSystem<TransformComponent, Rotate>
{
    [Dependency] private RotateToFaceSystem _rotate = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<Rotate> args)
    {
        var effect = args.Effect;
        var type = effect.Type;
        var angle = effect.Angle;
        var force = effect.Force;

        switch (type)
        {
            case RotateToFace.Set:
                if (force)
                {
                    _transform.SetWorldRotation(entity.Comp, angle);
                    break;
                }

                _rotate.TryFaceAngle(entity, angle, entity.Comp);
                break;
            case RotateToFace.Adjust:
                var worldRot = _transform.GetWorldRotation(entity.Comp);
                if (force)
                {
                    _transform.SetWorldRotation(entity.Comp, worldRot + angle);
                    break;
                }

                _rotate.TryFaceAngle(entity, worldRot + angle, entity.Comp);
                break;
        }
    }
}

public enum RotateToFace : byte
{
    Set,
    Adjust,
}
