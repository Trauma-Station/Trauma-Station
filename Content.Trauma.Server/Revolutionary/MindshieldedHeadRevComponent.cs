// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Server.Revolutionary;

/// <summary>
/// Component for headrev that needs a fake mindshield
/// </summary>
[RegisterComponent, Access(typeof(MindshieldedHeadRevSystem))]
public sealed partial class MindshieldedHeadRevComponent : Component
{
    [DataField]
    public EntProtoId FakeMindShieldImplant = "FakeMindShieldImplant";
}
