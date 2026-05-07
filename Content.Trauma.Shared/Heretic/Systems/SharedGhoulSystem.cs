// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Magic;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;

namespace Content.Trauma.Shared.Heretic.Systems;

public abstract class SharedGhoulSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;

    [Dependency] private readonly EntityQuery<BrainComponent> _brainQuery = default!;
    [Dependency] private readonly EntityQuery<WoundableComponent> _woundableQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhoulComponent, BeforeMindSwappedEvent>(OnBeforeMindSwap);
    }

    private void OnBeforeMindSwap(Entity<GhoulComponent> ent, ref BeforeMindSwappedEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = true;
        args.Message = "ghoul";
    }

    /// <summary>
    /// Required to prevent heretic from farming organs from ghouls
    /// </summary>
    public void MakeOrgansFragile(EntityUid uid)
    {
        foreach (var organ in _body.GetOrgans(uid))
        {
            // Don't curse brain and torso
            if (_brainQuery.HasComp(organ) || _woundableQuery.TryComp(organ, out var woundable) &&
                woundable.RootWoundable == organ.Owner)
                continue;

            EnsureComp<FragileOrganComponent>(organ);
        }
    }
}
