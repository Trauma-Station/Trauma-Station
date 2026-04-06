using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Attribute.Components;
using Content.Trauma.Shared.Attribute.Components;

namespace Content.Trauma.Shared.Attribute.Systems;

/// <summary>
/// Handles all charisma related bullshit.
/// </summary>
public sealed partial class CharismaSystem : EntitySystem
{
    [Dependency] private readonly SharedAttributeSystem _attribute = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttributeHolderComponent, GetPhysicalSavingThrowEvent>(_attribute.RelayEvent);
        SubscribeLocalEvent<AttackAttributeComponent, GetPhysicalSavingThrowEvent>(OnCalculateAttack);
    }

    private void OnCalculateAttack(Entity<AttackAttributeComponent> ent, ref GetPhysicalSavingThrowEvent args)
    {
        if (!TryComp<AttributeComponent>(ent, out var comp))
            return;

        args.Mod += SharedAttributeSystem.LerpCurve(comp.Attribute, 1.01, 22.01, -5, 6);
    }
}
