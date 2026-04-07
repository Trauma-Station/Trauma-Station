// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent]
public sealed partial class StarBlastComponent : Component
{
    [DataField]
    public float StarMarkRadius = 3f;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(4);

    [DataField]
    public EntityUid Action;
}
