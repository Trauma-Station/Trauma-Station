using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.MisandryBox.JobObjective;

public sealed partial class AddObjectiveSpecial : JobSpecial
{
    /// <summary>
    /// List of objective prototypes to assign to this job
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Objectives = new();

    public override void AfterEquip(EntityUid mob)
    {
        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<JobObjectiveSystem>();
        system.QueueObjectives(mob, Objectives);
    }
}
