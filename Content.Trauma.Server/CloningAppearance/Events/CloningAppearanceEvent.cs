// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.CloningAppearance.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Trauma.Server.CloningAppearance.Events;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class CloningAppearanceEvent : EntityEventArgs
{
    public ICommonSession Player = default!;
    public CloningAppearanceComponent Component = default!;
    public EntityCoordinates Coords { get; set; }
    public EntityUid? StationUid { get; set; }
    public EntityUid? MindId { get; set; }
}
