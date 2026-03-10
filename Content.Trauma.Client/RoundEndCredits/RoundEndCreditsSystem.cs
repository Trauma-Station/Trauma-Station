using System.Linq;
using System.Numerics;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.RoundEndCredits;


public sealed class RoundEndCreditsSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private float _timer;
    private const string Logo = "/Textures/Logo/logo.png";
    private const string Pixellari = "/Fonts/_Trauma/Pixellari.ttf";
    private ScrollContainer? _creditsContainer;
    private BoxContainer? _exitContainer;
    private const int SmallFontSize = 10;
    private const int NormalFontSize = 14;
    private const int BigFontSize = 24;
    private const int HeaderFontSize = 36;

    private Dictionary<string, Control> _departmentContainers = new();
    private Dictionary<string, Control> _antagContainers = new();
    private Dictionary<string, (RoundEndMessageEvent.RoundEndPlayerInfo Info, string? DepartmentId)> _playerContainers = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RoundEndMessageEvent>(OnRoundEnd);
    }


    private void OnRoundEnd(RoundEndMessageEvent message)
    {
        var headerFont = new VectorFont(_cache.GetResource<FontResource>(Pixellari), HeaderFontSize);
        var bigFont = new VectorFont(_cache.GetResource<FontResource>(Pixellari), BigFontSize);
        var normalFont = new VectorFont(_cache.GetResource<FontResource>(Pixellari), NormalFontSize);
        var smallFont = new VectorFont(_cache.GetResource<FontResource>(Pixellari), SmallFontSize);

        var texture = _cache.GetResource<TextureResource>(Logo);

        var mainCreditScroll = new ScrollContainer
        {
            SetSize = _clyde.MainWindow.Size,
            MouseFilter = Control.MouseFilterMode.Ignore,
            ReserveScrollbarSpace = false,
        };

        var mainCreditVBox = new BoxContainer
        {
            Align = BoxContainer.AlignMode.Center,
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };

        var serverImage = new TextureRect
        {
            Margin = new Thickness(0, 1000, 0, 500),
            Texture = texture
        };

        var episodeNumber = new Label
        {
            Text = Loc.GetString("round-end-credits-trauma-episode", ("roundid",  message.RoundId)),
            Align = Label.AlignMode.Center,
            FontOverride = bigFont,
            Margin =  new Thickness(0, 0, 0, 250),
        };

        var castLabel = new Label
        {
            Text = Loc.GetString("round-end-credits-trauma-cast"),
            Align = Label.AlignMode.Center,
            FontOverride = bigFont,
            Margin =  new Thickness(0, 0, 0, 150),
        };

        var thanksForPlaying = new Label
        {
            Text = Loc.GetString("round-end-credits-trauma-thankyou"),
            Align = Label.AlignMode.Center,
            FontOverride = bigFont,
            Margin =  new Thickness(0, 0, 0, 150),
        };

        var serverImageBox = new BoxContainer
        {
            Align =  BoxContainer.AlignMode.Center,
            VerticalAlignment = Control.VAlignment.Top
        };

        serverImageBox.AddChild(serverImage);

        mainCreditScroll.AddChild(mainCreditVBox);
        mainCreditVBox.AddChild(serverImageBox);
        mainCreditVBox.AddChild(episodeNumber);
        mainCreditVBox.AddChild(castLabel);

        foreach (var player in message.AllPlayersEndInfo)
        {
            if (player.PlayerICName != null)
                mainCreditVBox.AddChild(MakePlayerInfoBox(player, normalFont));
        }

        var sortedDepartments = _proto.EnumeratePrototypes<DepartmentPrototype>()
            .OrderByDescending(p => p.Weight)
            .ToList();

        foreach (var department in sortedDepartments)
        {
            mainCreditVBox.AddChild(MakeDepartmentContainer(department, headerFont, smallFont));
        }

        mainCreditVBox.AddChild(thanksForPlaying);
        _creditsContainer = mainCreditScroll;
        AddEndRoundCredits(mainCreditScroll);
    }

    private void AddEndRoundCredits(ScrollContainer creditScroll)
    {
        _ui.WindowRoot.AddChild(creditScroll);
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

    #region Helpers

    public float GetScrollingSpeed(TimeSpan time)
    {
        var normalSpeed = 70f;
        var speedUpDuration = 3f;
        var easing = Easings.InSine;
        return easing(Math.Min((float)time.TotalSeconds / speedUpDuration, 1f)) * normalSpeed;
    }

    private void CloseCredits()
    {
        if (_creditsContainer != null)
            _ui.WindowRoot.RemoveChild(_creditsContainer);

        if (_exitContainer != null)
            _ui.WindowRoot.RemoveChild(_exitContainer);
    }

    private BoxContainer MakePlayerInfoBox(RoundEndMessageEvent.RoundEndPlayerInfo playerInfo, VectorFont font)
    {
        var box = new BoxContainer
        {
            Align = BoxContainer.AlignMode.Center,
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MaxHeight = 100,
        };

        if (playerInfo.PlayerNetEntity != null)
        {
            box.AddChild(new SpriteView(playerInfo.PlayerNetEntity.Value, EntityManager)
            {
                OverrideDirection = Direction.South,
                VerticalAlignment = Control.VAlignment.Center,
                SetSize = new Vector2(64, 64),
                VerticalExpand = true,
                Stretch = SpriteView.StretchMode.Fill,
                Margin = new Thickness(3, 0, 3, 0)
            });
        }

        var role = Loc.GetString(playerInfo.Role);
        var text = new Label
        {
            Name = playerInfo.PlayerICName,
            Text = playerInfo.PlayerICName + "" + Loc.GetString("round-end-summary-window-player-name-role", ("role", role), ("player", playerInfo.PlayerOOCName)),
            Align = Label.AlignMode.Center,
            FontOverride =  font,
            Margin = new Thickness(0, 0, 0, 20)
        };

        box.AddChild(text);
        var playerDepartment = _proto.EnumeratePrototypes<DepartmentPrototype>()
            .FirstOrDefault(d => d.Roles.Contains(playerInfo.Role));

        _playerContainers[playerInfo.Role] = (playerInfo, playerDepartment?.ID);

        return box;
    }

    private BoxContainer MakePlayerInfoBoxShort(RoundEndMessageEvent.RoundEndPlayerInfo playerInfo, VectorFont font)
    {
        var box = new BoxContainer
        {
            Align = BoxContainer.AlignMode.Center,
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MaxHeight = 100,
        };

        if (playerInfo.PlayerNetEntity != null)
        {
            box.AddChild(new SpriteView(playerInfo.PlayerNetEntity.Value, EntityManager)
            {
                OverrideDirection = Direction.South,
                VerticalAlignment = Control.VAlignment.Center,
                SetSize = new Vector2(64, 64),
                VerticalExpand = true,
                Stretch = SpriteView.StretchMode.Fill,
                Margin = new Thickness(3, 0, 3, 0)
            });
        }

        var text = new Label
        {
            Name = playerInfo.PlayerICName,
            Text = playerInfo.PlayerICName,
            Align = Label.AlignMode.Center,
            FontOverride =  font,
            Margin = new Thickness(0, 0, 0, 10)
        };

        box.AddChild(text);

        return box;
    }

    private BoxContainer MakeDepartmentContainer(DepartmentPrototype department, VectorFont fontHeader, VectorFont smallFont)
    {
        var text = new Label
        {
            Text = Loc.GetString(department.Name),
            FontOverride = fontHeader,
            HorizontalAlignment = Control.HAlignment.Center,
            FontColorOverride = department.Color,
        };
        var boxH = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Align =  BoxContainer.AlignMode.Center,
        };
        var boxV = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Align =  BoxContainer.AlignMode.Center,
        };

        boxV.AddChild(boxH);
        boxV.AddChild(text);

        foreach (var player in _playerContainers)
        {
            boxH.AddChild(MakePlayerInfoBoxShort(player.Value.Info, smallFont));
        }

        return boxV;
    }
    private BoxContainer AddExitCreditsButton()
    {
        var buttonBox = new BoxContainer
        {
            HorizontalAlignment = Control.HAlignment.Right,
            VerticalAlignment = Control.VAlignment.Top
        };

        var button = new Button
        {
            Text = "Close Credits",
            HorizontalAlignment =  Control.HAlignment.Right,
            VerticalAlignment = Control.VAlignment.Top,
        };
        button.OnPressed += _ => CloseCredits();

        buttonBox.AddChild(button);

        _exitContainer = buttonBox;

        return buttonBox;
    }

    #endregion
}
