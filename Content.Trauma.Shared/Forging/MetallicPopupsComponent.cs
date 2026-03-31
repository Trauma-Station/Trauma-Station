// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// Shows popups when a metal changes workable state from heating/cooling.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMetalSystem))]
public sealed partial class MetallicPopupsComponent : Component
{
    [DataField(required: true)]
    public LocId HeatedPopup;

    [DataField(required: true)]
    public LocId CooledPopup;
}
