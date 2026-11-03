// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Antag;
using Content.Shared.EntityEffects;
using Content.Shared.Mind.Components;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that sends an antag briefing to an entity
/// </summary>
public sealed partial class SendBriefing : EntityEffectBase<SendBriefing>
{
    /// <summary>
    /// The text to display to the chat.
    /// </summary>
    [DataField(required: true)]
    public LocId Text;

    /// <summary>
    /// The color of the <see cref="Text"/>.
    /// </summary>
    [DataField]
    public Color? Color;

    /// <summary>
    /// The sound to play during the briefing
    /// </summary>
    [DataField]
    public SoundSpecifier? BriefingSound;
}

public sealed partial class SendBriefingEffectSystem : EntityEffectSystem<MindContainerComponent, SendBriefing>
{
    [Dependency] private AntagSelectionSystem _antag = default!;

    protected override void Effect(Entity<MindContainerComponent> ent, ref EntityEffectEvent<SendBriefing> args)
    {
        var effect = args.Effect;
        _antag.SendBriefing(ent, Loc.GetString(effect.Text), effect.Color, effect.BriefingSound);
    }
}
