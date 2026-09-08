// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.MartialArts.Components;

namespace Content.Trauma.Shared.MartialArts;

/// <summary>
/// Raised on the performer to calculate the multiplier for a <see cref="ComboAttackModifier"/>.
/// </summary>
[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class BaseComboMultiplierEvent : EntityEventArgs
{
    public EntityUid User = EntityUid.Invalid;

    public float Multiplier = 1f;

    public virtual void Reset(EntityUid user)
    {
        User = user;
        Multiplier = 1f;
    }
}

public sealed partial class FlatMultiplierEvent : BaseComboMultiplierEvent
{
    [DataField]
    public float Value = 1f;

    public override void Reset(EntityUid user)
    {
        base.Reset(user);
        Multiplier = Value;
    }
}

/// <summary>
/// A multiplier scaling with how fast the user is moving.
/// </summary>
public sealed partial class VelocityMultiplierEvent : BaseComboMultiplierEvent
{
    [DataField]
    public float Exponent = 0.2f;

    [DataField]
    public float Min = 1f;

    [DataField]
    public float Max = 1.5f;
}
