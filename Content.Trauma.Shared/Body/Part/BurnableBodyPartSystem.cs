// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Body.Part;

public sealed partial class BurnableWingsSystem : EntitySystem
{
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnDamageChanged(Entity<BurnableBodyPartComponent> ent, ref DamageDealtEvent args)
    {
        var allDmg = _dmg.GetAllDamage(ent.Owner);
        if (!allDmg.DamageDict.TryGetValue(ent.Comp.DamageType, out var dmg) || dmg < ent.Comp.DamageThreshold)
            return;

        var coords = Transform(ent).Coordinates;

        _audio.PlayPredicted(ent.Comp.BurnSound, coords, args.Origin);
        if (ent.Comp.BurnMessage is { } msg)
            _popup.PopupCoordinates(Loc.GetString(msg, ("ent", ent)), coords, PopupType.MediumCaution);

        var newWings = PredictedSpawnAtPosition(ent.Comp.BurntPart, coords);

        if (_body.GetBody(ent.Owner) is { } body)
        {
            if (_body.RemoveOrgan(body, ent.Owner))
                _body.InsertOrgan(body, newWings);
        }

        PredictedQueueDel(ent);
    }
}
