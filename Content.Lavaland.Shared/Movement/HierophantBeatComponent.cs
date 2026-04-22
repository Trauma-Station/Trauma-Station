// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;

namespace Content.Lavaland.Shared.Movement;

[RegisterComponent, NetworkedComponent]
public sealed partial class HierophantBeatComponent : Component
{
    [DataField]
    public float MovementSpeedBuff = 1.25f;

    [DataField]
    public ProtoId<AlertPrototype> HierophantBeatAlertId = "HierophantBeat";
}
