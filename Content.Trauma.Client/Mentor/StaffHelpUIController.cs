// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Client.Administration.Systems;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Shared.Input;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Mentor;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Trauma.Client.Mentor;

public sealed partial class StaffHelpUIController : UIController, IOnSystemChanged<BwoinkSystem>
{
    [Dependency] private AHelpUIController _aHelp = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPlayerManager _player = default!;
    [UISystemDependency] private readonly AudioSystem? _audio = default!;

    private readonly Dictionary<NetUserId, List<MentorMessage>> _messages = new();
    private readonly Dictionary<NetUserId, string> _destinationNames = new();

    private bool _isMentor;
    private StaffHelpWindow? _staffHelpWindow;
    private MentorHelpWindow? _mentorHelpWindow;
    private MentorWindow? _mentorWindow;
    private SoundSpecifier? _mHelpSound;
    private bool _unread;

    public override void Initialize()
    {
        _net.RegisterNetMessage<MentorStatusMsg>(OnMentorStatus);
        _net.RegisterNetMessage<MentorMessagesReceivedMsg>(OnMentorHelpReceived);
        _net.RegisterNetMessage<MentorSendMessageMsg>();
        _net.RegisterNetMessage<MentorHelpMsg>();

        AHelpUIController.OnLoad += OnLoad;

        _config.OnValueChanged(TraumaCVars.MentorHelpSound, v => _mHelpSound = new SoundPathSpecifier(v), true);
    }

    private void OnLoad(AHelpUIController obj)
    {
        ToggleWindow();
    }

    private void OnMentorStatus(MentorStatusMsg msg)
    {
        _isMentor = msg.IsMentor;

        if (_isMentor)
            _mentorHelpWindow?.Close();
        else
            _mentorWindow?.Close();
    }

    private void OnMentorHelpReceived(MentorMessagesReceivedMsg msg)
    {
        var other = false;
        foreach (var message in msg.Messages)
        {
            if (message.Author != _player.LocalUser)
                other = true;

            if (_isMentor &&
                _mentorWindow is not { IsOpen: true })
            {
                _unread = true;
                _aHelp.UnreadAHelpReceived();
            }

            _destinationNames.TryAdd(message.Destination, message.DestinationName);
            _messages.GetOrNew(message.Destination).Add(message);
            if (_mentorWindow is { IsOpen: true })
            {
                MentorAddPlayerButton(message.Destination);

                if (_mentorWindow.SelectedPlayer == message.Destination)
                {
                    _mentorWindow.Messages.AddMessage(CreateMessageLabel(message));
                    _mentorWindow.Messages.ScrollToBottom();
                }

                continue;
            }

            if (_mentorHelpWindow is { IsOpen: true } &&
                _player.LocalUser == message.Destination)
            {
                _mentorHelpWindow.Messages.AddMessage(CreateMessageLabel(message));
                _mentorHelpWindow.Messages.ScrollToBottom();
            }
        }

        if (other)
        {
            _audio?.PlayGlobal(_mHelpSound, Filter.Local(), false);
            _clyde.RequestWindowAttention();

            if (!_isMentor)
            {
                if (OpenWindow(ref _mentorHelpWindow, CreateMentorHelpWindow, () => _mentorHelpWindow = null))
                {
                    _mentorHelpWindow.OpenCentered();
                }
            }
        }
    }

