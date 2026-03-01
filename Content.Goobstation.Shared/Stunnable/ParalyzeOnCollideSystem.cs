// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Goobstation.Shared.Stunnable;

public sealed class ParalyzeOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParalyzeOnCollideComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ParalyzeOnCollideComponent, LandEvent>(OnLand);
    }

    private void OnStartCollide(EntityUid uid, ParalyzeOnCollideComponent component, ref StartCollideEvent args)
    {
        if (component.CollidableEntities != null &&
            _whitelist.IsValid(component.CollidableEntities, args.OtherEntity))
            return;

        if (component.ParalyzeOther)
            _stun.TryUpdateParalyzeDuration(args.OtherEntity, component.ParalyzeTime);
        if (component.ParalyzeSelf)
            _stun.TryUpdateParalyzeDuration(uid, component.ParalyzeTime);

        if (component.RemoveAfterCollide)
            RemCompDeferred(uid, component);
    }

    private void OnLand(EntityUid uid, ParalyzeOnCollideComponent component, ref LandEvent args)
    {
        if (component.RemoveOnLand)
            RemCompDeferred(uid, component);
    }
}
