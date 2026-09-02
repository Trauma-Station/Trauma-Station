// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Familiar;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Trauma.Client.Familiar;

/// <summary>
/// Shows familiars their master in the character menu.
/// </summary>
public sealed partial class FamiliarInfoSystem : EntitySystem
{
    [Dependency] private FamiliarSystem _familiar = default!;

    [SubscribeLocalEvent]
    private void OnGetCharacterInfoControls(ref GetCharacterInfoControlsEvent args)
    {
        if (_familiar.GetMasterName(args.Entity) is not { } master)
            return;

        master = FormattedMessage.EscapeText(master);
        var label = new RichTextLabel()
        {
            Text = $"[bold]{master}[/bold] is your master, serve them faithfully!",
            Margin = new Thickness(8, 4)
        };
        args.Controls.Add(label);
    }
}
