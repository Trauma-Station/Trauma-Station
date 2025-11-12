entity-effect-guidebook-delete-entity = {$chance ->
    [1] deletes
    *[other] delete
} the target
entity-effect-guidebook-force-equip-clothing = force {$chance ->
    [1] equips
    *[other] equip
} {A($name)} to the target's {$slot}

entity-effect-guidebook-part-add-slot = {$chance ->
    [1] adds
    *[other] add
} a {$slot} slot to the target part

entity-effect-guidebook-insert-new-organ = {$chance ->
    [1] inserts
    *[other] insert
} a {$organ} into the target part

entity-effect-guidebook-speak = Causes involuntary speech

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

entity-effect-guidebook-part-remove-slot = {$chance ->
    [1] removes
    *[other] remove
} a {$slot} slot from the target part

entity-effect-guidebook-remove-part = {$chance ->
    [1] detaches
    *[other] detach
} the body part from the body

entity-effect-guidebook-set-standing = {$chance ->
    [1] makes
    *[other] make
} the target {$standing ->
    [true] stand up
    *[other] get knocked down
}

entity-effect-guidebook-relay-random-part = for a random part, {$effect}
