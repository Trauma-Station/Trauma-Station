// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Trauma.Common.Language.Systems;
using Content.Trauma.Shared.Language.Components.Translators;
using Content.Shared.Examine;
using Content.Shared.Toggleable; // Ignore, touching for REUSE Headers.

namespace Content.Trauma.Shared.Language.Systems;

public abstract partial class SharedTranslatorSystem : CommonTranslatorSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldTranslatorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, HandheldTranslatorComponent component, ExaminedEvent args)
    {
        if (!component.ShowInfoOnExamine) // goob - don't show info if the component tag is false
            return;

        var understoodLanguageNames = component.UnderstoodLanguages
            .Select(it => Loc.GetString($"language-{it}-name"));
        var spokenLanguageNames = component.SpokenLanguages
            .Select(it => Loc.GetString($"language-{it}-name"));
        var requiredLanguageNames = component.RequiredLanguages
            .Select(it => Loc.GetString($"language-{it}-name"));

        args.PushMarkup(Loc.GetString("translator-examined-langs-understood", ("languages", string.Join(", ", understoodLanguageNames))));
        args.PushMarkup(Loc.GetString("translator-examined-langs-spoken", ("languages", string.Join(", ", spokenLanguageNames))));

        args.PushMarkup(Loc.GetString(component.RequiresAllLanguages ? "translator-examined-requires-all" : "translator-examined-requires-any",
            ("languages", string.Join(", ", requiredLanguageNames))));

        args.PushMarkup(Loc.GetString(component.Enabled ? "translator-examined-enabled" : "translator-examined-disabled"));
    }

    protected void OnAppearanceChange(EntityUid translator, HandheldTranslatorComponent? comp = null)
    {
        if (comp == null && !TryComp(translator, out comp))
            return;

        _appearance.SetData(translator, ToggleableVisuals.Enabled, comp.Enabled);
    }
}
