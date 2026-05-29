// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Light.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Content.Trauma.Shared.Light;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Server.Light;

/// <summary>
/// Handles area pickup of bulbs/tubes for light replacers.
/// </summary>
public sealed partial class LightReplacerAreaPickupSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private LightReplacerSystem _replacer = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private HashSet<Entity<LightBulbComponent>> _bulbs = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightReplacerAreaPickupComponent, AfterInteractEvent>(OnAfterInteract,
            before: [ typeof(LightReplacerSystem) ]);
    }

    private void OnAfterInteract(Entity<LightReplacerAreaPickupComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        _bulbs.Clear();
        var coords = args.ClickLocation;
        // does not include bulbs inside boxes
        _lookup.GetEntitiesInRange(coords, ent.Comp.Range, _bulbs);
        // dont try to insert broken bulbs
        _bulbs.RemoveWhere(bulb => bulb.Comp.State != LightBulbState.Normal);
        if (_bulbs.Count < 2)
            return; // if its just one let regular pickup logic handle it, 2 minimum

        foreach (var bulb in _bulbs)
        {
            args.Handled |= _replacer.TryInsertBulb(ent, bulb, args.User, bulb: bulb.Comp);
        }

        if (!args.Handled)
            return; // nothing was inserted..?

        _audio.PlayPvs(ent.Comp.Sound, ent);
        _popup.PopupEntity(Loc.GetString("light-replacer-area-pickup-popups"), ent, args.User, PopupType.Medium);
    }
}
