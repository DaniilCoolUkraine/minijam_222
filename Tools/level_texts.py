# Owned by the texts agent. Keys are object ids (see docs/LEVELS.md). PLACEHOLDERS until the texts agent fills them in.
TEXTS = {
    oid: {
        "story": "TODO story for " + oid,
        "success": "TODO happy reaction for " + oid,
        "partial": "TODO neutral reaction for " + oid,
        "failure": "TODO angry reaction for " + oid,
    }
    for oid in ["axe", "pickaxe", "pitchfork", "scissors", "hammer", "shovel", "plow", "anvil", "scythe", "millstone"]
}
