// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Phones;

/// <summary>
/// This is a prototype for phone category's in the phone-book
/// </summary>
[Prototype]
public sealed partial class PhoneCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { set; get; } = default!;

    /// <summary>
    /// The name of the category e.g "command"
    /// </summary>
    [DataField (required: true)]
    public required LocId Text;

    /// <summary>
    /// The appearance order of the category, 1 is high
    /// </summary>
    [DataField(required: true)]
    public required int Index;

    /// <summary>
    /// Should the phone be hidden in the phonebook
    /// </summary>
    [DataField]
    public bool HideInPhonebook = false;
}
