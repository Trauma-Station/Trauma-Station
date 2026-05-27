// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TransmutationRuneScriberComponent : Component
{
    [DataField]
    public float? Time;

    [DataField]
    public EntProtoId? RuneDrawingEntity;
}
