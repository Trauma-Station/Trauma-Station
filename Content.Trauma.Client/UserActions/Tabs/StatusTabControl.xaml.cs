// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.GameTicking;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.UserActions.Tabs;

[GenerateTypedNameReferences]
public sealed partial class StatusTabControl : BaseTabControl
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IGameTiming _timing = default!;

    private ClientGameTicker? _ticker;
    private StatusControlSystem? _status;

    private int _minutes = -1;

    public StatusTabControl()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _ent.TrySystem(out _ticker);
        _ent.TrySystem(out _status);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        _ent.TrySystem(out _ticker);
        _ent.TrySystem(out _status);

        _status?.OnInfoUpdated += UpdateInfoBlob;
        UpdateInfoBlob();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _status?.OnInfoUpdated -= UpdateInfoBlob;
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
