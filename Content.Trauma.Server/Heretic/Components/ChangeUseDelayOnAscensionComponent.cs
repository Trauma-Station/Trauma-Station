// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components;

namespace Content.Trauma.Server.Heretic.Components;

[RegisterComponent]
public sealed partial class ChangeUseDelayOnAscensionComponent : Component
{
    [DataField(required: true)]
    public TimeSpan NewUseDelay;

    [DataField]
    public HereticPath? RequiredPath;
}
