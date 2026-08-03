// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Configuration;

namespace Content.Shared.Construction;

/// <summary>
/// Trauma - virtual methods for calling from shared code
/// </summary>
public abstract partial class SharedConstructionSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private EntityQuery<KnowledgeHolderComponent> _knowledgeQuery = default!;

    private bool _skillsEnabled;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, TraumaCVars.SkillsEnabled, x => _skillsEnabled = x, true);
    }

    public virtual bool ChangeNode(EntityUid uid, EntityUid? userUid, string id, bool performActions = true)
        => false;

    /// <summary>
    /// Whether knowledge should be used for a given user.
    /// </summary>
    public bool UsesKnowledge(EntityUid user)
        => _skillsEnabled && _knowledgeQuery.HasComp(user);
}
