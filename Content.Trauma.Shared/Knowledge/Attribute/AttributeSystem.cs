// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute;

/// <summary>
/// This handles all attribute related entities.
/// </summary>
public sealed partial class AttributeSystem : EntitySystem
{
    // [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    /// Every attribute prototype and its data.
    /// </summary>
    public Dictionary<EntProtoId, AttributeComponent> AllAttributes = new();

    /// <summary>
    /// Common lerp used for attributes.
    /// </summary>
    public static int LerpCurve(FixedPoint2 input, FixedPoint2 minX, FixedPoint2 maxX, FixedPoint2 minY, FixedPoint2 maxY)
    {
        var rawY = minY + (input - minX) * (maxY - minY) / (maxX - minX);

        return rawY.Int();
    }

    /// <summary>
    /// Override method for adjusting attribute.
    /// </summary>
    public void AdjustAttribute(Entity<AttributeComponent> attribute, int adjust)
    {
        attribute.Comp.Inherent = AdjustAttribute(attribute.Comp.Inherent, adjust);
    }

    /// <summary>
    /// Adjusted an attribute according to exp shit.
    /// </summary>
    public static FixedPoint2 AdjustAttribute(FixedPoint2 inherent, int adjust)
    {
        FixedPoint2 value = inherent;
        int amount = Math.Abs(adjust);
        int direction = Math.Sign(adjust);

        for (int i = 0; i < amount; i++)
        {
            if (value < 10.00)
                value += direction * 0.10;
            else if (value > 16.00)
                value += direction * 0.03;
            else
                value += direction * 0.05;
        }

        return value;
    }
}
