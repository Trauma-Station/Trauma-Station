using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Fluids.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Inventory; // Assmos - Extinguisher Nozzle
using Content.Shared.Whitelist; // Assmos - Extinguisher Nozzle

namespace Content.Shared.Fluids.Components;

[RegisterComponent]
[Access(typeof(SharedSpraySystem))]
public sealed partial class SprayComponent : Component
{
    public const string SolutionName = "spray";
    public const string TankSolutionName = "tank"; // Assmos - Extinguisher Nozzle

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public FixedPoint2 TransferAmount = 10;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float SprayDistance = 3.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float SprayVelocity = 3.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public EntProtoId SprayedPrototype = "Vapor";

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int VaporAmount = 1;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float VaporSpread = 90f;

    /// <summary>
    /// How much the player is pushed back for each spray.
    /// </summary>
    [DataField]
    public float PushbackAmount = 5f;

    [DataField(required: true)]
    [Access(typeof(SharedSpraySystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public SoundSpecifier SpraySound { get; private set; } = default!;

    [DataField]
    public LocId SprayEmptyPopupMessage = "spray-component-is-empty-message";

    // <Goob>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SlotFlags TargetSlot;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public EntityWhitelist? ProviderWhitelist;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool ExternalContainer = false;
    // </Goob>
}
