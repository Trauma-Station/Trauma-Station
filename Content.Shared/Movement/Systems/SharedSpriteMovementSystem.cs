using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedSpriteMovementSystem : EntitySystem
{
    [SubscribeLocalEvent]
    protected virtual void OnSpriteMoveInput(Entity<SpriteMovementComponent> ent, ref SpriteMoveEvent args)
    {
        if (ent.Comp.IsMoving == args.IsMoving)
            return;

        ent.Comp.IsMoving = args.IsMoving;
        DirtyField(ent, ent.Comp, nameof(SpriteMovementComponent.IsMoving)); // Trauma - DirtyField instead of Dirty
    }
}
