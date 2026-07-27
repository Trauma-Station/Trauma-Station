// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Bitrunning.Components;

[RegisterComponent]
public sealed partial class ByteforgeComponent : Component
{
    public EntityUid? LinkedServer;

    public int VisualPulseSerial;
}
