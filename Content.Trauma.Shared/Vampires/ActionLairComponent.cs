// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Trauma.Shared.Heretic.Prototypes;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Vampires;

/// <summary>
/// Action that allows you to choose a coffin as your lair.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ActionLairComponent : Component
{
    /// <summary>
    /// The new coffin components to copy over.
    /// </summary>
    [DataField]
    public ProtoId<ComponentRegistryPrototype> LairComponents = "VampireCoffin";

    /// <summary>
    /// How long the do after for creating the lair is.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10f);

    /// <summary>
    /// Stores the vampiric rune effect entity, so it can be deleted if the do after cancels
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Effect;

    /// <summary>
    /// The sound that plays before the lair is made.
    /// </summary>
    [DataField]
    public SoundSpecifier BeforeCreationSound = new SoundPathSpecifier("/Audio/_Goobstation/Misc/enter_blood.ogg");

    /// <summary>
    /// The sound that plays when the lair gets made.
    /// </summary>
    [DataField]
    public SoundSpecifier CreationSound = new SoundPathSpecifier("/Audio/_Goobstation/Hallucinations/im_here1.ogg");
}

public sealed partial class ActionLairEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class VampireLairDoAfterEvent : SimpleDoAfterEvent;
