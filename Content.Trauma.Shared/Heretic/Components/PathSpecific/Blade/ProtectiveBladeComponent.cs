// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;

/// <summary>
///     Indicates that an entity can act as a protective blade.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProtectiveBladeComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid User;
}
