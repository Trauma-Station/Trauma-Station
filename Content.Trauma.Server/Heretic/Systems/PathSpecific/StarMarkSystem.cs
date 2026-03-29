// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class StarMarkSystem : SharedStarMarkSystem
{
    [Dependency] private readonly AirtightSystem _airtight = default!;

    protected override void InitializeCosmicField(Entity<CosmicFieldComponent> field, int strength)
    {
        base.InitializeCosmicField(field, strength);

        if (strength < 7) // Cosmic blade level
            return;

        var airtight = EnsureComp<AirtightComponent>(field);
        airtight.BlockExplosions = true;
        _airtight.UpdatePosition((field.Owner, airtight));
    }
}
