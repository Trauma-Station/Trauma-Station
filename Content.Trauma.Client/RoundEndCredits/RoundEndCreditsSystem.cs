using System.Linq;
using System.Numerics;
using Content.Client._RMC14.LinkAccount;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
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

    private float _timer;
    private const string Logo = "/Textures/Logo/logo.png";
    private const string Pixellari = "/Fonts/_Trauma/Pixellari.ttf";
    private const string GrandPixel = "/Fonts/_Trauma/Grand9K_Pixel.ttf";
    private ScrollContainer? _creditsContainer;
    private BoxContainer? _exitContainer;
    private const int SmallFontSize = 10;
    private const int NormalFontSize = 16;
    private const int BigFontSize = 24;
    private const int HeaderFontSize = 42;
    private bool Debug = false; // Set this to true if you want a bunch of dummy characters to spawn

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RoundEndMessageEvent>(OnRoundEnd);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent ev)
    {
        CloseCredits();
    }


    private void OnRoundEnd(RoundEndMessageEvent message)
    {
        var headerFont = new VectorFont(_cache.GetResource<FontResource>(Pixellari), HeaderFontSize);
        var bigFont = new VectorFont(_cache.GetResource<FontResource>(Pixellari), BigFontSize);
        var playerNameFont = new VectorFont(_cache.GetResource<FontResource>(GrandPixel), SmallFontSize);
        var normalFont = new VectorFont(_cache.GetResource<FontResource>(Pixellari), NormalFontSize);

        var texture = _cache.GetResource<TextureResource>(Logo);

        var mainCreditScroll = new ScrollContainer
        {
            SetSize = _clyde.MainWindow.Size,
            MouseFilter = Control.MouseFilterMode.Ignore,
            ReserveScrollbarSpace = false,
            HScrollEnabled = false,
            // Hidden = True, TODO when robust pr is done
        };

        var mainCreditVBox = new BoxContainer
        {
            Align = BoxContainer.AlignMode.Center,
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };

        var serverImage = new TextureRect
        {
            Margin = new Thickness(0, 1000, 0, 500),
            Texture = texture
        };

        var episodeNumber = new Label
        {
            Text = Loc.GetString("round-end-credits-trauma-episode", ("roundid",  message.RoundId), ("title", message.GamemodeTitle)),
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
            Margin =  new Thickness(0, 500, 0, 1500),
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
        mainCreditVBox.AddChild(MakeIntroJargon(bigFont)); // The larp
        mainCreditVBox.AddChild(MakeShoutOutBox(bigFont));
        mainCreditVBox.AddChild(castLabel);

        foreach (var player in message.AllPlayersEndInfo)
        {
            if (player.PlayerICName != null)
                mainCreditVBox.AddChild(MakePlayerInfoBox(player, playerNameFont, Color.White , true, false));
        }

        var sortedDepartments = _proto.EnumeratePrototypes<DepartmentPrototype>()
            .OrderByDescending(p => p.Weight)
            .ToList();

        foreach (var department in sortedDepartments)
        {
            mainCreditVBox.AddChild(MakeDepartmentContainer(department, headerFont, playerNameFont, message.AllPlayersEndInfo));
        }

        var antags = _proto.EnumeratePrototypes<AntagPrototype>()
            .OrderBy(p => p.Name)
            .ToList();

        foreach (var antag in antags)
        {
            mainCreditVBox.AddChild(MakeAntagBox(message.AllPlayersEndInfo, playerNameFont, headerFont, antag));
        }

        var lastwords = false;

        foreach (var player in message.AllPlayersEndInfo)
        {
            if (player.LastWords != null)
            {
                lastwords = true;
                break;
            }
        }

        if (lastwords)
        {
            mainCreditVBox.AddChild(MakeFamousLastWordsBox(bigFont));
            mainCreditVBox.AddChild(MakeLastWordsBox(playerNameFont, message.AllPlayersEndInfo));
        }

        mainCreditVBox.AddChild(thanksForPlaying);

        if (_random.Prob(0.01f))
            mainCreditVBox.AddChild(MakeKojimaBox(normalFont, bigFont));

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
        var normalSpeed = 240f;
        var speedUpDuration = 5f;
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

    private BoxContainer MakePlayerInfoBox(RoundEndMessageEvent.RoundEndPlayerInfo playerInfo, VectorFont font, Color color, bool fullInfo = false, bool addSprite = true)
    {
        var box = new BoxContainer
        {
            Align = BoxContainer.AlignMode.Center,
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MaxHeight = 100,
        };

        if (playerInfo.PlayerNetEntity != null && addSprite)
        {
            box.AddChild(new SpriteView(playerInfo.PlayerNetEntity.Value, EntityManager)
            {
                OverrideDirection = Direction.South,
                VerticalAlignment = Control.VAlignment.Center,
                SetSize = new Vector2(64, 64),
                VerticalExpand = true,
                Stretch = SpriteView.StretchMode.Fill,
                Margin = new Thickness(10, 0, 10, 5)
            });
        }

        var role = Loc.GetString(playerInfo.Role);
        var text = new Label
        {
            Name = playerInfo.PlayerICName,
            Text = fullInfo ? Loc.GetString("round-end-credits-trauma-player-name-role", ("name", playerInfo.PlayerICName ?? "Unknown"), ("role", role), ("player", playerInfo.PlayerOOCName)) : playerInfo.PlayerICName,
            Align = Label.AlignMode.Center,
            FontOverride =  font,
            FontColorOverride = color,
            Margin = new Thickness(15, 0, 15, 15),
        };

        box.AddChild(text);

        return box;
    }

    private BoxContainer MakeDepartmentContainer(DepartmentPrototype department, VectorFont fontHeader, VectorFont smallFont, RoundEndMessageEvent.RoundEndPlayerInfo[] players)
    {
        var text = new Label
        {
            Text = Loc.GetString(department.Name),
            FontOverride = fontHeader,
            HorizontalAlignment = Control.HAlignment.Center,
            FontColorOverride = department.Color,
        };
        var boxH = new GridContainer
        {
            Columns = 11,
            HorizontalAlignment = Control.HAlignment.Center,
        };
        var boxV = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalAlignment = Control.HAlignment.Center,
            Margin =  new Thickness(0, 150, 0, 50),
        };

        boxV.AddChild(text);
        boxV.AddChild(boxH);

        foreach (var playerInfo in players)
        {
            var belongsToDepartment = playerInfo.JobPrototypes.Any(jobId =>
                department.Roles.Contains(new ProtoId<JobPrototype>(jobId)));

            if (belongsToDepartment)
                boxH.AddChild(MakePlayerInfoBox(playerInfo, smallFont, Color.White));

            if (Debug)
            {
                for (int i = 0; i < 35; i++)
                {
                    boxH.AddChild(MakePlayerInfoBox(playerInfo, smallFont, Color.White));
                }
            }
        }

        return boxV;
    }

    private BoxContainer MakeAntagBox(RoundEndMessageEvent.RoundEndPlayerInfo[] players, VectorFont smallfont, VectorFont headerFont, AntagPrototype antag)
    {
        var boxH = new GridContainer
        {
            Columns = 11,
            HorizontalAlignment = Control.HAlignment.Center,
        };
        var boxV = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Align =  BoxContainer.AlignMode.Center,
            Margin =  new Thickness(0, 150, 0, 50),
        };
        if (!string.IsNullOrWhiteSpace(antag.CreditImage) && _cache.TryGetResource<TextureResource>(antag.CreditImage, out var texture))
        {
            var image = new TextureRect
            {
                Texture = texture,
                HorizontalAlignment = Control.HAlignment.Center,
            };
            boxV.AddChild(image);
        }
        else
        {
            var text = new Label
            {
                HorizontalAlignment = Control.HAlignment.Center,
                Text = Loc.GetString(antag.Name),
                FontOverride = headerFont,
                FontColorOverride = antag.Color,
            };
            boxV.AddChild(text);
        }

        boxV.AddChild(boxH);

        var playersInSection = false;

        foreach (var player in players)
        {
            if (!player.Antag)
                continue;

            foreach (var playerAntag in player.AntagPrototypes)
            {
                if (playerAntag == antag.ID && !antag.DontShowInCredits)
                {
                    boxH.AddChild(MakePlayerInfoBox(player, smallfont, antag.Color));
                    playersInSection = true;
                }
            }
        }

        if (playersInSection == false)
        {
            var boxEmpty = new BoxContainer();
            return boxEmpty;
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
            Text = Loc.GetString("round-end-credits-trauma-close"),
            HorizontalAlignment =  Control.HAlignment.Right,
            VerticalAlignment = Control.VAlignment.Top,
        };
        button.OnPressed += _ => CloseCredits();

        buttonBox.AddChild(button);

        _exitContainer = buttonBox;

        return buttonBox;
    }

    private BoxContainer MakeIntroJargon(VectorFont font)
    {
        var box = new BoxContainer
        {
            Align =  BoxContainer.AlignMode.Center,
            Margin =   new Thickness(0, 0, 0, 150),
        };

        var label = new Label
        {
            Text =  Loc.GetString("round-end-credits-trauma-jargon"),
            Align =  Label.AlignMode.Center,
            FontOverride = font,
        };

        box.AddChild(label);

        return box;
    }

    private BoxContainer MakeKojimaBox(VectorFont directorFont, VectorFont kojimaFont)
    {
        var vBox = new BoxContainer
        {
            Align = BoxContainer.AlignMode.Center,
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin =  new Thickness(0, 0, 0, 300),
        };
        var directedby = new Label
        {
            Text = Loc.GetString("round-end-credits-trauma-created"),
            Align = Label.AlignMode.Center,
            FontOverride = directorFont,
        };
        var kojima = new Label
        {
            Text = Loc.GetString("round-end-credits-trauma-kojima"),
            Align = Label.AlignMode.Center,
            FontOverride = kojimaFont,
        };
        vBox.AddChild(directedby);
        vBox.AddChild(kojima);

        return vBox;
    }

    private BoxContainer MakeFamousLastWordsBox(VectorFont font)
    {
        var box = new BoxContainer
        {
            Align = BoxContainer.AlignMode.Center,
            Margin =   new Thickness(0, 0, 0, 50),
        };

        var label = new Label
        {
            Text = Loc.GetString("round-end-credits-trauma-lastwords-title"),
            FontOverride =  font,
        };

        box.AddChild(label);

        return box;
    }

    private BoxContainer MakeShoutOutBox(VectorFont font)
    {
        var shoutout = "John Nanotrasen";

        if (_linkAccount.GetPatrons().Count != 0)
            shoutout = _random.Pick(_linkAccount.GetPatrons()).Name;

        var box = new BoxContainer
        {
            Align = BoxContainer.AlignMode.Center,
            Margin =   new Thickness(0, 0, 0, 200),
        };

        var label = new Label
        {
            Text = Loc.GetString("round-end-credits-trauma-director", ("shoutout", shoutout)),
            FontOverride =  font,
        };

        box.AddChild(label);

        return box;
    }

    private BoxContainer MakeLastWordsBox(VectorFont font, RoundEndMessageEvent.RoundEndPlayerInfo[] players)
    {
        var box = new BoxContainer
        {
            Align = BoxContainer.AlignMode.Center,
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };

        foreach (var player in players)
        {
            if (player.LastWords != null)
            {
                var label = new Label
                {
                    FontOverride = font,
                    Text = Loc.GetString("round-end-credits-trauma-lastwords",
                        ("words", player.LastWords),
                        ("player", player.PlayerICName ?? "Unknown")),
                    Align = Label.AlignMode.Center,
                };
                box.AddChild(label);
            }
        }

        return box;
    }

    #endregion
}
