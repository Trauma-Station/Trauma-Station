// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Silicons.Laws;

namespace Content.Goobstation.Shared.CustomLawboard;


[RegisterComponent, NetworkedComponent]
public sealed partial class CustomLawboardComponent : Component
{
    /// <summary>
    /// The laws of this custom lawboard.
    /// </summary>
    public List<SiliconLaw> Laws = new();
}
