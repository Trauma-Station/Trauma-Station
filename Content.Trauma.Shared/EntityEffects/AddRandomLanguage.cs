// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Components;
using Content.Trauma.Shared.Language.Systems;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Changes the target's active language to one randomly picked from a list.
/// </summary>
public sealed partial class AddRandomLanguage : EntityEffectBase<AddRandomLanguage>
{
    [DataField]
    public List<ProtoId<LanguagePrototype>> Languages = new()
    {
        "Monkey",
        "RootSpeak",
        "Moffic",
        "Draconic",
        "Calcic",
        "Bubblish",
        "NewKinPidgin",
        "Xeeplian",
        "Elyran",
        "Freespeak",
        "NovuNederic",
        "Tradeband",
        "SpaceItalian",
        "Carptongue"
    };

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed class AddRandomLanguageSystem : EntityEffectSystem<LanguageSpeakerComponent, AddRandomLanguage>
{
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<LanguageSpeakerComponent> ent, ref EntityEffectEvent<AddRandomLanguage> args)
    {
        var lang = _random.Pick(args.Effect.Languages);
        _language.AddLanguage(ent.AsNullable(), lang);
        _language.SetLanguage(ent.AsNullable(), lang);
    }
}
