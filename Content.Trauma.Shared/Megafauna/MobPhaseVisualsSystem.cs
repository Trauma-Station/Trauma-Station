// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.MobPhases;

namespace Content.Trauma.Shared.Megafauna;

public sealed partial class MobPhaseVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [SubscribeLocalEvent]
    private void OnPhaseChange(Entity<MobPhaseVisualsComponent> ent, ref MobPhaseChangedEvent args)
    {
        if (!ent.Comp.Phases.Contains(args.Phase))
            return;

        _appearance.SetData(ent, MobPhaseVisuals.Key, args.Phase);
    }
}

[Serializable, NetSerializable]
public enum MobPhaseVisuals : byte
{
    Key,
    Layer,
}
