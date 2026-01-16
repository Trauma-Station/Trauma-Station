using Content.Shared.Access;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo.Prototypes;

/// <summary>
/// Trauma - ungimping dept economy
/// </summary>
public sealed partial class CargoAccountPrototype
{
    /// <summary>
    /// The access level required to approve orders.
    /// Ignored if the used console has been emagged by an access breaker.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AccessLevelPrototype> ApproveAccess;
}
