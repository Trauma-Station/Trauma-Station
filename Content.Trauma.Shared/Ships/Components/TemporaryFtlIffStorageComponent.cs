// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Shuttles.Components;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Ships.Components;

/// <summary>
/// Temporarily stores the original IFF flags of a ship.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TemporaryFtlIffStorageComponent : Component
{
    /// <summary>
    /// The IFF flags that were active before FTL travel began.
    /// </summary>
    [DataField, AutoNetworkedField]
    public IFFFlags OriginalFlags;
}
