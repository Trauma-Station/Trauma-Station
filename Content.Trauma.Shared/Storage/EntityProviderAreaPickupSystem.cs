// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Trauma.Shared.Light;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Storage;

/// <summary>
/// Handles area pickup of bulbs/tubes for light replacers.
/// </summary>
public sealed partial class EntityProviderAreaPickupSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityProviderSystem _provider = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<EntityProviderComponent> _query = default!;

    private HashSet<Entity<IComponent>> _targets = new();

    [SubscribeLocalEvent(before: [typeof(EntityProviderSystem)])]
    private void OnAfterInteract(Entity<EntityProviderAreaPickupComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        var provider = new Entity<EntityProviderComponent>(ent, _query.Comp(ent));
        if (!provider.Comp.CanTransfer)
            return;

        ent.Comp.Type ??= Factory.GetRegistration(ent.Comp.Comp).Type;

        _targets.Clear();
        var coords = _transform.ToMapCoordinates(args.ClickLocation);
        // does not include bulbs inside boxes
        _lookup.GetEntitiesInRange(ent.Comp.Type, coords, ent.Comp.Range, _targets);
        if (_targets.Count < 2)
            return; // if its just one let regular pickup logic handle it, 2 minimum

        foreach (var target in _targets)
        {
            // dont pass user to avoid sound spam
            args.Handled |= _provider.TryInsertIntoProvider(provider, target, user: null);
        }

        if (!args.Handled)
            return; // nothing was inserted..?

        _audio.PlayPvs(ent.Comp.Sound, ent);
        _popup.PopupEntity(Loc.GetString("light-replacer-area-pickup-popups"), ent, args.User, PopupType.Medium);
    }
}
