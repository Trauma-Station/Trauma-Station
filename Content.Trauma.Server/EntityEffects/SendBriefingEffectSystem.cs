// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Antag;
using Content.Shared.EntityEffects;
using Content.Shared.Mind.Components;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Server.EntityEffects;

public sealed partial class SendBriefingEffectSystem : EntityEffectSystem<MindContainerComponent, SendBriefing>
{
    [Dependency] private AntagSelectionSystem _antag = default!;

    protected override void Effect(Entity<MindContainerComponent> ent, ref EntityEffectEvent<SendBriefing> args)
    {
        var effect = args.Effect;
        _antag.SendBriefing(ent, Loc.GetString(effect.Text), effect.Color, effect.BriefingSound);
    }
}
