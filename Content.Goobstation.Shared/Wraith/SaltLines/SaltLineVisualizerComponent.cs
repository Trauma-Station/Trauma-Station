// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Wraith.SaltLines;

[RegisterComponent]
public sealed partial class SaltLineVisualizerComponent : Component
{
    [DataField]
    public string State = "salt_";
}
