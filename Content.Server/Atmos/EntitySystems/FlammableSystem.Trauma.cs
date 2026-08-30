using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Common.Flammability;
using Content.Shared.Body;
using Content.Trauma.Common.Wizard;
using Robust.Shared.Configuration;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class FlammableSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private CommonSpellbladeSystem _spellblade = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private EntityQuery<FireImmunityComponent> _fireImmuneQuery = default!;

    private int _addHeatFirestack = 1500;

    private void InitTrauma()
    {
        Subs.CVar(_cfg, GoobCVars.FireStackHeat, value => _addHeatFirestack = value, true);
    }
}
