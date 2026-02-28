using System;
using System.Collections.Generic;
using System.Text;
using Content.Shared.Damage;
using Content.Trauma.Common.Knowledge;

namespace Content.Shared.Armor;

/// <summary>
///     This handles logic relating to <see cref="ArmorComponent" />
/// </summary>
public abstract partial class SharedArmorSystem
{
    public DamageModifierSet GetQualityAdjustment(EntityUid target, DamageModifierSet input)
    {
        var newDict = new DamageModifierSet();
        var ev = new InvokeArmorQualityEvent(1.0f);
        RaiseLocalEvent(target, ref ev);
        foreach (var coefficient in input.Coefficients)
        {
            newDict.Coefficients[coefficient.Key] = coefficient.Value * ev.Coefficient;
        }
        return newDict;
    }
}
