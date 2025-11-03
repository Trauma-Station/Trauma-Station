using Content.Shared._EinsteinEngines.Language;
using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._EinsteinEngines.Language;

public sealed class TowerOfBabelSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TowerOfBabelComponent, MapInitEvent>(OnInit, before: [typeof(LanguageSystem)]);
    }

    private void OnInit(Entity<TowerOfBabelComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out LanguageKnowledgeComponent? knowledge) ||
            !TryComp(ent, out LanguageSpeakerComponent? speaker))
            return;

        var spoken = knowledge.SpokenLanguages;
        spoken.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<LanguagePrototype>())
        {
            spoken.Add(proto.ID);
        }
        var understood = knowledge.UnderstoodLanguages;
        understood.Clear();
        understood.AddRange(spoken);
        Dirty(ent.Owner, knowledge);
        _language.EnsureValidLanguage((ent.Owner, speaker));
    }
}
