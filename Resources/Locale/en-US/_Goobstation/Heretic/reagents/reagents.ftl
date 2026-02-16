# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

reagent-name-eldritch = eldritch essence
reagent-desc-eldritch = A strange liquid that defies the laws of physics. It re-energizes and heals those who can see beyond this fragile reality, but is incredibly harmful to the closed-minded.

reagent-effect-guidebook-deconvert-ghoul = deconverts ghoulified entity

entity-condition-guidebook-heretic-or-ghoul = target is a heretic or ghoul
entity-condition-guidebook-not-heretic-or-ghoul = target is not a heretic or ghoul

entity-condition-guidebook-environment-temperature = environment temperature is
    { $invert ->
        [true] at least
        *[false] at most
    } {$threshold} degrees

entity-condition-guidebook-has-body-part = target
    { $invert ->
        [true] has no
        *[false] has
    } {$part}

entity-condition-guidebook-on-fire = target is
    { $invert ->
        [true] not on fire
        *[false] on fire
    }

reagent-physical-desc-eldritch = eldritch

flavor-complex-eldritch = Ag'hsj'saje'sh
