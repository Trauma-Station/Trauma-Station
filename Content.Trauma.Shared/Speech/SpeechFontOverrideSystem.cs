// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Common.Speech;

namespace Content.Trauma.Shared.Speech;

public sealed class SpeechFontOverrideSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeechFontOverrideComponent, SpeechFontOverrideEvent>(OnOverride);
    }

    private void OnOverride(Entity<SpeechFontOverrideComponent> ent, ref SpeechFontOverrideEvent args)
    {
        if (ent.Comp.SourceOnly && args.Source != ent.Owner)
            return;

        args.Font = ent.Comp.Font;
    }
}
