// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.GameTicking.Components;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Entity effect that starts a gamerule.
/// Target entity just has to exist, it doesn't have any impact on anything.
/// </summary>
public sealed partial class StartGameRule : EntityEffectBase<StartGameRule>
{
    [DataField(required: true)]
    public EntProtoId<GameRuleComponent> Rule;

    public override string? EntityEffectGuidebookText(IPrototypeManager proto, IEntitySystemManager entSys)
        => null;
}
