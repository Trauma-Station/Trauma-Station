// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Wraith.SaltLines;

[RegisterComponent, NetworkedComponent]
public sealed partial class SaltLinePlacerComponent : Component
{
    [DataField]
    public EntProtoId SaltLine = "SaltLine";
}
