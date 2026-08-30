// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AntiTamper;

[Flags]
public enum AntiTamperAlertType : byte
{
    None = 0,
    OnDamaged = 1 << 1,
    OnDestroyed = 1 << 2,
    All = OnDamaged | OnDestroyed
}
