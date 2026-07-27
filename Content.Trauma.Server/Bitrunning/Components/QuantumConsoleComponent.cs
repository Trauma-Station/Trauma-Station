// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.Bitrunning.Systems;

namespace Content.Trauma.Server.Bitrunning.Components;

[RegisterComponent]
public sealed partial class QuantumConsoleComponent : Component
{
    [Access(typeof(QuantumConsoleSystem))]
    public EntityUid? LinkedServerId;
}
