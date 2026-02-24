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

entity-effect-guidebook-add-to-chemicals = { $chance ->
    [1] { $deltasign ->
            [1] Adds
            *[-1] Removes
        }
    *[other]
        { $deltasign ->
            [1] add
            *[-1] remove
        }
} {NATURALFIXED($amount, 2)}u of {$reagent} { $deltasign ->
    [1] to
    *[-1] from
} the solution

entity-effect-guidebook-make-traitor = { $chance ->
    [1] makes
    *[other] make
} the target a traitor

entity-effect-guidebook-infect-disease = { $chance ->
    [1] infects
    *[other] infect
} the target with {$disease}

entity-effect-guidebook-add-marking = { $chance ->
    [1] adds
    *[other] add
} {$marking} to the target
entity-effect-guidebook-remove-marking = { $chance ->
    [1] removes
    *[other] remove
} {$marking} to the target

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

entity-effect-guidebook-nothing = nothing ever {$chance ->
    [1] happens
    *[other] happen
}

entity-effect-guidebook-scramble-dna = {$chance ->
    [1] scrambles
    *[other] scramble
} the target's mutations

entity-effect-guidebook-move-organ = {$chance ->
    [1] moves
    *[other] move
} the target's {$organ} to its {$dest}
