// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech;
using Content.Shared.Speech.Prototypes;
using Content.Shared.Speech.EntitySystems;
using System.Text.RegularExpressions;

namespace Content.Trauma.Shared.Speech;

public sealed partial class StreetpunkAccentSystem : EntitySystem
{
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private static readonly ProtoId<ReplacementAccentPrototype> Accent = "streetpunk";

    private static readonly Regex RegexIng = new(@"ing\b");
    private static readonly Regex RegexAnd = new(@"\band\b");
    private static readonly Regex RegexDve = new("d've");

    [SubscribeLocalEvent]
    private void OnAccentGet(Entity<StreetpunkAccentComponent> ent, ref AccentGetEvent args)
    {
        var msg = args.Message;

        //They shoulda started runnin' an' hidin' from me! <- bit from SouthernDrawl Accent
        msg = RegexIng.Replace(msg, "in'");
        msg = RegexAnd.Replace(msg, "an'");
        msg = RegexDve.Replace(msg, "da");

        msg = _replacement.ApplyReplacements(msg, "streetpunk");

        args.Message = msg;
    }
}
