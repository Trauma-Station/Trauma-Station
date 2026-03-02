using Content.Server.Explosion.EntitySystems;
using Content.Shared.Chat;

namespace Content.Server.Shuttles.Systems;

/// <summary>
/// Trauma - shuttle spam checker
/// </summary>
public sealed partial class EmergencyShuttleSystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;

    private EntityUid? _lastRepealUser;
    private double _repealTimestamp = 0;
    private int _consoleSpams = 0;

    private void SpamChecker(EntityUid uid, EntityUid? repealUser)
    {
        if (_timing.RealTime.TotalSeconds - _repealTimestamp <= 5 && repealUser == _lastRepealUser)
        {
            _consoleSpams += 1;
            if (_consoleSpams == 5)
            {
                _chatSystem.TrySendInGameICMessage(uid, "Error 452: RepealButtonOveruseException", InGameICChatType.Speak, hideChat: true);
                _chatSystem.TrySendInGameICMessage(uid, "The maximum threshold for pressing the repeal button has been exceeded.", InGameICChatType.Speak, hideChat: true);
                _chatSystem.TrySendInGameICMessage(uid, "Please refrain from further attempts at repeal at this time.", InGameICChatType.Speak, hideChat: true);
            }
            else if (_consoleSpams >= 7)
            {
                _explosion.TriggerExplosive(uid);
                _consoleSpams = 0;
            }
        }

        _lastRepealUser = repealUser;
        _repealTimestamp = _timing.RealTime.TotalSeconds;
    }
}
