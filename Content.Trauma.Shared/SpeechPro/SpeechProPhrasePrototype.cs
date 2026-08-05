// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.SpeechPro;

/// <summary>
/// A phrase the Speech Pro can speak.
/// </summary>
[Prototype]
public sealed partial class SpeechProPhrasePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Button text shown in the Speech Pro UI.
    /// </summary>
    [DataField(required: true)]
    public LocId Button;

    /// <summary>
    /// Message spoken when this phrase is selected.
    /// </summary>
    [DataField(required: true)]
    public LocId Message;
}

/// <summary>
/// A UI section containing related Speech Pro phrases.
/// </summary>
[Prototype]
public sealed partial class SpeechProPhraseGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Section heading shown in the Speech Pro UI.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Sort order for the group in the Speech Pro UI.
    /// </summary>
    [DataField]
    public int Order;

    /// <summary>
    /// Phrase prototypes shown in this section.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<SpeechProPhrasePrototype>> Phrases = [];
}
