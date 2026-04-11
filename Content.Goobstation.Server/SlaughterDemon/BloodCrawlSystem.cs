// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.SlaughterDemon;
using Content.Goobstation.Shared.SlaughterDemon.Systems;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Polymorph;
using Robust.Server.Audio;

namespace Content.Goobstation.Server.SlaughterDemon;

public sealed class BloodCrawlSystem : SharedBloodCrawlSystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly EntityQuery<PolymorphedEntityComponent> _polymorphedQuery = default!;

    protected override bool CheckAlreadyCrawling(Entity<BloodCrawlComponent> ent)
    {
        base.CheckAlreadyCrawling(ent);

        var component = ent.Comp;
        var uid = ent.Owner;

        if (component.IsCrawling || !_polymorphedQuery.TryComp(uid, out var polymorph))
            return true;

        if (_polymorph.Revert(uid) is {} reverted)
            _audio.PlayPvs(component.ExitJauntSound, reverted);

        var evExit = new BloodCrawlExitEvent();
        if (polymorph.Parent is {} parent)
            RaiseLocalEvent(parent, ref evExit);

        return false;
    }

    protected override void PolymorphDemon(EntityUid user, ProtoId<PolymorphPrototype> polymorph)
    {
        base.PolymorphDemon(user, polymorph);

        _polymorph.PolymorphEntity(user, polymorph);
    }
}
