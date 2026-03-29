// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicEffigyConditionComponent : Component
{
    [DataField]
    public EntityUid? EffigyTarget;

    [DataField]
    public EntityWhitelist? Blacklist;
}
