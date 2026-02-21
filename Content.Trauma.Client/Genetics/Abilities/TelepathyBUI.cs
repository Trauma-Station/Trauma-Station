// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Administration;
using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.Genetics.Abilities;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Genetics.Abilities;

public sealed class TelepathyBUI : BoundUserInterface
{
    private DialogWindow? _window;

    public TelepathyBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var maxLength = EntMan.GetComponentOrNull<TelepathyActionComponent>(Owner)?.MaxLength ?? 30;

        var field = "msg";
        var prompt = Loc.GetString("MutationTelepathy-window-message");
        var placeholder = Loc.GetString("MutationTelepathy-window-placeholder");
        var entries = new List<QuickDialogEntry>();
        entries.Add(new(field, QuickDialogEntryType.ShortText, prompt, placeholder));
        var title = Loc.GetString("MutationTelepathy-window-title");

        _window = new(title, entries);

        _window.OnClose += Close;
        _window.OnConfirmed += responses =>
        {
            var msg = responses[field].Trim();
            if (msg.Length < 1)
                return;

            msg = msg.Substring(0, Math.Min(maxLength, msg.Length));

            SendPredictedMessage(new TelepathyChosenMessage(msg));
            _window.Close();
        };
        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Orphan();
    }
}
