// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Lavaland.Shared.Megafauna.NumberSelectors;

/// <summary>
/// Gives a constant value.
/// </summary>
public sealed partial class MegafaunaConstantNumberSelector : MegafaunaNumberSelector
{
    public MegafaunaConstantNumberSelector(float value)
    {
        Value = value;
    }

    public override float Get(MegafaunaCalculationBaseArgs args)
    {
        return Value;
    }
}
