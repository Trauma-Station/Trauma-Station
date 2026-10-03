
using Content.Server.Cargo.Components;
using Content.Shared.Cargo;

using Robust.Shared.Log;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    /*
     * Этот partial оставлен для дополнительной логики
     * репутации, если она понадобится в будущем.
     *
     * ВАЖНО:
     *
     * Репутация больше НЕ начисляется за сам факт продажи
     * оружия, аптечки, еды и т.д.
     *
     * Репутация начисляется только после выполнения
     * конкретного CargoBounty.
     *
     * Логика находится в CargoSystem.Bounties.cs:
     *
     * OnSold()
     *     ↓
     * ProcessReputationForBounty()
     *     ↓
     * GetReputationType()
     */

    public void InitializeReputationEvents()
    {
        Logger.InfoS(
            "CargoReputation",
            "Faction cargo reputation system initialized.");
    }
}
