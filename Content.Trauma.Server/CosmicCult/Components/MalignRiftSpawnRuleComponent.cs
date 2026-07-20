// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Server.CosmicCult.Components;

[RegisterComponent, Access(typeof(MalignRiftSpawnRule))]
public sealed partial class MalignRiftSpawnRuleComponent : Component
{
    [DataField] public EntProtoId MalignRift = "CosmicMalignRift";
    [DataField] public SoundSpecifier Tier2Sound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/tier1.ogg");
}
