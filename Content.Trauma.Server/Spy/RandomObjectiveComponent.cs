// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.RandomMetadata;
using Content.Shared.Dataset;

namespace Content.Trauma.Server.Spy;

/// <summary>
/// <see cref="RandomMetadataComponent"/> but more random and for objectives
/// Only affects objective name
/// </summary>
[RegisterComponent]
public sealed partial class RandomObjectiveComponent : Component
{
    [DataField]
    public List<ProtoId<LocalizedDatasetPrototype>> NameSegments = new();

    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> NameFormats;

    [DataField]
    public string PickedFormat = string.Empty;
}
