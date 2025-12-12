// SPDX-FileCopyrightText: 2024 Magnus Larsen <i.am.larsenml@gmail.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Botany.Components;

[NetworkedComponent]
public abstract partial class SharedSeedComponent : Component
{
}

[Serializable, NetSerializable]
public partial struct SeedChemQuantityAlternate
{
    /// <summary>
    /// Minimum amount of chemical that is added to produce, regardless of the potency
    /// </summary>
    [DataField("Min")] public int Min = 0;

    /// <summary>
    /// Maximum amount of chemical that can be produced after taking plant potency into account.
    /// </summary>
    [DataField("Max")] public int Max = 5;

    /// <summary>
    /// When chemicals are added to produce, the potency of the seed is divided with this value. Final chemical amount is the result plus the `Min` value.
    /// Example: PotencyDivisor of 20 with seed potency of 55 results in 2.75, 55/20 = 2.75. If minimum is 1 then final result will be 3.75 of that chemical, 55/20+1 = 3.75.
    /// </summary>
    [DataField("PotencyDivisor")] public int PotencyDivisor = 20;

    /// <summary>
    /// Inherent chemical is one that is NOT result of mutation or crossbreeding. These chemicals are removed if species mutation is executed.
    /// </summary>
    [DataField("Inherent")] public bool Inherent = true;

    public SeedChemQuantityAlternate(int min, int max, int potencyDivisor, bool inherent)
    {
        Min = min;
        Max = max;
        PotencyDivisor = potencyDivisor;
        Inherent = inherent;
    }
}
