// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

namespace Content.Goobstation.Shared.Virology;

[RegisterComponent]
public sealed partial class ActiveVirologyMachineComponent : Component
{
    [ViewVariables]
    public TimeSpan EndTime;
}
