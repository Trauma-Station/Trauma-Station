// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Wizard;

[RegisterComponent, NetworkedComponent]
public sealed partial class LockOnMarkActionComponent : Component
{
    [DataField]
    public float LockOnRadius = 3f;

    [DataField]
    public EntityUid? Target;

    [DataField]
    public EntityUid? Mark;
}
