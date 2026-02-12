using Content.Shared.Atmos;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Spawns gas in the target entity's contained air mixture and makes it emote.
/// By default any gas can spawn.
/// </summary>
public sealed partial class ExpelGas : EntityEffectBase<ExpelGas>
{
    [DataField]
    public List<Gas> PossibleGases = new()
    {
        Gas.Oxygen,
        Gas.Nitrogen,
        Gas.CarbonDioxide,
        Gas.Plasma,
        Gas.Tritium,
        Gas.WaterVapor,
        Gas.Ammonia,
        Gas.NitrousOxide,
        Gas.Frezon,
        Gas.BZ,
        Gas.Healium,
        Gas.Nitrium,
        Gas.Pluoxium,
    };

    [DataField]
    public float Moles = 60f;
}
