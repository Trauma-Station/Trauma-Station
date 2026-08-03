using Content.Goobstation.Common.Actions;

namespace Content.Client.Actions;

public sealed partial class ActionsSystem
{
    public event Action<EntityUid>? ActionsSaved;
    public event Action<EntityUid>? ActionsLoaded;

    [SubscribeNetworkEvent]
    private void OnLoadActions(LoadActionsEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession != _playerManager.LocalSession)
            return;

        ActionsLoaded?.Invoke(GetEntity(msg.Entity));
    }

    public override void SaveActions(EntityUid performer)
    {
        if (_playerManager.LocalEntity != performer)
            return;

        ActionsSaved?.Invoke(performer);
    }

    public override void LoadActions(EntityUid performer)
    {
        if (_playerManager.LocalEntity != performer)
            return;

        ActionsLoaded?.Invoke(performer);
    }
}
