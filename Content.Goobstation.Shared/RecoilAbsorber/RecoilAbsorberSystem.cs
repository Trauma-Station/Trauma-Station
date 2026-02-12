using Content.Goobstation.Common.Weapons.Ranged;
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared.Body;

namespace Content.Goobstation.Shared.RecoilAbsorber;

public sealed class RecoilAbsorberSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecoilAbsorberArmComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RecoilAbsorberArmComponent, OrganGotInsertedEvent>(OnAttach);
        SubscribeLocalEvent<RecoilAbsorberArmComponent, OrganGotRemovedEvent>(OnRemove);

        SubscribeLocalEvent<RecoilAbsorberComponent, GetRecoilModifiersEvent>(OnShot);
    }

    private void OnMapInit(Entity<RecoilAbsorberArmComponent> ent, ref MapInitEvent args)
    {
        if (_body.GetBody(ent.Owner) is {} body)
            UpdateComp(body);
    }

    private void OnAttach(Entity<RecoilAbsorberArmComponent> ent, ref OrganGotInsertedEvent args)
    {
        UpdateComp(args.Target);
    }

    private void OnRemove(Entity<RecoilAbsorberArmComponent> ent, ref OrganGotRemovedEvent args)
    {
        UpdateComp(args.Target);
    }

    private void UpdateComp(EntityUid body)
    {
        var arms = 0;
        var reduction = 0f;
        foreach (var part in _part.GetBodyParts(body, BodyPartType.Arm))
        {
            arms++;
            if (TryComp<RecoilAbsorberArmComponent>(part, out var absorber))
                reduction += 1f - absorber.Modifier;
        }

        if (arms == 0 || reduction == 0f)
        {
            RemComp<RecoilAbsorberComponent>(body);
            return;
        }

        // We have valid modifiers from all arms, add/update the component
        var comp = EnsureComp<RecoilAbsorberComponent>(body);
        // it goes from modifier of 0.3 to reduction of 0.7
        // then for both arms it's 1.4 reduction, halved to 0.7
        // then turned back into modifier of 0.3
        // if you only had 1 absorbing arm it would instead be reduction of 0.35, or modifier of 0.65 (funny)
        reduction /= arms;
        comp.Modifier = 1f - reduction;
        Dirty(body, comp);
    }

    private void OnShot(Entity<RecoilAbsorberComponent> ent, ref GetRecoilModifiersEvent args)
    {
        if (args.User != ent.Owner)
            return;

        args.Modifier *= ent.Comp.Modifier;
    }
}
