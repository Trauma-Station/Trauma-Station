// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Shared.Construction.Prototypes;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction.UI;

internal sealed partial class ConstructionMenuPresenter
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private CommonKnowledgeSystem _knowledge = default!;

    private bool _autoFocusSearch;
    private bool _useSkills;
    private Dictionary<EntProtoId, int> _skills = new();

    private void InitializeTrauma()
    {
        _knowledge = _entManager.System<CommonKnowledgeSystem>();

        _cfg.OnValueChanged(GoobCVars.AutoFocusSearchOnBuildMenu, x => _autoFocusSearch = x, true);
    }

    bool CanUnderstand(ConstructionPrototype recipe)
    {
        if (!_useSkills)
            return true; // for mobs that dont use the knowledge system, let them build anything

        foreach (var (id, needed) in recipe.Theory)
        {
            if (!_skills.TryGetValue(id, out var mastery) || mastery < needed)
                return false;
        }

        return true;
    }
}
