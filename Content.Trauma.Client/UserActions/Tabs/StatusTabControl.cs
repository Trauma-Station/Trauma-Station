// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.GameTicking;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.UserActions.Tabs;

[GenerateTypedNameReferences]
public sealed partial class StatusTabControl : BaseTabControl, IOnSystemChanged<ClientGameTicker>, IOnSystemChanged<StatusControlSystem>
{
    [Dependency] private IGameTiming _timing = default!;

    private ClientGameTicker? _ticker;
    private StatusControlSystem? _status;

    private int _minutes = -1;

    public StatusTabControl()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    public void OnSystemLoaded(ClientGameTicker system)
    {
        _ticker = system;
    }

    public void OnSystemUnloaded(ClientGameTicker system)
    {
        _ticker = null;
    }

    public void OnSystemLoaded(StatusControlSystem system)
    {
        _status = system;
        system.OnInfoUpdated += UpdateInfoBlob;
        UpdateInfoBlob();
    }

    public void OnSystemUnloaded(StatusControlSystem system)
    {
        system.OnInfoUpdated -= UpdateInfoBlob;
        _status = null;
    }

    protected override void FrameUpdate(FrameEventArgs e)
    {
        if (_ticker is not { })
            return;

        var time = _timing.CurTime.Subtract(_ticker.RoundStartTimeSpan);
        if (time.Minutes == _minutes)
            return;

        _minutes = time.Minutes;
        StationTime.Text = Loc.GetString("lobby-state-player-status-round-time", ("hours", time.Hours), ("minutes", time.Minutes));
    }

    public override bool UpdateState()
    {
        UpdateInfoBlob();
        return true;
    }

    private void UpdateInfoBlob()
    {
        if (_status?.Info is { } info)
            ServerInfo.SetInfoBlob(info);
    }
}
