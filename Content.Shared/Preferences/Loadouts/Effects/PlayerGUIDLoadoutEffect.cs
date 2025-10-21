using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;


namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Checks for a specific player GUID.
/// </summary>
public sealed partial class PlayerGUIDLoadoutEffect : LoadoutEffect
{
    [DataField]
    public string Guid {  get; set; }

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (session == null)
        {
            reason = FormattedMessage.Empty;
            return true;
        }

        if (session.UserId == new Guid(Guid))
        {
            reason = null;
            return true;
        }
        reason = FormattedMessage.FromUnformatted(Loc.GetString("loadout-group-player-restriction"));
        return false;
    }
}
