// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.CosmicCult.Components;

/// <summary>
/// Component for revealing cosmic cultists to the crew, but not if the cultist's face is covered.
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CosmicSubtleMarkComponent : Component
{
    [DataField]
    public LocId ExamineText = "cosmic-examine-text-subtle-mark";

    [DataField]
    public SpriteSpecifier Sprite = new SpriteSpecifier.Rsi(new("/Textures/_DV/CosmicCult/Effects/cultsubtlerevealed.rsi"), "default");

    /// <summary>
    /// If not null, the mark will disappear after a specified moment.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? ExpireTimer = null;
}
