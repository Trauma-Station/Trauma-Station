// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Objectives.Systems;
using Content.Shared.Revolutionary.Components;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed partial class RevolutionaryRuleSystem
{
    [Dependency] private CommonNewAntagOrEvacSystem _antagEvac = default!;
}
