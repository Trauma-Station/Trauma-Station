// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Revolutionary;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Client.Revolutionary;

/// <summary>
/// Gives enemies of the revolution a status icon.
/// </summary>
public sealed class RevolutionaryEnemySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevolutionEnemyComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnGetStatusIcons(Entity<RevolutionEnemyComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_proto.Resolve(ent.Comp.StatusIcon, out var icon))
            args.StatusIcons.Add(icon);
    }
}
