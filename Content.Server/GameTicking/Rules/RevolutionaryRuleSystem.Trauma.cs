// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class RevolutionaryRuleSystem
{
    [Dependency] private CommonNewAntagOrEvacSystem _antagEvac = default!;

    private static readonly EntProtoId ErtSecurity = "SpawnERTSecurity";
}
