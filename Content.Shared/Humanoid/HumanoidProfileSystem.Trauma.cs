using Content.Goobstation.Common.Barks;
using Content.Shared.Body;
using Content.Shared.DetailExaminable;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// Trauma - barks stuff and "api" for humanoid
/// </summary>
public sealed partial class HumanoidProfileSystem
{
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    public static readonly ProtoId<BarkPrototype> DefaultBarkVoice = "Alto";

    public static readonly ProtoId<OrganCategoryPrototype> EyesCategory = "Eyes";
    public static readonly ProtoId<OrganCategoryPrototype> TorsoCategory = "Torso";

    public void SetBarkVoice(Entity<HumanoidProfileComponent> ent, [ForbidLiteral] ProtoId<BarkPrototype>? barkvoiceId)
    {
        var voicePrototypeId = DefaultBarkVoice;
        var species = ent.Comp.Species;
        if (barkvoiceId != null &&
            _prototype.TryIndex(barkvoiceId, out var bark) &&
            bark.SpeciesWhitelist?.Contains(species) != false)
        {
            voicePrototypeId = barkvoiceId.Value;
        }
        else
        {
            // use first valid bark as a fallback
            foreach (var o in _prototype.EnumeratePrototypes<BarkPrototype>())
            {
                if (o.RoundStart && o.SpeciesWhitelist?.Contains(species) != false)
                {
                    voicePrototypeId = o.ID;
                    break;
                }
            }
        }

        var comp = EnsureComp<SpeechSynthesisComponent>(ent);
        comp.VoicePrototypeId = voicePrototypeId;
        Dirty(ent, comp);
        ent.Comp.BarkVoice = voicePrototypeId;
        Dirty(ent);
    }

    // god i love shitcode having 0 apis so i must write even more shitcode
    public HumanoidCharacterProfile? CreateProfile(Entity<HumanoidProfileComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp) ||
            !_visualBody.TryGatherMarkingsData(ent.Owner, null, out var organs, out _, out var markings))
            return null;

        var flavortext = CompOrNull<DetailExaminableComponent>(ent)?.Content ?? string.Empty;
        // THANK YOU FOR NOT STORING THESE ANYWHERE!!!!!
        var eyeColor = organs.TryGetValue(EyesCategory, out var eye) ? eye.EyeColor : Color.Black;
        var skinColor = organs.TryGetValue(TorsoCategory, out var torso) ? torso.SkinColor : Color.White;
        var appearance = new HumanoidCharacterAppearance(eyeColor, skinColor, markings);
        return new HumanoidCharacterProfile(
            Name(ent),
            flavortext,
            ent.Comp.Species,
            ent.Comp.Age,
            ent.Comp.Sex,
            ent.Comp.Gender,
            appearance,
            // not spawning player don't care about anything here
            default!,
            new(),
            default!,
            new(),
            new(),
            new(),
            ent.Comp.BarkVoice);
    }
}
