// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.CollectiveMind;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Shared.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    [SubscribeLocalEvent]
    private void OnXray(HereticXRayVisionEvent args)
    {
        _eye.SetDrawFov(args.Heretic, args.Negative);
    }

    [SubscribeLocalEvent]
    private void OnAscensionLock(HereticAscensionLockEvent args)
    {
        var collectiveMind = EnsureComp<CollectiveMindComponent>(args.Heretic);
        if (args.Negative)
            collectiveMind.Channels.Remove(MansusLinkMind);
        else
            collectiveMind.Channels.Add(MansusLinkMind);
    }
}
