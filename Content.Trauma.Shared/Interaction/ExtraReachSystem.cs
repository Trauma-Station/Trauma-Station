// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Shared.Interaction;

namespace Content.Trauma.Shared.Interaction;

public sealed class ExtraReachSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtraReachComponent, OrganEnabledEvent>(OnPartEnabled);
        SubscribeLocalEvent<ExtraReachComponent, OrganDisabledEvent>(OnPartDisabled);
        // run before TK so it can use the extra reach for its check
        SubscribeLocalEvent<ExtraReachComponent, InRangeOverrideEvent>(OnRangeOverride,
            before: new[] { typeof(TelekinesisSystem) });
    }

    private void OnPartEnabled(Entity<ExtraReachComponent> ent, ref OrganEnabledEvent args)
    {
        ModifyReach(args.Body, ent.Comp.Bonus);
    }

    private void OnPartDisabled(Entity<ExtraReachComponent> ent, ref OrganDisabledEvent args)
    {
        ModifyReach(args.Body, -ent.Comp.Bonus);
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
