// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.Kitchen.Components;

public sealed partial class MicrowaveComponent
{
    /// <summary>
    /// Stores the last user of the microwave for CookedFoodEvent.
    /// </summary>
    [DataField]
    public EntityUid? LastUser;
}
