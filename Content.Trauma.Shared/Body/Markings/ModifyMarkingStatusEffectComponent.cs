// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Humanoid;

namespace Content.Trauma.Shared.Body.Markings;

/// <summary>
/// Toggles some organ marking to use another marking
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModifyMarkingStatusEffectComponent : Component
{
    [DataField(required: true)]
    public HumanoidVisualLayers Layer;

    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Organ;

    /// <summary>
    /// Suffix of "Toggled" version of the marking id
    /// So if our id is "MothWingsDefault" and suffix is "Open", we toggle it and use marking with id "MothWingsDefaultOpen"
    /// </summary>
    [DataField(required: true)]
    public string Suffix;

    /// <summary>
    /// newmarking -> oldmarking ids saved when toggled for easier revert
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, string> CachedMarkings = new();
}
