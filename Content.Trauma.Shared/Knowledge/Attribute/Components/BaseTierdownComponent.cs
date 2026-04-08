// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public abstract partial class BaseTierdownComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Mod;
}
