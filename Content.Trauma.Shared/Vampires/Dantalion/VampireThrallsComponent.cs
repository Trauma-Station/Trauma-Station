// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Trauma.Shared.Vampires.Dantalion;

/// <summary>
/// Component that holds a list of the thralls this vampire has.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VampireThrallsComponent : Component
{
    /// <summary>
    /// The thralls we currently own.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Thralls = new();

    /// <summary>
    /// How many thralls we are currently allowed to own.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ThrallCap = 1;
}

public sealed partial class DanEnthrallActionEvent : EntityTargetActionEvent;
