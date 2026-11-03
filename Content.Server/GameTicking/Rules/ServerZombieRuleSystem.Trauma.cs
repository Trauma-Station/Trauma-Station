using Content.Shared.GameTicking.Rules.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ServerZombieRuleSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    /// <summary>
    /// Trauma - Sends a CBurn shuttle when zombies get to a certain percentage of infected crew.
    /// </summary>
    private void CheckCBurnCall(ZombieRuleComponent comp)
    {
        if (comp.ZombieCBurnCalled || GetInfectedFraction(false) < comp.ZombieCBurnCallPercentage)
            return;

        foreach (var station in Station.GetStations())
        {
            _chat.DispatchStationAnnouncement(station, Loc.GetString("zombie-cburn-call"), colorOverride: Color.Crimson);
        }
        GameTicker.StartGameRule(comp.ZombieCBurnEvent);
        comp.ZombieCBurnCalled = true;
    }
}
