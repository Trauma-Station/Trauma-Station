genome-extractor-examine = {$empty ->
    [true] It is empty and ready to use.
    *[other] It has a genome stored inside.
}

genome-extractor-fail-full = {CAPITALIZE(THE($item))} {$item} is already filled with genetic material!
genome-extractor-fail-dead = {CAPITALIZE($target)} is dead!
genome-extractor-fail-genetic = {CAPITALIZE(POSS-ADJ($target))} genes are damaged!
