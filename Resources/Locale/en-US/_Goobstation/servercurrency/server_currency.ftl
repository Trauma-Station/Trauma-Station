# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
# SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

# Trauma - changed all Goob Coin to Evil Coin
server-currency-name-singular = Evil Coin
server-currency-name-plural = Evil Coins

## Commands

server-currency-gift-command = gift
server-currency-gift-command-description = Gifts some of your balance to another player.
server-currency-gift-command-help = Usage: gift <player> <value>
server-currency-gift-command-error-1 = You can't gift yourself!
server-currency-gift-command-error-2 = You can not afford to gift this! You have a balance of {$balance}.
server-currency-gift-command-giver = You gave {$player} {$amount}.
server-currency-gift-command-reciever = {$player} gave you {$amount}.

server-currency-balance-command = balance
server-currency-balance-command-description = Returns your balance.
server-currency-balance-command-help = Usage: balance
server-currency-balance-command-return = You have {$balance}.

server-currency-add-command = balance:add
server-currency-add-command-description = Adds currency to a player's balance.
server-currency-add-command-help = Usage: balance:add <player> <value>

server-currency-remove-command = balance:rem
server-currency-remove-command-description = Removes currency from a player's balance.
server-currency-remove-command-help = Usage: balance:rem <player> <value>

server-currency-set-command = balance:set
server-currency-set-command-description = Sets a player's balance.
server-currency-set-command-help = Usage: balance:set <player> <value>

server-currency-get-command = balance:get
server-currency-get-command-description = Gets the balance of a player.
server-currency-get-command-help = Usage: balance:get <player>

server-currency-command-completion-1 = Username
server-currency-command-completion-2 = Value
server-currency-command-error-1 = Unable to find a player by that name.
server-currency-command-error-2 = Value must be an integer.
server-currency-command-return = {$player} has {$balance}.

# 65% Update

gs-balanceui-title = Store
gs-balanceui-confirm = Confirm

gs-balanceui-gift-label = Transfer:
gs-balanceui-gift-player = Player
gs-balanceui-gift-player-tooltip = Insert the name of the player you want to send the money to
gs-balanceui-gift-value = Value
gs-balanceui-gift-value-tooltip = Amount of money to transfer

gs-balanceui-shop-label = Tokens Store
gs-balanceui-shop-empty = Out of stock!
gs-balanceui-shop-buy = Buy
gs-balanceui-shop-footer = ⚠ Ahelp to use your token. Only 1 use per day.

gs-balanceui-shop-token-label = Tokens
gs-balanceui-shop-tittle-label = Titles

gs-balanceui-shop-buy-minor-roundstart-token-antag = Buy a Minor Roundstart Antag Token - {$price} Evil Coins
gs-balanceui-shop-buy-major-roundstart-token-antag = Buy a Major Roundstart Antag Token - {$price} Evil Coins
gs-balanceui-shop-buy-minor-midround-token-antag = Buy a Minor Midround Antag Token - {$price} Evil Coins
gs-balanceui-shop-buy-major-midround-token-antag = Buy a Major Midround Antag Token - {$price} Evil Coins
gs-balanceui-shop-buy-wizard-token-antag = Buy a Wizard Antag Token - {$price} Evil Coins
gs-balanceui-shop-buy-token-admin-abuse = Buy an Admin Abuse Token - {$price} Evil Coins
gs-balanceui-shop-buy-token-hat = Buy a Hat Token - {$price} Evil Coins

gs-balanceui-shop-minor-roundstart-token-antag = Minor Roundstart Antag Token
gs-balanceui-shop-major-roundstart-token-antag = Major Roundstart Antag Token
gs-balanceui-shop-minor-midround-token-antag = Minor Midround Antag Token
gs-balanceui-shop-major-midround-token-antag = Major Midround Antag Token
gs-balanceui-shop-wizard-token-antag = Wizard Antag Token
gs-balanceui-shop-token-admin-abuse = Admin Abuse Token
gs-balanceui-shop-token-hat = Hat Token

# TODO: Add Spy and Werewolf once they're done
gs-balanceui-shop-buy-minor-roundstart-token-antag-desc = Allows you to choose from these antags: Insurgents, Vampire, Thief, Traitor, Devil.
gs-balanceui-shop-buy-major-roundstart-token-antag-desc = Allows you to choose from these antags: Wraith, Xenomorphs, Heretic, Changeling, Shadowling, HeadRev, Initial Infected, Cosmic Cult, Xenoborgs, Blob.
gs-balanceui-shop-buy-minor-midround-token-antag-desc = Allows you to choose from these antags: Midround Wraith, Lone Xenomorph, Lone Abductor, Paradox Clone, Rat King, Tunnel Clown, Mime Assassin, Dark Priest, Greytide.
gs-balanceui-shop-buy-major-midround-token-antag-desc = Allows you to choose from these antags: Space Dragon, Bingle, Ninja, Entropic Colossus, Slaughter/Shadow Demon, Morph, Blob, LoneOp, Singuloth Knights, Vox Raiders, Dark Lord.
gs-balanceui-shop-buy-wizard-token-antag-desc = Allows you to become a wizard.
gs-balanceui-shop-buy-token-admin-abuse-desc = Allows you to request an admin to abuse their powers against you. Admins are encouraged to go wild.
gs-balanceui-shop-buy-token-hat-desc = An admin will give you a random hat.

gs-balanceui-admin-add-label = Add (or subtract) money:
gs-balanceui-admin-add-player = Player name
gs-balanceui-admin-add-value = Value

gs-balanceui-remark-minor-roundstart-token-antag = Bought a minor roundstart antag token. Can be exchanged for these antags: Insurgents, Vampire, Thief, Traitor, Devil.
gs-balanceui-remark-major-roundstart-token-antag = Bought a major roundstart antag token. Can be exchanged for these antags: Wraith, Xenomorphs, Heretic, Changeling, Shadowling, HeadRev, Initial Infected, Cosmic Cult, Xenoborgs, Blob.
gs-balanceui-remark-minor-midround-token-antag = Bought a minor midround antag token. Can be exchanged for these antags: Midround Wraith, Lone Xenomorph, Lone Abductor, Paradox Clone, Rat King, Tunnel Clown, Mime Assassin, Dark Priest, Greytide.
gs-balanceui-remark-major-midround-token-antag = Bought a major midround antag token. Can be exchanged for these antags: Space Dragon, Bingle, Ninja, Entropic Colossus, Slaughter/Shadow Demon, Morph, Blob, LoneOp, Singuloth Knights, Vox Raiders, Dark Lord.
gs-balanceui-remark-wizard-token-antag = Bought a wizard antag token.
gs-balanceui-remark-token-admin-abuse = Bought an admin abuse token.
gs-balanceui-remark-token-hat = Bought a hat token.
gs-balanceui-shop-click-confirm = Click again to confirm
gs-balanceui-shop-purchased = Purchased {$item}