    public void OnSystemLoaded(BwoinkSystem system)
    {
        CommandBinds.Builder
            .BindBefore(ContentKeyFunctions.OpenAHelp,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()),
                typeof(AHelpUIController))
            .Register<StaffHelpUIController>();
    }

    public void OnSystemUnloaded(BwoinkSystem system)
    {
        CommandBinds.Unregister<StaffHelpUIController>();
    }

    public void ToggleWindow()
    {
        if (_staffHelpWindow != null)
        {
            _staffHelpWindow.Close();
            _staffHelpWindow = null;
            SetAHelpButtonPressed(false);
            return;
        }

        SetAHelpButtonPressed(true);
        _staffHelpWindow = new StaffHelpWindow();
        _staffHelpWindow.OnClose += () => _staffHelpWindow = null;
        _staffHelpWindow.OpenCentered();
        UIManager.ClickSound();

        if (_unread)
            _staffHelpWindow.MentorHelpButton.StyleClasses.Add("ButtonColorRed");

        _staffHelpWindow.AdminHelpButton.OnPressed += _ =>
        {
            _aHelp.Open();
            _staffHelpWindow.Close();
            SetAHelpButtonPressed(false);
        };

        _staffHelpWindow.MentorHelpButton.OnPressed += _ =>
        {
            SetAHelpButtonPressed(false);
            _unread = false;
            if (_isMentor)
            {
                if (OpenWindow(ref _mentorWindow, CreateMentorWindow, () => _mentorWindow = null))
                {
                    foreach (var destination in _messages.Keys)
                    {
                        MentorAddPlayerButton(destination);
                    }

                    _mentorWindow.OpenCentered();
                }
            }
            else
            {
                if (OpenWindow(ref _mentorHelpWindow, CreateMentorHelpWindow, () => _mentorHelpWindow = null))
                {
                    _mentorHelpWindow.OpenCentered();
                }
            }

            _staffHelpWindow.Close();
        };
    }

    private MentorHelpWindow CreateMentorHelpWindow()
    {
        var window = new MentorHelpWindow();
        window.Chat.OnTextEntered += args =>
        {
            window.Chat.Clear();
            if (string.IsNullOrWhiteSpace(args.Text))
                return;

            var msg = new MentorHelpMsg() { Message = args.Text };
            _net.ClientSendMessage(msg);
        };

        if (_player.LocalUser is { } local && _messages.TryGetValue(local, out var messages))
        {
            foreach (var message in messages)
            {
                window.Messages.AddMessage(CreateMessageLabel(message));
                window.Messages.ScrollToBottom();
            }
        }

        return window;
    }

    private MentorWindow CreateMentorWindow()
    {
        var window = new MentorWindow();
        window.Chat.OnTextEntered += args =>
        {
            var msg = new MentorSendMessageMsg { Message = args.Text, To = window.SelectedPlayer };
            _net.ClientSendMessage(msg);
            window.Chat.Clear();
        };

        return window;
    }

    private void MentorAddPlayerButton(NetUserId player)
    {
        if (_mentorWindow == null)
            return;

        if (_mentorWindow.PlayerDict.TryGetValue(player, out var button))
        {
            button.SetPositionFirst();
            return;
        }

        var playerName = player.ToString();
        if (_destinationNames.TryGetValue(player, out var destinationName))
            playerName = destinationName;

        var playerButton = new Button
        {
            Text = playerName,
            StyleClasses = { "OpenBoth" },
        };
        playerButton.OnPressed += _ =>
        {
            if (_mentorWindow is not { IsOpen: true })
                return;

            _mentorWindow.SelectedPlayer = player;
            _mentorWindow.Messages.Clear();
            _mentorWindow.Chat.Editable = true;
            if (!_messages.TryGetValue(player, out var authorMessages))
                return;

            foreach (var message in authorMessages)
            {
                _mentorWindow.Messages.AddMessage(CreateMessageLabel(message));
                _mentorWindow.Messages.ScrollToBottom();
            }
        };

        _mentorWindow.Players.AddChild(playerButton);
        playerButton.SetPositionFirst();
        _mentorWindow.PlayerDict[player] = playerButton;
    }

    private bool OpenWindow<T>([NotNullWhen(true)] ref T? window, Func<T> create, Action onClose) where T : DefaultWindow
    {
        if (window != null)
            return true;

        window = create();
        window.OnClose += onClose;
        return true;
    }

    private FormattedMessage CreateMessageLabel(MentorMessage message)
    {
        var author = message.AuthorName;
        if (message.IsMentor)
            author = $"[bold][color=red]{author}[/color][/bold]";

        var text = $"{message.Time:HH:mm} {author}: {FormattedMessage.EscapeText(message.Text)}";
        return FormattedMessage.FromMarkupPermissive(text);
    }

    private void SetAHelpButtonPressed(bool pressed)
    {
        if (_aHelp.GameAHelpButton != null)
            _aHelp.GameAHelpButton.Pressed = pressed;

        if (_aHelp.GameAHelpButton != null)
            _aHelp.GameAHelpButton.Pressed = pressed;
    }
}
