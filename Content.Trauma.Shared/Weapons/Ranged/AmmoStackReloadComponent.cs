// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Stacks;

namespace Content.Trauma.Shared.Weapons.Ranged;

/// <summary>
/// Allows a <c>BasicEntityAmmoProvider</c> to be reloaded by clicking it with a stack.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AmmoStackReloadComponent : Component
{
    /// <summary>
    /// Whitelist for stacks that can be used.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<StackPrototype>> Whitelist = new();
}
