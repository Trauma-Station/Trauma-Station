ent-SpawnPointGhostBlob = Blob spawner
    .suffix = DEBUG, Ghost Role Spawner
    .desc = { ent-MarkerBase.desc }
ent-MobBlobPod = Blob Drop
    .desc = An ordinary blob fighter.
ent-MobBlobBlobbernaut = Blobbernaut
    .desc = An elite blob fighter.
ent-BaseBlob = basic blob.
    .desc = { "" }
ent-NormalBlobTile = Regular Tile Blob
    .desc = An ordinary part of the blob required for the construction of more advanced tiles.
ent-CoreBlobTile = Blob Core
    .desc = The most important organ of the blob. By destroying the core, the infection will cease.
ent-FactoryBlobTile = Blob Factory
    .desc = Spawns Blob Drops and Blobbernauts over time.
ent-ResourceBlobTile = Resource Blob
    .desc = Produces resources for the blob.
ent-NodeBlobTile = Blob Node
    .desc = A mini version of the core that allows you to place special blob tiles around itself.
ent-StrongBlobTile = Strong Blob Tile
    .desc = A reinforced version of the regular tile. It does not allow air to pass through and protects against brute damage.
ent-ReflectiveBlobTile = Blob Reflective Tiles
    .desc = It reflects lasers, but does not protect against brute damage as well.
    .desc = { "" }
objective-issuer-blob = Blob


ghost-role-information-blobbernaut-name = Blobbernaut
ghost-role-information-blobbernaut-description = You are a Blobbernaut. You must defend the blob core. Use + or +e in chat to talk in the Blobmind.

ghost-role-information-blob-name = Blob
ghost-role-information-blob-description = You are the Blob Infection. Consume the station.

roles-antag-blob-name = Blob
roles-antag-blob-objective = Reach critical mass.

guide-entry-blob = Blob

# Popups
blob-target-normal-blob-invalid = Wrong blob type, select a normal blob.
blob-target-factory-blob-invalid = Wrong blob type, select a factory blob.
blob-target-node-blob-invalid = Wrong blob type, select a node blob.
blob-target-close-to-resource = Too close to another resource blob.
blob-target-nearby-not-node = No node or resource blob nearby.
blob-target-close-to-node = Too close to another node.
blob-target-already-produce-blobbernaut = This factory has already produced a blobbernaut.
blob-cant-split = You can not split the blob core.
blob-not-have-nodes = You have no nodes.
blob-not-enough-resources = Not enough resources.
blob-help = Only God can help you.
blob-swap-chem = In development.
blob-mob-attack-blob = You can not attack a blob.
blob-get-resource = +{ $point }
blob-spent-resource = -{ $point }
blobberaut-not-on-blob-tile = You are dying while not on blob tiles.
carrier-blob-alert = You have { $second } seconds left before transformation.

blob-mob-zombify-second-start = { $pod } starts turning you into a zombie.
blob-mob-zombify-third-start = { $pod } starts turning { $target } into a zombie.

blob-mob-zombify-second-end = { $pod } turns you into a zombie.
blob-mob-zombify-third-end = { $pod } turns { $target } into a zombie.

blobberaut-factory-destroy = factory destroy
blob-target-already-connected = already connected


# UI
blob-chem-swap-ui-window-name = Swap chemicals

blob-alert-out-off-station = The blob was removed because it was found outside the station!

# Announcment
blob-alert-recall-shuttle = The emergency shuttle can not be sent while there is a level 5 biohazard present on the station.
blob-alert-detect = Confirmed outbreak of level 5 biohazard aboard the station. All personnel must contain the outbreak.
blob-alert-critical = Biohazard level critical, nuclear authentication codes have been sent to the station. Central Command orders any remaining personnel to activate the self-destruction mechanism.
blob-alert-critical-NoNukeCode = Biohazard level critical. Central Command orders any remaining personnel to seek shelter, and await resque.
blob-alert-shuttle-arrived = Biohazard detected on board. All personnel must evacuate immediately.

# Actions
blob-teleport-to-node-action-name = Jump to Node (0)
blob-teleport-to-node-action-desc = Teleports you to a random blob node.
blob-help-action-name = Help
blob-help-action-desc = Get basic information about playing as blob.

# Ghost role
blob-carrier-role-name = Blob carrier
blob-carrier-role-desc =  A blob-infected creature.
blob-carrier-role-rules = You are an antagonist. You have 10 minutes before you transform into a blob.
                        Use this time to find a safe spot on the station. Keep in mind that you will be very weak right after the transformation.
blob-carrier-role-greeting = You are a carrier of Blob. Find a secluded place at the station and transform into a Blob. Turn the station into a mass and its inhabitants into your servants. We are all Blobs.

# Verbs
blob-pod-verb-zombify = Zombify
blob-verb-remove-blob-tile = Remove Blob

# Alerts
blob-resource-alert-name = Core Resources
blob-resource-alert-desc = Your resources produced by the core and resource blobs. Use them to expand and create special blobs.
blob-health-alert-name = Core Health
blob-health-alert-desc = Your core's health. You will die if it reaches zero.

# Greeting
blob-role-greeting =
    You are blob - a parasitic space creature capable of destroying entire stations.
        Your goal is to survive and grow as large as possible.
        You are almost invulnerable to physical damage, but heat can still hurt you.
        Use Alt+LMB to upgrade normal blob tiles to strong blob and strong blob to reflective blob.
        Make sure to place resource blobs to generate resources.
        Keep in mind that resource blobs and factories will only work when next to node blobs or cores.
        You may use + or +e in chat to use the Blobmind to talk to your minions.
blob-zombie-greeting = You were infected and raised by a blob spore. Now you must help the blob take over the station. Use +e in chat to talk in the Blobmind.

# End round
blob-round-end-agent-name = blob infection

# Objectivies
objective-condition-blob-capture-title = Take over the station
objective-condition-blob-capture-description = Your only goal is to take over the whole station. You need to have at least {$count} blob tiles.
objective-condition-success = { $condition } | [color={ $markupColor }]Success![/color]
objective-condition-fail = { $condition } | [color={ $markupColor }]Failure![/color] ({ $progress }%)

# Admin Verbs

admin-verb-make-blob = Make the target into a blob carrier.
admin-verb-text-make-blob = Make Blob Carrier

# Language
language-Blob-name = Blob
chat-language-Blob-name = Blob
language-Blob-description = Bleeb bob! Blob blob!
