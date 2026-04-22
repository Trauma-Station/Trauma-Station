// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared.Body;
using Content.Shared.Damage.Components;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Propogates godmode to all organs and prevents removing them.
/// </summary>
public sealed class BodyGodmodeSystem : EntitySystem
{
    [Dependency] private readonly EntityQuery<BodyComponent> _bodyQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GodmodeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GodmodeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<GodmodeComponent, OrganRemoveAttemptEvent>(OnRemoveAttempt);
    }

    private void OnStartup(Entity<GodmodeComponent> ent, ref ComponentStartup args)
    {
        if (_bodyQuery.CompOrNull(ent)?.Organs is not {} organs)
            return;

        foreach (var organ in organs.ContainedEntities)
        {
            EnsureComp<GodmodeComponent>(organ);
        }
    }

    private void OnShutdown(Entity<GodmodeComponent> ent, ref ComponentShutdown args)
    {
        if (_bodyQuery.CompOrNull(ent)?.Organs is not {} organs)
            return;

        foreach (var organ in organs.ContainedEntities)
        {
            RemComp<GodmodeComponent>(organ);
        }
    }

    private void OnRemoveAttempt(Entity<GodmodeComponent> ent, ref OrganRemoveAttemptEvent args)
    {
        // no stealing godmode limbs for yourself, and doesn't make sense that you can have organs harvested etc with godmode
        args.Cancelled = true;
    }
}
