// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared._Orion.Bitrunning.Prototypes;
using Robust.Shared.Prototypes;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Roles;
using Content.Shared.Humanoid;
using Robust.Shared.Containers;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed class BitrunningDomainSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly List<BitrunningVirtualDomainPrototype> _allDomains = new();

    public override void Initialize()
    {
        base.Initialize();

        ReloadDomains();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.ByType.TryGetValue(typeof(BitrunningVirtualDomainPrototype), out _))
            return;

        ReloadDomains();
    }

    private void ReloadDomains()
    {
        _allDomains.Clear();

        foreach (var domain in _prototype.EnumeratePrototypes<BitrunningVirtualDomainPrototype>())
        {
            _allDomains.Add(domain);
        }
    }

    public IReadOnlyList<BitrunningVirtualDomainPrototype> GetAllDomains()
    {
        return _allDomains.AsReadOnly();
    }

    public bool TryGetDomain(string id, [NotNullWhen(true)] out BitrunningVirtualDomainPrototype? domain)
    {
        return _prototype.TryIndex(id, out domain);
    }

    public string GetDisplayName(BitrunningVirtualDomainPrototype domain, int scannerTier, int points)
    {
        if (CanViewName(domain, scannerTier, points))
            return Loc.GetString(domain.Name);

        return Loc.GetString("bitrunning-console-redacted");
    }

    public string GetDisplayDescription(BitrunningVirtualDomainPrototype domain, int scannerTier, int points)
    {
        if (CanViewName(domain, scannerTier, points))
            return Loc.GetString(domain.Description);

        return Loc.GetString("bitrunning-console-redacted-desc");
    }

    public string GetDisplayReward(BitrunningVirtualDomainPrototype domain, int scannerTier, int points)
    {
        if (CanViewReward(domain, scannerTier, points))
        {
            return Loc.GetString("bitrunning-ui-domain-reward",
                ("server", domain.ServerRewardPoints),
                ("np", domain.BitrunningRewardPoints));
        }

        return Loc.GetString("bitrunning-console-redacted");
    }

    private static bool CanViewName(BitrunningVirtualDomainPrototype domain, int scannerTier, int points)
    {
        if (!domain.HiddenUntilScanned)
            return true;

        return scannerTier >= domain.RequiredScannerTier && points + domain.NameRevealPointBuffer >= domain.Cost;
    }

    private static bool CanViewReward(BitrunningVirtualDomainPrototype domain, int scannerTier, int points)
    {
        return scannerTier >= domain.RequiredScannerTier + 1 && points >= domain.RequiredPointsToRevealReward;
    }

    public ProtoId<StartingGearPrototype>? GetResolvedForcedLoadout(BitrunningVirtualDomainPrototype domain, EntityUid user)
    {
        if (domain.ForcedLoadout == null)
            return null;

        if (HasCompInContainerTree<BitrunningProfileDiskComponent>(user) && TryComp<HumanoidProfileComponent>(user, out var humanoid) && domain.ForcedLoadoutSpecie.TryGetValue(humanoid.Species, out var speciesLoadout))
            return speciesLoadout;

        return domain.ForcedLoadout;
    }

    private bool HasCompInContainerTree<T>(EntityUid root) where T : Component
    {
        var queue = new Queue<EntityUid>();
        var visited = new HashSet<EntityUid>();
        queue.Enqueue(root);

        while (queue.TryDequeue(out var current))
        {
            if (!visited.Add(current))
                continue;

            if (HasComp<T>(current))
                return true;

            if (!TryComp<ContainerManagerComponent>(current, out var manager))
                continue;

            foreach (var container in manager.Containers.Values)
            {
                foreach (var contained in container.ContainedEntities)
                {
                    queue.Enqueue(contained);
                }
            }
        }

        return false;
    }
}
