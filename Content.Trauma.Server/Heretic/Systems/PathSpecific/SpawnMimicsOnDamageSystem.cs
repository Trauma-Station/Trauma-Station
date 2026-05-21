// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Mind;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Trauma.Server.Heretic.Abilities;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Flesh;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed partial class SpawnMimicsOnDamageSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private HereticAbilitySystem _ability = default!;
    [Dependency] private HereticSystem _heretic = default!;
    [Dependency] private MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<SpawnMimicsOnDamageComponent, DamageModifyEvent>(OnModify, held: false);
    }

    private void OnModify(Entity<SpawnMimicsOnDamageComponent> ent, ref DamageModifyEvent args)
    {
        if (args.TargetPart == null)
            return;

        var total = args.OriginalDamage.GetTotal();
        if (total < ent.Comp.MinDamage)
            return;

        if (!_heretic.IsHereticOrGhoul(args.Target))
            return;

        var chance = Math.Clamp(total.Float() * ent.Comp.DamageChanceRatio, 0f, 1f);
        if (!_random.Prob(chance))
            return;

        var health = ent.Comp.BaseGhoulHealth;
        if (TryComp(args.Target, out GhoulComponent? ghoul))
            health = ghoul.TotalHealth * ent.Comp.GhoulHealthMultiplier;

        var user = args.Target;
        int id;
        if (TryComp(args.Target, out HereticMinionComponent? minion))
        {
            if (minion.BoundHeretic is { } heretic)
                user = heretic;

            id = minion.MinionId;
        }
        else if (_mind.TryGetMind(args.Target, out var mind, out _))
            id = GetNetEntity(mind).Id;
        else
            return;

        _ability.CreateFleshMimic(args.Target, user, id, true, true, health, args.Origin, false);
    }
}
