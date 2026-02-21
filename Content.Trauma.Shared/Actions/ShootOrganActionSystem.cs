// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Shared.Polymorph;
using Content.Shared.Polymorph.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;

namespace Content.Trauma.Shared.Actions;

public sealed class ShootOrganActionSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedPolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShootOrganActionComponent, ShootOrganActionEvent>(OnShootOrganAction);
    }

    private void OnShootOrganAction(Entity<ShootOrganActionComponent> ent, ref ShootOrganActionEvent args)
    {
        args.Handled = true;

        var user = args.Performer;
        if (RemoveOrgan(ent, user) is not {} organ)
        {
            _popup.PopupClient(Loc.GetString("MutationTongueSpike-popup-no-organ", ("organ", ent.Comp.Organ)), user, user);
            return;
        }

        // polymorph isn't predicted so this returns false on client
        if (_polymorph.PolymorphEntity(organ, ent.Comp.Polymorph) is not {} projectile)
            return;

        _throwing.TryThrow(projectile, coordinates: args.Target, user: user,
            predicted: false); // TODO: remove if polymorph gets predicted
    }

    private EntityUid? RemoveOrgan(Entity<ShootOrganActionComponent> ent, EntityUid user)
    {
        if (_body.GetOrgan(user, ent.Comp.Organ) is {} organ &&
            _body.RemoveOrgan(user, organ))
            return organ;

        return null;
    }
}
