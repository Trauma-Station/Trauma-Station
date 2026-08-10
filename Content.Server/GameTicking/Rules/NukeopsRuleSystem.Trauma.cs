// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.GameTicking;

namespace Content.Server.GameTicking.Rules;

public sealed partial class NukeopsRuleSystem
{
    [Dependency] private CommonNewAntagOrEvacSystem _antagEvac = default!;
}
