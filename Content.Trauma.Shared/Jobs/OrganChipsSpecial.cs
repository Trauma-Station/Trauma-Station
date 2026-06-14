// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Content.Trauma.Shared.Body.Chips;

namespace Content.Trauma.Shared.Jobs;

/// <summary>
/// Spawns and inserts organ chips to the target mob.
/// </summary>
public sealed partial class OrganChipsSpecial : JobSpecial
{
    [DataField(required: true)]
    public List<EntProtoId<OrganChipComponent>> Chips = default!;

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var sys = entMan.System<OrganChipSystem>();
        foreach (var id in Chips)
        {
            sys.InstallChip(mob, id);
        }
    }
}
