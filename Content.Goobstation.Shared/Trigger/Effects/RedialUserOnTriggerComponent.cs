// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Trigger.Effects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RedialUserOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public string Address = string.Empty;
}
