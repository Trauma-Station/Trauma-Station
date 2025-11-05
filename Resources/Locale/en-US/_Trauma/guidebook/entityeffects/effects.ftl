entity-effect-guidebook-delete-entity = {$chance ->
    [1] deletes
    *[other] delete
} the target
entity-effect-guidebook-relay-implanted = for the implant's user, {$effect}
entity-effect-guidebook-add-killsign = gives the target a KILL sign
entity-effect-guidebook-add-accents = gives the target some funny accents
entity-effect-guidebook-force-equip-clothing = force {$chance ->
    [1] equips
    *[other] equip
} {A($name)} to the target's {$slot}

entity-effect-guidebook-speak = Causes involuntary speech

entity-effect-guidebook-increases-reach = Increases reach
entity-effect-guidebook-decreases-reach = Decreases reach

entity-effect-guidebook-scale-entity = Scales the target's size by ({$x}, {y})

entity-effect-guidebook-attack-self = {$chance ->
    [1] makes
    *[other] make
} the target {$canUse ->
    [true] attack
    *[false] punch
} itself
entity-effect-guidebook-attack-others = {$chance ->
    [1] makes
    *[other] make
} the target attack a random nearby thing

entity-effect-guidebook-start-use-delay = {$chance ->
    [1] starts
    *[other] start
} the {$id} use delay on the target

entity-effect-guidebook-part-add-slot = {$chance ->
    [1] adds
    *[other] add
} a {$slot} slot to the target part

entity-effect-guidebook-part-remove-slot = {$chance ->
    [1] removes
    *[other] remove
} a {$slot} slot from the target part
