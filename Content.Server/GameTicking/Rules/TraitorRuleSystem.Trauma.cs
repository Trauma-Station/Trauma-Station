using System.Text;
using Content.Goobstation.Common.Traitor;
using Content.Server.PDA.Ringer;
using Content.Shared.FixedPoint;
using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;

namespace Content.Server.GameTicking.Rules;

public sealed partial class TraitorRuleSystem
{
    [Dependency] private readonly GoobCommonUplinkSystem _goobUplink = default!;

    // RequestUplink method but with preference and pen spin code output
    // This doesn't work properly, so it's currently unused
    private (Note[]?, int[]?, string) RequestUplink(EntityUid traitor,
        EntityUid mindId,
        FixedPoint2 startingBalance,
        string briefing)
    {
        Note[]? code = null;
        int[]? spinCode = null;

        var uplinkPreference = _goobUplink.GetUplinkPreference(mindId);

        var uplinkTarget = uplinkPreference switch
        {
            UplinkPreference.Pda => _uplink.FindUplinkTarget(traitor),
            UplinkPreference.Pen => _goobUplink.FindPenUplinkTarget(traitor),
            _ => null // Implant doesn't need a target entity
        };

        if (!_uplink.AddUplink(traitor, startingBalance, uplinkTarget, giveDiscounts: true, uplinkPreference))
            return (null, null, briefing);

        if (uplinkTarget != null)
        {
            switch (uplinkPreference)
            {
                case UplinkPreference.Pda:
                    EnsureComp<RingerUplinkComponent>(uplinkTarget.Value);
                    var ringerEv = new GenerateUplinkCodeEvent();
                    RaiseLocalEvent(uplinkTarget.Value, ref ringerEv);
                    code = Comp<RingerUplinkComponent>(uplinkTarget.Value).Code;
                    break;
                case UplinkPreference.Pen:
                    var spinEv = new GeneratePenSpinCodeEvent();
                    RaiseLocalEvent(uplinkTarget.Value, ref spinEv);
                    spinCode = spinEv.Code;
                    break;
            }
        }

        return (code, spinCode, briefing);
    }

    private string GenerateBriefingCharacter(string[]? codewords,
        Note[]? uplinkCode,
        int[]? penSpinCode,
        string objectiveIssuer)
    {
        var sb = new StringBuilder();
        sb.AppendLine("\n" + Loc.GetString($"traitor-{objectiveIssuer.Replace(" ", "").ToLower()}-intro"));

        if (uplinkCode != null)
            sb.AppendLine(Loc.GetString($"traitor-role-uplink-code-short",
                ("code", string.Join("-", uplinkCode).Replace("sharp", "#"))));
        else if (penSpinCode != null)
            sb.AppendLine(Loc.GetString($"traitor-role-uplink-pen-code-short",
                ("code", string.Join("-", penSpinCode))));
        else
            sb.AppendLine("\n" + Loc.GetString($"traitor-role-nouplink"));

        if (codewords != null)
            sb.AppendLine(Loc.GetString($"traitor-role-codewords-short", ("codewords", string.Join(", ", codewords))));

        sb.AppendLine("\n" + Loc.GetString($"traitor-role-allegiances"));
        sb.AppendLine(Loc.GetString($"traitor-{objectiveIssuer.Replace(" ", "").ToLower()}-allies"));

        sb.AppendLine("\n" + Loc.GetString($"traitor-role-notes"));
        sb.AppendLine(Loc.GetString($"traitor-{objectiveIssuer.Replace(" ", "").ToLower()}-goal"));

        return sb.ToString();
    }
}
