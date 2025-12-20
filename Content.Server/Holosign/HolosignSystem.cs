using Content.Shared.Examine;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.PowerCell;
using Content.Shared.Interaction;
using Content.Shared.Storage;

namespace Content.Server.Holosign;

public sealed partial class HolosignSystem : EntitySystem // Trauma - made partial
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HolosignProjectorComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        SubscribeLocalEvent<HolosignProjectorComponent, ExaminedEvent>(OnExamine);

        InitializeTrauma(); // Trauma
    }

    private void OnExamine(EntityUid uid, HolosignProjectorComponent component, ExaminedEvent args)
    {
        // TODO: This should probably be using an itemstatus
        // TODO: I'm too lazy to do this rn but it's literally copy-paste from emag.
        var charges = _powerCell.GetRemainingUses(uid, component.ChargeUse);
        var maxCharges = _powerCell.GetMaxUses(uid, component.ChargeUse);

        using (args.PushGroup(nameof(HolosignProjectorComponent)))
        {
            args.PushMarkup(Loc.GetString("limited-charges-charges-remaining", ("charges", charges)));

            if (charges > 0 && charges == maxCharges)
            {
                args.PushMarkup(Loc.GetString("limited-charges-max-charges"));
            }
        }
    }

    private void OnBeforeInteract(EntityUid uid, HolosignProjectorComponent component, BeforeRangedInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach // prevent placing out of range
            || HasComp<StorageComponent>(args.Target) // if it's a storage component like a bag, we ignore usage so it can be stored
            || CheckCoords((uid, component), args) is not {} coords) // Goob - replaces power check
            return;

        var holoUid = Spawn(component.SignProto, coords); // Goob - use coords from above logic
        var xform = Transform(holoUid);
        // TODO: Just make the prototype anchored
        if (!xform.Anchored)
            _transform.AnchorEntity(holoUid, xform); // anchor to prevent any tempering with (don't know what could even interact with it)

        args.Handled = true;
    }
}
