// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MetabolismModifierComponent : Component
{
    [DataField(required: true)]
    public float Modifier;
}
