// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.NTR.Scan
{
    [RegisterComponent]
    public sealed partial class BriefcaseScannerComponent : Component
    {
        [DataField]
        public float ScanDuration = 10f;
    }
}
