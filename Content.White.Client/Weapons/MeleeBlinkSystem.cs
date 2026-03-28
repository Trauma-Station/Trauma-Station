using System.Numerics;
using Content.Trauma.Common.Weapons;
using Content.White.Shared.Blink;

namespace Content.White.Client.Weapons;

public sealed partial class MeleeBlinkSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkComponent, PreHeavyAttackEvent>(OnHeavyAttack);
    }

    private void OnHeavyAttack(Entity<BlinkComponent> ent, ref PreHeavyAttackEvent args)
    {
        if (args.Handled || !(args.Direction != Vector2.Zero) || !ent.Comp.IsActive)
            return;

        RaisePredictiveEvent(new BlinkEvent(GetNetEntity(ent.Owner), args.Direction));
        args.Handled = true;
    }
}
