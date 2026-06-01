// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Vehicles.Clowncar;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedClowncarSystem))]
[AutoGenerateComponentState]
public sealed partial class ClowncarComponent : Component
{
    [DataField]
    public string Container = "clowncar_container";

    [DataField]
    public EntProtoId ThankRiderAction = "ActionThankDriver";

    [DataField]
    public EntProtoId QuietInTheBackAction = "ActionQuietBackThere";

    [DataField]
    public EntProtoId DrunkDrivingAction = "ActionDrivingWithStyle";

    [DataField, AutoNetworkedField]
    public int ThankCounter;

    /// <summary>
    /// How many times passengers have to use the thank rider action without the driver using quiet in the back to be freed.
    /// </summary>
    [DataField]
    public int FreedomThanks = 5;
}
