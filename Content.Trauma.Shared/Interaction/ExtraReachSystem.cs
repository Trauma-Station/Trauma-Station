using Content.Shared._Shitmed.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Interaction;

namespace Content.Trauma.Shared.Interaction;

public sealed class ExtraReachSystem : EntitySystem
{
    private EntityQuery<BodyPartComponent> _partQuery;

    public override void Initialize()
    {
        base.Initialize();

        _partQuery = GetEntityQuery<BodyPartComponent>();

        SubscribeLocalEvent<ExtraReachComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ExtraReachComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ExtraReachComponent, BodyPartEnableChangedEvent>(OnPartEnableChanged);
        // run before TK so it can use the extra reach for its check
        SubscribeLocalEvent<ExtraReachComponent, InRangeOverrideEvent>(OnRangeOverride,
            before: new[] { typeof(TelekinesisSystem) });
    }

    private void OnMapInit(Entity<ExtraReachComponent> ent, ref MapInitEvent args)
    {
        if (_partQuery.CompOrNull(ent)?.Body is not {} body)
            return;

        ModifyReach(body, ent.Comp.Bonus);
    }

    private void OnShutdown(Entity<ExtraReachComponent> ent, ref ComponentShutdown args)
    {
        if (_partQuery.CompOrNull(ent)?.Body is not {} body)
            return;

        ModifyReach(body, -ent.Comp.Bonus);
    }

    private void OnPartEnableChanged(Entity<ExtraReachComponent> ent, ref BodyPartEnableChangedEvent args)
    {
        if (_partQuery.CompOrNull(ent)?.Body is not {} body)
            return;

        // add or remove the bonus to the body depending on being enabled or not
        var sign = args.Enabled ? 1f : -1f;
        ModifyReach(body, sign * ent.Comp.Bonus);
    }

    private void OnRangeOverride(Entity<ExtraReachComponent> ent, ref InRangeOverrideEvent args)
    {
        args.Range += ent.Comp.Bonus;
    }

    public void ModifyReach(EntityUid uid, float reach)
    {
        // don't care if the body is being deleted
        if (TerminatingOrDeleted(uid))
            return;

        var comp = EnsureComp<ExtraReachComponent>(uid);
        comp.Bonus += reach;
        Dirty(uid, comp);

        // remove the component if it goes to 0f
        if (Math.Abs(comp.Bonus) < 0.001f)
            RemComp(uid, comp);
    }
}
