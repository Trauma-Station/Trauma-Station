// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Traumas;
using Content.Medical.Common.Wounds;
using Content.Shared.FixedPoint;

namespace Content.Medical.Shared.Traumas;

public partial class TraumaSystem
{
    #region Data

    private readonly Dictionary<BoneSeverity, FixedPoint2> _boneThresholds = new()
    {
        { BoneSeverity.Normal, 40 },
        { BoneSeverity.Damaged, 25 },
        { BoneSeverity.Cracked, 10 },
        { BoneSeverity.Broken, 0 },
    };

    private readonly Dictionary<WoundableSeverity, float> _boneTraumaChanceMultipliers = new()
    {
        { WoundableSeverity.Healthy, 0f },
        { WoundableSeverity.Minor, 0f },
        { WoundableSeverity.Moderate, 0.03f },
        { WoundableSeverity.Severe, 0.1f },
        { WoundableSeverity.Critical, 0.16f },
        { WoundableSeverity.Mangled, 0.21f },
        { WoundableSeverity.Severed, 0f },
    };

    private readonly Dictionary<WoundableSeverity, float> _organTraumaChanceMultipliers = new()
    {
        { WoundableSeverity.Healthy, 0f },
        { WoundableSeverity.Minor, 0f },
        { WoundableSeverity.Moderate, 0.1f },
        { WoundableSeverity.Severe, 0.4f },
        { WoundableSeverity.Critical, 0.7f },
        { WoundableSeverity.Mangled, 1f },
        { WoundableSeverity.Severed, 0f },
    };

    #endregion
}
