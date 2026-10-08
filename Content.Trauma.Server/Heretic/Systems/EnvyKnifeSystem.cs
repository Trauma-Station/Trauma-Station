// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Abductor;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Server.Abductor;
using Content.Trauma.Shared.Heretic.Components.Side;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed partial class EnvyKnifeSystem : EntitySystem
{
    [Dependency] private AbductorVestDisguiseSystem _disguise = default!;
    [Dependency] private HumanoidProfileSystem _profile = default!;

    [SubscribeLocalEvent]
    private void OnHit(Entity<EnvyKnifeComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        var victim = args.HitEntities[0];
        if (victim == args.User || !HasComp<HumanoidProfileComponent>(args.User) ||
            HasComp<AbductorDisguiseStateComponent>(victim) ||
            !TryComp(victim, out HumanoidProfileComponent? profileComp) ||
            !TryComp(victim, out BodyComponent? body))
            return;

        var profile = _profile.CreateProfile((victim, profileComp));
        var organs = _disguise.GetOrgans((victim, body));
        _disguise.ApplyDisguise(args.User, profile, organs, ent.Comp.RevertAction, true, false);
    }
}
