// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.FireControl;

[RegisterComponent]
public sealed partial class FireControlServerComponent : Component
{
    [ViewVariables]
    public EntityUid? ConnectedGrid = null;

    [ViewVariables]
    public HashSet<EntityUid> Controlled = new();

    [ViewVariables]
    public HashSet<EntityUid> Consoles = new();

    [ViewVariables, DataField]
    public int ProcessingPower;

    [ViewVariables]
    public int UsedProcessingPower;
}
