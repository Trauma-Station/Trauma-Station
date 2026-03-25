// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.SecTrack;

[Serializable, NetSerializable]
public enum SquadIconNum : byte
{
    Alpha,
    Beta,
    Gamma,
    Delta,
    Epsilon,
    Zeta,
    Heta,
    Theta,
    Iota,
    Kappa,
    Lambda,
    Mu,
    Nu,
    Xi,
    Omicron,
    Pi,
    Ro,
    Sigma,
    Tau,
    Upsilon,
    Fi,
    Hi,
    Psi,
    Omega
}

[Serializable, NetSerializable]
public enum SquadStatus : byte
{
    Active,
    OnBreak
}
