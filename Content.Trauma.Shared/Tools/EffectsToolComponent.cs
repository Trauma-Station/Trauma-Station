using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Tools;

/// <summary>
/// When used on an entity, starts a doafter which will run some entity effects on the target entity.
/// This effectively lets you make a custom tool, or an admin abuse stick.
/// Also has a verb for funny targets e.g. bags.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EffectsToolSystem))]
public sealed partial class EffectsToolComponent : Component
{
    /// <summary>
    /// Whitelist for target entities.
    /// Targets that fail this will be ignored.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist for target entities.
    /// Targets that pass this, even if they pass the whitelist, will be ignored.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// If non-null, popup to show when trying to use the tool on an invalid target.
    /// Should the popup be shown, the interaction will also be handled, preventing other components from running logic.
    /// Has "target" passed.
    /// </summary>
    [DataField]
    public LocId? InvalidPopup;

    /// <summary>
    /// Effects to run on the target.
    /// </summary>
    [DataField(required: true)]
    public List<EntityEffect> Effects = new();

    /// <summary>
    /// How long the doafter takes.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Popup shown to the user after it's used.
    /// Has "target" and "used" passed.
    /// </summary>
    [DataField(required: true)]
    public LocId UserPopup;

    /// <summary>
    /// Popup shown to everyone else after it's used.
    /// Has "target", "used" and "user" passed.
    /// </summary>
    [DataField(required: true)]
    public LocId OthersPopup;

    /// <summary>
    /// Text for the verb.
    /// </summary>
    [DataField(required: true)]
    public LocId VerbText;

    /// <summary>
    /// Optional icon for the verb.
    /// </summary>
    [DataField]
    public SpriteSpecifier? VerbIcon;

    /// <summary>
    /// Sound to play when used.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}

/// <summary>
/// Raised on an effect tool when the doafter starts and ends to prevent an effect tool from working.
/// If this is cancelled the user should be shown a popup.
/// </summary>
[ByRefEvent]
public record struct EffectsToolUseAttemptEvent(EntityUid Target, EntityUid User, bool Cancelled = false);

/// <summary>
/// Raised on an effect tool after an effect has successfully ran.
/// </summary>
[ByRefEvent]
public record struct EffectsToolUsedEvent(EntityUid Target, EntityUid User);

/// <summary>
/// Entity effect args that also passes a user and tool.
/// Raises <see cref="EffectsToolUsedEvent"/> afterwards if an effect called <c>Handle()</c>.
/// </summary>
public record class EntityEffectToolArgs(EntityUid target, EntityUid user, EntityUid tool, IEntityManager entMan) : EntityEffectBaseArgs(target, entMan)
{
    public EntityUid User = user;
    public EntityUid Tool = tool;
    public bool Handled;

    public void Handle()
    {
        Handled = true;
    }
}
