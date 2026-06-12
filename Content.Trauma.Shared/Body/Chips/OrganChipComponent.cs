// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;

namespace Content.Trauma.Shared.Body.Chips;

/// <summary>
/// For chips that can be installed into an organ with <see cref="OrganChipContainerComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class OrganChipComponent : Component
{
    /// <summary>
    /// The organs this chip can go in.
    /// They must have <see cref="OrganChipContainerComponent"/> to work.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<OrganCategoryPrototype>> Parents = default!;

    /// <summary>
    /// The organ this chip is currently installed in, if any.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Organ;

    /// <summary>
    /// How long it takes to install/remove for yourself or directly with an organ.
    /// </summary>
    [DataField]
    public TimeSpan ShortDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long it takes to install/remove for another person.
    /// </summary>
    [DataField]
    public TimeSpan LongDelay = TimeSpan.FromSeconds(8);
}

/// <summary>
/// Raised on an organ chip when it is installed into an organ, or when that organ is installed into a body.
/// </summary>
[ByRefEvent]
public record struct OrganChipInsertedEvent(EntityUid Organ, EntityUid? Body);

/// <summary>
/// Raised on an organ chip when it is removed from an organ, or when that organ is removed from a body.
/// </summary>
[ByRefEvent]
public record struct OrganChipRemovedEvent(EntityUid Organ, EntityUid? Body);
