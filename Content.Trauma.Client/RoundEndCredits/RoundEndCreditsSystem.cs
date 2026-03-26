// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._RMC14.LinkAccount;
using Content.Shared.GameTicking;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.CCVar;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Client.RoundEndCredits;

public sealed class RoundEndCreditsSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private float _timer;
    private EndRoundCreditsControl? _creditsContainer;
    private BoxContainer? _exitContainer;
    private bool _showCredits = true;
    private float _uiScale;
    private bool Debug = false; // Set this to true if you want a bunch of dummy characters to spawn

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RoundEndMessageEvent>(OnRoundEnd);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundCleanup);

        Subs.CVar(_cfg, TraumaCVars.PlayMovieEndCredits, x => _showCredits = x, true);
        Subs.CVar(_cfg, CVars.DisplayUIScale, x => _uiScale = x, true);
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        if (!_showCredits)
            return;

        CloseCredits();
    }

    private void OnRoundEnd(RoundEndMessageEvent message)
    {
        if (!_showCredits)
            return;

        var shoutout = "John Nanotrasen";
        if (_linkAccount.GetPatrons().Count != 0)
            shoutout = _random.Pick(_linkAccount.GetPatrons()).Name;

        var credits = new EndRoundCreditsControl();
        credits.SetSize = _clyde.MainWindow.Size / _uiScale;
        credits.Populate(message, _cache, _proto, shoutout, Debug);

        var rand = new RobustRandom();
        rand.SetSeed(message.RoundId);

        if (rand.Prob(0.01f)) // Kojima is god...?
            credits.AddKojimaBox(_cache);

        _creditsContainer = credits;

        _ui.WindowRoot.AddChild(credits);
        _ui.WindowRoot.AddChild(AddExitCreditsButton());
    }

    public override void FrameUpdate(float frameTime)
    {
        if (_creditsContainer is null)
            return;

        base.FrameUpdate(frameTime);
        _timer += frameTime;
        var scroll = _creditsContainer.GetScrollValue();
        var scrollSpeed = GetScrollingSpeed(TimeSpan.FromSeconds(_timer));
        _creditsContainer.SetScrollValue(scroll + new Vector2(0f, scrollSpeed * frameTime));
    }

    public float GetScrollingSpeed(TimeSpan time)
    {
        var normalSpeed = 200f;
        var speedUpDuration = 10f;
        var easing = Easings.InSine;
        return easing(Math.Min((float)time.TotalSeconds / speedUpDuration, 1f)) * normalSpeed;
    }

    private void CloseCredits()
    {
        if (_creditsContainer != null)
            _ui.WindowRoot.RemoveChild(_creditsContainer);

        if (_exitContainer != null)
            _ui.WindowRoot.RemoveChild(_exitContainer);

        _creditsContainer = null;
        _exitContainer = null;
    }

    private BoxContainer AddExitCreditsButton()
    {
        var buttonBox = new BoxContainer
        {
            HorizontalAlignment = Control.HAlignment.Right,
            VerticalAlignment = Control.VAlignment.Top,
        };

        var button = new Button
        {
            Text = Loc.GetString("round-end-credits-trauma-close"),
            HorizontalAlignment = Control.HAlignment.Right,
            VerticalAlignment = Control.VAlignment.Top,
        };
        button.OnPressed += _ => CloseCredits();

        buttonBox.AddChild(button);
        _exitContainer = buttonBox;

        return buttonBox;
    }
}
