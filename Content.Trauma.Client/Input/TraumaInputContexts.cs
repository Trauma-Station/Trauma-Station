// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Input;
using Robust.Shared.Input;

namespace Content.Trauma.Client.Input;

public static class TraumaInputContexts
{
    public static void SetupContexts(IInputContextContainer contexts)
    {
        var common = contexts.GetContext("common");
        common.AddFunction(TraumaKeyFunctions.NetBandwidth);

        var human = contexts.GetContext("human");
        human.AddFunction(TraumaKeyFunctions.ResistGrab);
        human.AddFunction(TraumaKeyFunctions.OpenLanguageMenu);
        human.AddFunction(TraumaKeyFunctions.OpenMartialArtsMenu);
        human.AddFunction(TraumaKeyFunctions.Tackle);
        human.AddFunction(TraumaKeyFunctions.Strafe);
        // TODO: change to cycling x/y
        human.AddFunction(TraumaKeyFunctions.TargetHead);
        human.AddFunction(TraumaKeyFunctions.TargetChest);
        human.AddFunction(TraumaKeyFunctions.TargetGroin);
        human.AddFunction(TraumaKeyFunctions.TargetLeftArm);
        human.AddFunction(TraumaKeyFunctions.TargetLeftHand);
        human.AddFunction(TraumaKeyFunctions.TargetRightArm);
        human.AddFunction(TraumaKeyFunctions.TargetRightHand);
        human.AddFunction(TraumaKeyFunctions.TargetLeftLeg);
        human.AddFunction(TraumaKeyFunctions.TargetLeftFoot);
        human.AddFunction(TraumaKeyFunctions.TargetRightLeg);
        human.AddFunction(TraumaKeyFunctions.TargetRightFoot);
    }
}
