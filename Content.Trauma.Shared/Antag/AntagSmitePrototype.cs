// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Antag;

/// <summary>
/// Lets you define an antag smite in a few lines of YML.
/// Relies on the gamerule doing the work instead of some random function.
/// </summary>
[Prototype]
public sealed partial class AntagSmitePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The antag to use.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AntagPrototype> Antag;

    /// <summary>
    /// The gamerule to create if none with <see cref="RuleComp"/> are found.
    /// Loading grids for it will be disabled.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Rule;

    /// <summary>
    /// Name of the gamerule's component to look for to find an existing rule.
    /// </summary>
    [DataField(required: true, serverOnly: true)]
    public string RuleComp = string.Empty;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// Optional whitelist that the target's mob must match.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Optional blacklist that the target's mob must not match.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
