using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Trauma.Shared.AER;

namespace Content.Trauma.Server.Aer.Objectives;

public sealed partial class AerBreachConditionSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AerBreachConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<AerBreachConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.MindId, args.Mind);
    }

    private float GetProgress(EntityUid mindId, MindComponent mind)
    {
        // if you are dead you arent breaching
        if (mind.OwnedEntity == null || _mind.IsCharacterDeadIc(mind))
            return 0f;

        // if you aren't contained you are breaching
        if (TryComp<AnomalousEntityComponent>(mind.OwnedEntity, out var aerEntity) && aerEntity.Contained == false)
            return 1f;
        else
            return 0f;
    }
}