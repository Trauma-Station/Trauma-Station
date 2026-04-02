// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.TelescopicBaton;

[ByRefEvent]
public record struct KnockdownOnHitAttemptEvent(bool Cancelled, bool DropItems); // Goobstation

public sealed class KnockdownOnHitSuccessEvent(List<EntityUid> knockedDown) : EntityEventArgs // Goobstation
{
    public List<EntityUid> KnockedDown = knockedDown;
}
