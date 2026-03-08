// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Areas;

/// <summary>
/// Prevents using this martial art unless you are in a whitelisted area.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MartialArtAreaSystem))]
public sealed partial class AreaMartialArtComponent : Component
{
    /// <summary>
    /// Areas which allow this art to be used.
    /// </summary>
    [DataField(required: true)]
    public HashSet<EntProtoId> Areas = new();
}
