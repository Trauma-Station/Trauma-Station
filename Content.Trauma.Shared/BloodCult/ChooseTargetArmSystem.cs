// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
//using Content.Shared.Random.Helpers;
//using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.BloodCult;

public sealed partial class ChooseTargetArmSystem : EntitySystem
{
    //[Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    public TargetBodyPart ChooseArm()
    {
        /// how am i supposed to use this when it takes NetEntity and not enum?
        /// do you want me to get ent's hands the hard way or what?
        // return SharedRandomExtensions.PredictedProb(_timing, .5f, TargetBodyPart.LeftArm, TargetBodyPart.RightArm);

        var targetArm = TargetBodyPart.LeftArm;
        if(_random.Next(0, 2) == 1)
            targetArm = TargetBodyPart.RightArm;
        return targetArm;
    }
}
