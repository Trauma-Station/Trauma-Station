// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Common.Wizard;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpellCardsActionComponent : Component
{
    /// <summary>
    /// Whether the next spell card burst will be purple or red
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PurpleCard;
}
