// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Restrict;

// TODO: kill this shit just use whitelist
public sealed partial class SharedRestrictSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;

    [SubscribeLocalEvent]
    private void OnAttemptInteract(Entity<RestrictInteractionByUserTagComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (_tag.HasAllTags(args.User, ent.Comp.Contains) && !_tag.HasAnyTag(args.User, ent.Comp.DoesntContain))
            return;

        if (ent.Comp.Messages.Count != 0)
        {
            var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
            _popup.PopupEntity(Loc.GetString(rand.Pick(ent.Comp.Messages)), args.User);
        }

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnAttemptMelee(Entity<RestrictMeleeByUserTagComponent> ent, ref AttemptMeleeEvent args)
    {
        if (_tag.HasAllTags(args.User, ent.Comp.Contains) && !_tag.HasAnyTag(args.User, ent.Comp.DoesntContain))
            return;

        if (ent.Comp.Messages.Count != 0)
        {
            var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
            args.Message = Loc.GetString(rand.Pick(ent.Comp.Messages));
        }

        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnAttemptGunshot(Entity<RestrictGunshotsByUserTagComponent> ent, ref ShotAttemptedEvent args)
    {
        if (_tag.HasAllTags(args.User, ent.Comp.Contains) && !_tag.HasAnyTag(args.User, ent.Comp.DoesntContain))
            return;

        var now = _timing.CurTime;
        if (ent.Comp.Messages.Count != 0 && now > ent.Comp.LastPopup + TimeSpan.FromSeconds(1))
        {
            ent.Comp.LastPopup = now;
            var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
            _popup.PopupEntity(Loc.GetString(rand.Pick(ent.Comp.Messages)), args.User);
        }

        args.Cancel();
    }
}
