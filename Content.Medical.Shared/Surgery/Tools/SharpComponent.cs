// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Medical.Shared.Surgery.Tools;

/// <summary>
/// Given to sharp things that gives them shitty default scalpel and bone saw surgery qualities while added.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SharpComponent : Component
{
    /// <summary>
    /// Whether this item had <c>SurgeryToolComponent</c> before sharp was added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HadSurgeryTool;

    /// <summary>
    /// Whether this item had <c>ScalpelComponent</c> before sharp was added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HadScalpel;

    /// <summary>
    /// Whether this item had <c>BoneSawComponent</c> before sharp was added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HadBoneSaw;
}
