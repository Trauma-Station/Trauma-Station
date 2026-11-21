// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Clothing.Sealable;

/// <summary>
/// Component used to designate contol of sealable clothing. It'll contain action to seal clothing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SealableClothingControlComponent : Component
{
    /// <summary>
    /// True if clothing in sealing/unsealing process, false if not
    /// </summary>
    [ViewVariables]
    public bool IsInProcess => ProcessQueue.Count > 0;

    /// <summary>
    ///     True if clothing is currently sealed and need to start unsealing process. False if opposite.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsCurrentlySealed;

    /// <summary>
    /// Queue of attached parts that should be sealed/unsealed
    /// </summary>
    [DataField, AutoNetworkedField]
    public Queue<NetEntity> ProcessQueue = new();

    /// <summary>
    /// Doafter time for other players to start sealing via stripping menu
    /// </summary>
    [DataField]
    public TimeSpan NonWearerSealingTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// if true; after ClothingControlSealCompleteEvent it will unToggle the control
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UnequipAfterUnseal;

    #region Popups & Sounds

    [DataField]
    public LocId ToggleFailedPopup = "sealable-clothing-equipment-not-toggled";

    [DataField]
    public LocId SealFailedPopup = "sealable-clothing-equipment-seal-failed";

    [DataField]
    public LocId SealedInProcessToggleFailPopup = "sealable-clothing-sealed-process-toggle-fail";

    [DataField]
    public LocId UnsealedInProcessToggleFailPopup = "sealable-clothing-unsealed-process-toggle-fail";

    [DataField]
    public LocId CurrentlySealedToggleFailPopup = "sealable-clothing-sealed-toggle-fail";

    [DataField]
    public LocId SealBrokenPopup = "sealable-clothing-seal-was-broken";

    [DataField]
    public LocId VerbText = "sealable-clothing-seal-verb";

    [DataField]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/_Goobstation/Machines/ErrorBeep2.wav");

    [DataField]
    public SoundSpecifier SealCompleteSound = new SoundPathSpecifier("/Audio/_Goobstation/Mecha/nominal.ogg");

    [DataField]
    public SoundSpecifier UnsealCompleteSound = new SoundPathSpecifier("/Audio/_Goobstation/Machines/computer_end.ogg");

    [DataField]
    public SoundSpecifier GenericSuitWarning = new SoundPathSpecifier("/Audio/_Goobstation/Machines/MaxTempAlertCut.wav");

    #endregion
}
