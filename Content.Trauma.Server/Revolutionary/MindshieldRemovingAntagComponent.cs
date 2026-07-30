// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Revolutionary;

/// <summary>
/// Gamerule component that removes a real mindshield and replaces it with a fake one for antags who start with a real mindshield
/// </summary>
[RegisterComponent, Access(typeof(MindshieldRemovingAntagSystem))]
public sealed partial class MindshieldRemovingAntagComponent : Component
{
    [DataField]
    public EntProtoId FakeMindShieldImplant = "FakeMindShieldImplant";
}
