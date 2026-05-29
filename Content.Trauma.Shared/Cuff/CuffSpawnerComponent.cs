// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Cuff;

/// <summary>
/// Arrests somebody and spawns cuffs when doing so.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CuffSpawnerComponent : Component
{
    [DataField]
    public EntProtoId HandcuffId = "Handcuffs";
}
