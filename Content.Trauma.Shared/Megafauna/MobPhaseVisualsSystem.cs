// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.MobPhases;

namespace Content.Trauma.Shared.Megafauna;

public sealed partial class MobPhaseVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnPhaseChange(Entity<MobPhaseVisualsComponent> ent, ref MobPhaseChangedEvent args)
    {
        _appearance.SetData(ent, MobPhaseVisuals.Key, args.Phase);
    }
}
