using Content.Goobstation.Common.Barks;
using Content.Shared.Body;
using Content.Shared.DetailExaminable;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// Trauma - barks stuff and "api" for humanoid
/// </summary>
public sealed partial class HumanoidProfileSystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    public static readonly ProtoId<BarkPrototype> DefaultBarkVoice = "Alto";

    public static readonly ProtoId<OrganCategoryPrototype> EyesCategory = "Eyes";
    public static readonly ProtoId<OrganCategoryPrototype> TorsoCategory = "Torso";

    /// <summary>
    /// Organ visual layers needed for eye and skin color.
    /// </summary>
    public static readonly HashSet<HumanoidVisualLayers> CoreLayers = new()
    {
        HumanoidVisualLayers.Eyes,
        HumanoidVisualLayers.Chest
    };

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
        var eyeColor = GetEyeColor(organs) ?? Color.Black;
        var skinColor = GetSkinColor(organs) ?? Color.White; // curse of yakub
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

    /// <summary>
    /// Gets the eye color from a set of organ visual data, or null if it has no eyes.
    /// </summary>
    public Color? GetEyeColor(Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>? organs)
        => organs?.TryGetValue(EyesCategory, out var eye) == true ? eye.EyeColor : null;

    /// <summary>
    /// Gets the skin color from a set of organ visual data, or null if it has no torso. (Should never happen)
    /// </summary>
    public Color? GetSkinColor(Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>? organs)
        => organs?.TryGetValue(TorsoCategory, out var torso) == true ? torso.SkinColor : null;

    /// <summary>
    /// Get the visual data you need for <see cref="GetEyeColor"/> and <see cref="GetSkinColor"/>.
    /// </summary>
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>? GetOrgansData(EntityUid mob)
    {
        _visualBody.TryGatherMarkingsData(mob, CoreLayers, out var organs, out _, out _);
        return organs;
    }

    public void SetEyeColor(EntityUid mob, Color color)
    {
        if (!TryComp<BodyComponent>(mob, out var body) ||
            _body.GetOrgan(mob, EyesCategory) is not {} eyes ||
            !TryComp<VisualOrganComponent>(eyes, out var visual) ||
            visual.Profile.EyeColor == color)
            return;

        // raise the event chuddery so it applies organcolor etc automatically
        var profile = visual.Profile;
        profile.EyeColor = color;
        var ev = new BodyRelayedEvent<ApplyOrganProfileDataEvent>((mob, body), new ApplyOrganProfileDataEvent(profile, null));
        RaiseLocalEvent(eyes, ref ev);
    }

    public void SetSkinColor(EntityUid mob, Color color, Color? eyeColor = null)
    {
        if (!TryComp<HumanoidProfileComponent>(mob, out var comp))
            return;

        _visualBody.ApplyProfile(mob, new()
        {
            Sex = comp.Sex,
            SkinColor = color,
            EyeColor = eyeColor ?? GetEyeColor(GetOrgansData(mob)) ?? Color.Black
        });
        // TODO: fix upstream thing for marking skin-matched colors
    }

    // REMOVED THE ENTIRE API AWARD!!!
    public void SetSex(Entity<HumanoidProfileComponent> ent, Sex sex)
    {
        var old = ent.Comp.Sex;
        if (old == sex)
            return;

        ent.Comp.Sex = sex;
        Dirty(ent);
        var ev = new SexChangedEvent(old, sex);
        RaiseLocalEvent(ent, ref ev);
    }

    public void SetGender(Entity<HumanoidProfileComponent> ent, Gender gender)
    {
        if (ent.Comp.Gender == gender)
            return;

        ent.Comp.Gender = gender;
        Dirty(ent);

        if (TryComp<GrammarComponent>(ent, out var grammar))
            _grammar.SetGender((ent, grammar), gender);
    }
}
