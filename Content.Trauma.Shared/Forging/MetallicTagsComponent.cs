// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// Adds/removes tags to the entity based on its workability.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMetalSystem))]
[AutoGenerateComponentState]
public sealed partial class MetallicTagsComponent : Component
{
    /// <summary>
    /// Tags to add while workable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> Workable = new();

    /// <summary>
    /// Tags to remove while unworkable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> Unworkable = new();
}
