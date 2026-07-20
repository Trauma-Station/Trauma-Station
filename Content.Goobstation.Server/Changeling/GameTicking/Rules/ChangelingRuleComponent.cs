// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Server.Changeling.GameTicking.Rules;

[RegisterComponent, Access(typeof(ChangelingRuleSystem))]
public sealed partial class ChangelingRuleComponent : Component
{
    public readonly List<EntityUid> ChangelingMinds = new();
}
