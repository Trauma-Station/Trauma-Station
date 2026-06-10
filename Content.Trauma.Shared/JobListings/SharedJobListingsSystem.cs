// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.JobListings;

public abstract partial class SharedJobListingsSystem : EntitySystem
{
    [Dependency] protected SharedUserInterfaceSystem _ui = default!;
}
