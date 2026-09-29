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

    private ClientGameTicker _ticker;
    private StatusControlSystem _status;

    private int _minutes = -1;

    public StatusTabControl()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _ticker = _ent.System<ClientGameTicker>();
        _status = _ent.System<StatusControlSystem>();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        _status.OnInfoUpdated += UpdateInfoBlob;
        UpdateInfoBlob();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _status.OnInfoUpdated -= UpdateInfoBlob;
    }

    protected override void FrameUpdate(FrameEventArgs e)
    {
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
        ServerInfo.SetInfoBlob(_status.Info);
    }
}
