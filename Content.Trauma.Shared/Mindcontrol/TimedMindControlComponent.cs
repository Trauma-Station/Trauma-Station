// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Mindcontrol;

[RegisterComponent]
public sealed partial class TimedMindControlComponent : Component
{
    [DataField]
    public TimeSpan ExpiresAt;
}
