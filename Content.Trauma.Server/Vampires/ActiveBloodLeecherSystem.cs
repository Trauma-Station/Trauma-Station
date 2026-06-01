// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Beam;
using Content.Trauma.Shared.Vampires.Haemomancer;

namespace Content.Trauma.Server.Vampires;

public sealed partial class ActiveBloodLeecherSystem : SharedActiveBloodLeecherSystem
{
    [Dependency] private BeamSystem _beam = default!;

    protected override void CreateBeam(EntityUid user, EntityUid target, EntProtoId beamProto)
    {
        base.CreateBeam(user, target, beamProto);

        _beam.TryCreateBeam(user, target, beamProto);
    }
}
