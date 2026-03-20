// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.NTR.Scan;

[RegisterComponent]
public sealed partial class ScannableForPointsComponent : Component
{
    [DataField]
    public int Points = 5;

    [DataField]
    public bool AlreadyScanned;
}
