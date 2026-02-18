using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Rejuvenate;

namespace Content.Medical.Shared.Body;

public sealed class BodyRestoreSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, RejuvenateEvent>(OnRejuvenate,
            before: [ typeof(DamageableSystem) ]);
    }

    private void OnRejuvenate(Entity<BodyComponent> ent, ref RejuvenateEvent args)
    {
        RestoreBody(ent.AsNullable());
        // not using _body.RelayEvent because it wraps it in BodyRelayedEvent
        foreach (var organ in _body.GetOrgans(ent.AsNullable()))
        {
            RaiseLocalEvent(organ, args); // TODO: make by ref if it stops being a class
        }
    }

    public void RestoreBody(Entity<BodyComponent?> body)
    {
        if (_part.GetRootPart(body) is {} root)
            _part.RestoreInitialOrgans(root.AsNullable()); // recursively restore the body from its prototype
        else
            Log.Error($"Tried to restore body {ToPrettyString(body)} which had no root part!");
    }
}
