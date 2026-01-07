// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EmptyScroll;

/// <summary>
/// A prayer that can be used with <see cref="ScrollPrayerPrototype"/> to do things to the prayee.
/// </summary>
[Prototype]
public sealed partial class ScrollPrayerPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// GIVE or TEACH
    /// </summary>
    [DataField(required: true)]
    public string Verb = string.Empty;

    /// <summary>
    /// Valid last lines of the prayer, all caps.
    /// </summary>
    [DataField(required: true)]
    public List<string> Subjects = new();

    /// <summary>
    /// Items to give to the prayee and try to put inhand.
    /// </summary>
    [DataField]
    public EntityTableSelector? Items;

    /// <summary>
    /// Effects to run on the prayee.
    /// </summary>
    [DataField]
    public EntityEffect[] Effects = [];
}
