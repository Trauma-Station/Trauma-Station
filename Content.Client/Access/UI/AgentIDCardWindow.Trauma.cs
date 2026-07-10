using Content.Trauma.Common.NanoChat;
using Robust.Client.UserInterface.Controls;
using System.Linq;
using System.Numerics;

namespace Content.Client.Access.UI;

public sealed partial class AgentIDCardWindow
{
    [Dependency] private IEntityManager _ent = default!;

    private const int MaxNumberLength = 4;
    private EntityUid _owner;
    private uint? _number;

    public event Action<uint>? OnSetNumber;

    private void InitializeTrauma()
    {
        NumberLineEdit.OnTextEntered += OnNumberEntered;
        NumberLineEdit.OnFocusExit += OnNumberEntered;

        NumberLineEdit.OnTextChanged += args =>
        {
            if (args.Text.Length > MaxNumberLength)
            {
                NumberLineEdit.Text = args.Text[..MaxNumberLength];
            }

            // Filter to digits only
            var newText = string.Concat(args.Text.Where(char.IsDigit));
            if (newText != args.Text)
                NumberLineEdit.Text = newText;
        };
    }

    public void SetOwner(EntityUid owner)
    {
        _owner = owner;
        UpdateNumber();
    }

    private void OnNumberEntered(LineEdit.LineEditEventArgs args)
    {
        if (uint.TryParse(args.Text, out var number) && number > 0)
            OnSetNumber?.Invoke(number);
    }

    private void UpdateNumber()
    {
        if (!_ent.TryGetComponent<NanoChatCardComponent>(_owner, out var comp) || comp.Number is not { } number)
            return;

        if (_number == number)
            return;

        _number = number;
        NumberLineEdit.Text = number.ToString("D4");
    }
}
