// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Abductor;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Server.Abductor;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.Side;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class EnvyKnifeSystem : EntitySystem
{
    [Dependency] private readonly AbductorVestDisguiseSystem _disguise = default!;
    [Dependency] private readonly HumanoidProfileSystem _profile = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnvyKnifeComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(Entity<EnvyKnifeComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        var victim = args.HitEntities[0];
        if (victim == args.User || HasComp<AbductorDisguiseStateComponent>(victim) ||
            !TryComp(victim, out HumanoidProfileComponent? profileComp) ||
            !TryComp(victim, out BodyComponent? body))
            return;

        // If not heretic and not ghoul or heretic and ascended then can't use it
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out _) &&
            !HasComp<GhoulComponent>(args.User) || heretic?.Ascended is true)
            return;

        var profile = _profile.CreateProfile((victim, profileComp));
        var organs = _disguise.GetOrgans((victim, body));
        _disguise.ApplyDisguise(args.User, profile, organs, ent.Comp.RevertAction, true, false);
    }
}
