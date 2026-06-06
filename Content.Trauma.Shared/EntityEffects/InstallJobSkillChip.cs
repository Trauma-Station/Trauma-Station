// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Installs a mob's expected skill chip for its job.
/// </summary>
public sealed partial class InstallJobSkillChip : EntityEffectBase<InstallJobSkillChip>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // do not fucking make a reagent with this :sob:
}

public sealed partial class InstallJobSkillChipSystem : EntityEffectSystem<MindContainerComponent, InstallJobSkillChip>
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    public static readonly ProtoId<OrganCategoryPrototype> Head = "Head";

    protected override void Effect(Entity<MindContainerComponent> ent, ref EntityEffectEvent<InstallJobSkillChip> args)
    {
        if (ent.Comp.Mind is not {} mind ||
            !_role.MindHasRole<JobRoleComponent>(mind, out var role) ||
            role?.Comp1.JobPrototype is not {} job ||
            _body.GetOrgan(ent.Owner, Head) is not {} part)
            return;

        var id = "SkillChip" + job;
        if (!_proto.HasIndex<EntityPrototype>(id))
        {
            Log.Error($"Job {job} of {ToPrettyString(ent)} had no skill chip prototype defined! ({id})");
            return;
        }

        _part.SpawnAndInsert(part, id);
    }
}
