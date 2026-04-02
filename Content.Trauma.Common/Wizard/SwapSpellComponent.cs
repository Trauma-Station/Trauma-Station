// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Common.Wizard;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SwapSpellComponent : Component
{
    [DataField]
    public bool AllowSecondaryTarget = true;

    [DataField, AutoNetworkedField]
    public EntityUid? SecondaryTarget;
}
