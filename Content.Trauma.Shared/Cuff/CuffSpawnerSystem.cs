using Content.Shared.Coordinates;
using Content.Shared.Cuffs;
using Content.Shared.Interaction;

namespace Content.Trauma.Shared.Cuff;

public sealed partial class CuffSpawnerSystem : EntitySystem
{
    [Dependency] private readonly SharedCuffableSystem _cuffs = default!;

    private static readonly EntProtoId Cuff = "Handcuffs";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CuffSpawnerComponent, InteractUsingEvent>(OnInteract);
    }

    private void OnInteract(Entity<CuffSpawnerComponent> ent, ref InteractUsingEvent args)
    {
        var cuff = SpawnAtPosition(Cuff, ent.Owner.ToCoordinates());
        _cuffs.TryCuffing(ent.Owner, args.Target, cuff);
    }
}
