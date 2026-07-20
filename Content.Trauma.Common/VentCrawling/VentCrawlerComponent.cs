// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.VentCrawling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VentCrawlerComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool InTube = false;
    [DataField]
    public float EnterDelay = 2.5f;

    //used for if the user can have inventory on backpack, suit and suit slot.
    [DataField]
    public bool AllowInventory = true;
}
