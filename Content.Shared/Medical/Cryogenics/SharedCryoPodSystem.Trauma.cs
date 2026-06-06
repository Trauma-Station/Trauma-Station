// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Bed.Sleep;

namespace Content.Shared.Medical.Cryogenics;

public abstract partial class SharedCryoPodSystem
{
    [Dependency] private SleepingSystem _sleeping = default!;
}
