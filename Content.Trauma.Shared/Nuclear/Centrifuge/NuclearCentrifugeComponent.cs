// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Nuclear.Centrifuge;

[RegisterComponent, NetworkedComponent, Access(typeof(NuclearCentrifugeSystem))]
[AutoGenerateComponentState]
public sealed partial class NuclearCentrifugeComponent : Component
{
    /// <summary>
    /// Fuel left to be processed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FuelToExtract;

    /// <summary>
    /// How long it takes to process 1 fuel item.
    /// </summary>
    [DataField]
    public TimeSpan ExtractTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Fuel to spawn when done, stack size gets set to <see cref="FuelToExtract"/>.
    /// </summary>
    [DataField]
    public EntProtoId Result = "IngotPlutonium1";

    /// <summary>
    /// Sound played when loading an item into the centrifuge
    /// </summary>
    [DataField]
    public SoundPathSpecifier SoundLoad = new("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

    /// <summary>
    /// Sound played when the centrifuge failed to create any plutonium
    /// </summary>
    [DataField]
    public SoundPathSpecifier SoundFail = new("/Audio/Machines/buzz-two.ogg");

    /// <summary>
    /// Sound played when the centrifuge creates plutonium
    /// </summary>
    [DataField]
    public SoundPathSpecifier SoundSucceed = new("/Audio/Machines/ding.ogg");
}
