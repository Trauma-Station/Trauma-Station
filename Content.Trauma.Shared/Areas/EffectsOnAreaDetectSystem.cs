// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Areas;

public sealed class EffectsOnAreaDetectSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EffectsOnAreaDetectComponent, AreaDetectorChangedEvent>(OnChanged);
    }

    private void OnChanged(Entity<EffectsOnAreaDetectComponent> ent, ref AreaDetectorChangedEvent args)
    {
        if (args.OldArea is { } oldArea &&
            _area.GetAreaPrototype(oldArea) is { } oldProto &&
            ent.Comp.Areas.Contains(oldProto) &&
            ent.Comp.EffectsOnExit is { } exitEffects)
        {
            _effects.ApplyEffects(ent.Owner, exitEffects);
            Log.Debug("Running exit effects");
        }

        if (args.NewArea is { } newArea &&
            _area.GetAreaPrototype(newArea) is { } newProto &&
            ent.Comp.Areas.Contains(newProto) &&
            ent.Comp.EffectsOnEnter is { } enterEffects)
        {
            _effects.ApplyEffects(ent.Owner, enterEffects);
            Log.Debug("Running enter effects");
        }
    }
}
