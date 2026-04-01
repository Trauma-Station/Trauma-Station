// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Magic;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Zombies;
using Content.Trauma.Common.Wizard;
using Content.Trauma.Shared.Wizard.Chuuni;
using Content.Trauma.Shared.Wizard.FadingTimedDespawn;
using Content.Trauma.Shared.Wizard.Projectiles;
using Content.Trauma.Shared.Wizard.TimeStop;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Trauma.Shared.Wizard;

public sealed partial class SharedWizardSystem : CommonWizardSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostComponent, BeforeMindSwappedEvent>(OnMindswapGhost);
        SubscribeLocalEvent<SpectralComponent, BeforeMindSwappedEvent>(OnMindswapSpectral);
        SubscribeLocalEvent<TimedDespawnComponent, BeforeMindSwappedEvent>(OnMindswapTemporary);
        SubscribeLocalEvent<FadingTimedDespawnComponent, BeforeMindSwappedEvent>(OnMindswapFadedTemporary);
        SubscribeLocalEvent<MobStateComponent, BeforeMindSwappedEvent>(OnMindswapIncapacitated);
        SubscribeLocalEvent<ZombieComponent, BeforeMindSwappedEvent>(OnMindswapZombie);

        SubscribeLocalEvent<AfterMindSwappedEvent>(OnMindswapAfter);

        SubscribeLocalEvent<ReflectiveComponent, ProjectileReflectedEvent>(OnReflection);
    }

    public override bool IsChunni(EntityUid? eyepatch)
    {
        return HasComp<ChuuniEyepatchComponent>(eyepatch);
    }

    public override bool IsMovementBlocked(EntityUid? wizard)
    {
        return HasComp<FrozenComponent>(wizard);
    }

    private void OnMindswapGhost(Entity<GhostComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled)
            return;

        args.Message = "ghost";
        args.Cancelled = true;
    }

    private void OnMindswapSpectral(Entity<SpectralComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled)
            return;

        args.Message = "ghost";
        args.Cancelled = true;
    }

    private void OnMindswapTemporary(Entity<TimedDespawnComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled)
            return;

        args.Message = "temporary";
        args.Cancelled = true;
    }

    private void OnMindswapFadedTemporary(Entity<FadingTimedDespawnComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled)
            return;

        args.Message = "temporary";
        args.Cancelled = true;
    }

    private void OnMindswapIncapacitated(Entity<MobStateComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled || !_mobState.IsIncapacitated(ent))
            return;

        args.Message = "dead";
        args.Cancelled = true;
    }

    private void OnMindswapZombie(Entity<ZombieComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled)
            return;
        args.Message = "dead";
        args.Cancelled = true;
    }

    private void OnMindswapAfter(ref AfterMindSwappedEvent args)
    {
        TransferComponent<RevolutionaryComponent>(args.Performer, args.Target);
        TransferComponent<HeadRevolutionaryComponent>(args.Performer, args.Target);
        TransferComponent<WizardComponent>(args.Performer, args.Target);
        TransferComponent<ApprenticeComponent>(args.Performer, args.Target);
        OnFactionSwap(ref args);
    }

    private void TransferComponent<T>(EntityUid a, EntityUid b) where T : IComponent, new()
    {
        var aHas = TryComp<T>(a, out var compA);
        var bHas = TryComp<T>(b, out var compB);

        if (aHas && bHas)
            return;

        if (aHas)
        {
            RemComp<T>(a);
            EnsureComp<T>(b);
        }
        else if (bHas)
        {
            RemComp<T>(b);
            EnsureComp<T>(a);
        }
    }

    private void OnFactionSwap(ref AfterMindSwappedEvent args)
    {
        // These are the only factions we want to "follow" the mind
        var factionsToTransfer = new List<ProtoId<NpcFactionPrototype>> { "Wizard", "Assistant" };
        var fallback = new ProtoId<NpcFactionPrototype>("NanoTrasen");

        // Get the actual components
        var perfComp = EnsureComp<NpcFactionMemberComponent>(args.Performer);
        var tarComp = EnsureComp<NpcFactionMemberComponent>(args.Target);

        // 1. Snapshot the relevant factions from both
        var perfFactions = perfComp.Factions.Where(f => factionsToTransfer.Contains(f)).ToList();
        var tarFactions = tarComp.Factions.Where(f => factionsToTransfer.Contains(f)).ToList();

        // 2. Clear ONLY the transferable factions from both bodies
        foreach (var f in perfFactions) _faction.RemoveFaction(args.Performer, f, false);
        foreach (var f in tarFactions) _faction.RemoveFaction(args.Target, f, false);

        // 3. Swap them
        _faction.AddFactions(args.Target, perfFactions.ToHashSet());
        _faction.AddFactions(args.Performer, tarFactions.ToHashSet());

        // 4. Fallback logic: If a body is now factionless, give them the default
        if (perfComp.Factions.Count == 0) _faction.AddFaction(args.Performer, fallback);
        if (tarComp.Factions.Count == 0) _faction.AddFaction(args.Target, fallback);
    }

    private void OnReflection(Entity<ReflectiveComponent> ent, ref ProjectileReflectedEvent args)
    {
        RemComp<HomingProjectileComponent>(ent);
    }
}
