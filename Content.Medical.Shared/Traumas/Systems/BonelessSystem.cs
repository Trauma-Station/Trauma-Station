// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Medical.Shared.Traumas;

public sealed partial class BonelessSystem : EntitySystem
{
    // imagine if prototypes could remove comps...
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BonelessComponent> ent, ref MapInitEvent args)
    {
        RemComp<BoneComponent>(ent);
    }
}
