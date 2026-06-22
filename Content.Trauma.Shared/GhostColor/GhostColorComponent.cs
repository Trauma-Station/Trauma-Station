// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.GhostColor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GhostColorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color? Color;
}
