// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Preferences.Loadouts;

public sealed partial class LoadoutGroupPrototype
{
    /// <summary>
    /// If true, hides locked loadouts instead of greying them out.
    /// </summary>
    [DataField]
    public bool HideLocked;
}
