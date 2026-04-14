// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.DeepFryer.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DeepFryerComponent : Component
{
    [DataField]
    public TimeSpan TimeToDeepFry = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan FryFinishTime = TimeSpan.Zero;

    [DataField]
    public float HeatDamage = 8f;

    [DataField]
    public float SolutionSpentPerFry = 10f;

    [DataField]
    public float HeatToAddToSolution = 500f;

    [DataField]
    public float MaxHeat = 500f;

    [DataField]
    public EntProtoId AshedItemToSpawn = "Ash";

    /// <summary>
    /// Which components should we remove
    /// </summary>
    [DataField]
    public ComponentRegistry ComponentsToRemove = new();

    /// <summary>
    /// Which components should we add to all objects that are deep-fried
    /// </summary>
    [DataField]
    public ComponentRegistry ComponentsToAdd = new();

    /// <summary>
    /// which components get added to any non-sentient objects (doesn't have a mind container)
    /// </summary>
    [DataField]
    public ComponentRegistry ComponentsToAddObjects = new();

    /// <summary>
    /// which components get removed to any non-sentient objects (doesn't have a mind container)
    /// </summary>
    [DataField]
    public ComponentRegistry ComponentsToRemoveObjects = new();

    [DataField]
    public SoundPathSpecifier StartSound = new("/Audio/_Trauma/Machines/DeepFryer/deep_fryer_initial.ogg");

    [DataField]
    public SoundPathSpecifier FinishSound = new("/Audio/_Trauma/Machines/DeepFryer/deep_fryer_done.ogg");

    [DataField]
    public List<EntityUid> StoredObjects = new();

    [DataField]
    public string SolutionContainer = "food";

    [DataField]
    public string FryerSolutionContainer = "fryer";

    /// <summary>
    /// Name of the containers to remove e.g bag storage
    /// </summary>
    [DataField]
    public List<string> ContainersToRemove = new ();

    /// <summary>
    /// The last played start/finish sound.
    /// Stopped when toggling it to prevent sound spamming.
    /// </summary>
    [DataField]
    public EntityUid? Sound;

    /// <summary>
    /// Last person that closed the fryer, reset every time it's closed.
    /// </summary>
    [DataField]
    public EntityUid? LastUser;
}

[Serializable, NetSerializable]
public enum DeepFryerVisuals : byte
{
    Open,
    Frying,
    BigFrying
}
