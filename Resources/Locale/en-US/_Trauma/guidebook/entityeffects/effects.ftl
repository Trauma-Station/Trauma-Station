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
