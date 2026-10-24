# Owned by the texts agent. Keys are object ids (see docs/LEVELS.md).
# Plain ASCII only. story <= 230 chars, reactions <= 90 chars.
# Hint vocabulary: durability = sturdy/doesn't break or chip/lasts; haste = fast/quick/hurry; sharpness = sharp/cuts/dull;
# efficiency = less effort/more done per swing; luck = lucky/finds treasure/fortune; protection = safe/guards/shield from harm.
# Tiers: I = a tiny bit/just slightly; II = decently/properly/quite; III = the best/as much as possible/legendary; any = plain mention.
TEXTS = {
    # 1. Wren the woodcutter: durability (any) - handle only
    "axe": {
        "story": "Hi, I'm Wren the woodcutter. My axe handle snaps like a dry twig whenever I swing it. "
                 "Please give me a handle that lasts!",
        "success": "Chop chop! This handle will never snap. Thank you!",
        "partial": "Okay, it holds. That should do the work.",
        "failure": "Hey, my handle is still bare! I can't chop with my bare hands!",
    },
    # 2. Dora the miner: efficiency III (head), durability I (handle)
    "pickaxe": {
        "story": "Dora the miner here, and my arms are noodles. The head must get as much done with every single swing "
                 "as possible, the best there is! The handle only needs to be just slightly sturdy.",
        "success": "Yay, now I can dig all the way to the mouse caves!",
        "partial": "Okay, the tunnel got a bit longer. That should do the work.",
        "failure": "No, now it's even worse! My arms hurt twice as much!",
    },
    # 3. Farmer Hank: durability II (handle), sharpness II (prongs)
    "pitchfork": {
        "story": "Bah, Farmer Hank here. The handle snaps every time I toss a hay bale, so make it properly sturdy. "
                 "The prongs should be decently sharp, but nothing crazy, I only stab hay.",
        "success": "Well, I'll be! Hay flies like butter and nothing snaps! Best fork ever!",
        "partial": "Hmph. It's a fork. It forks. Fine.",
        "failure": "Useless! I've met turnips with more sense than this fork!",
    },
    # 4. Lina the tailor: sharpness III, durability (any)
    "scissors": {
        "story": "Lina the tailor, darling. My scissors must cut velvet and leather like air: the sharpest in the whole land, "
                 "legendary! And they must not chip or break after one week, please.",
        "success": "Snip! The velvet falls apart like butter! Darling, you are a genius!",
        "partial": "Well, they cut. The hem is a bit ragged, but it will do.",
        "failure": "Now they chew my cloth! My best silk is ruined, ruined!",
    },
    # 5. Bruno the builder: durability II, efficiency (any), haste III
    "hammer": {
        "story": "Bruno the builder here. I'm in a hurry, a big one! I want this hammer as fast as possible, ten nails a second. "
                 "It should be properly sturdy and never break, and take less effort per swing.",
        "success": "Bam bam bam! Ten nails a second and my arm feels great! Fantastic!",
        "partial": "Okay, it hammers. Nothing special, but fine.",
        "failure": "Now I hit my own thumb every time! Take it back!",
    },
    # 6. Gus the well-digger: efficiency III, durability I, luck II
    "shovel": {
        "story": "Gus the well-digger here, and I'm worn out. I want to move as much dirt per scoop as possible, legendary! "
                 "The blade should be just slightly sturdy, I do hit rocks. And I'd like to be decently lucky and find treasure down there.",
        "success": "Treasure! A golden spoon! And my back doesn't even hurt!",
        "partial": "I found a button. Not a treasure, but fine.",
        "failure": "Now I only find old boots, and the hole got bigger!",
    },
    # 7. Farmer Greta: durability II, efficiency III, luck I, protection II
    "plow": {
        "story": "Farmer Greta here. This plow should do a whole field with the least effort, the best it can be, legendary! "
                 "It must be properly sturdy on stones and keep my oxen decently safe. Maybe a tiny bit lucky, too.",
        "success": "The whole field in one morning! Sturdy, safe, and I found a coin! Wonderful!",
        "partial": "The field is plowed. Slowly. Well, it will do.",
        "failure": "Now the oxen are sulking and the field is a mess! Out!",
    },
    # 8. Old Brandt the smith: durability III, protection III, sharpness II, haste I
    "anvil": {
        "story": "Old Brandt the smith. Forty years of hammering, and this anvil must last forever, the sturdiest ever, "
                 "and guard me from every spark, as safe as possible! Blades forged on it come out properly sharp, a tiny bit faster.",
        "success": "Ha! Forty more years of forging! This anvil sings like new!",
        "partial": "Hm. It rings. Not like in my youth, but it rings.",
        "failure": "My poor old anvil! You've wrecked it, you young rascal!",
    },
    # 9. Reaper Wilma: sharpness III, haste II, luck III, efficiency I
    "scythe": {
        "story": "Reaper Wilma here, and harvest is coming! My scythe must cut as sharp as possible, legendary, and be quite quick, "
                 "no time to waste! I want the best fortune too, treasure in every sheaf. And just slightly less effort, my back!",
        "success": "A golden harvest in one day! Even the wheat bowed to me!",
        "partial": "The wheat is cut. The fortune can wait for next year.",
        "failure": "Now it cuts nothing and the crows are laughing at me! Shameful!",
    },
    # 10. Miller Magnus (finale): durability III, efficiency II, protection III, haste I, luck II
    "millstone": {
        "story": "Villagers, the flood is coming and this is the last millstone! It must never break, the sturdiest ever, and shield "
                 "the mill from harm, as safe as possible! Grind properly with less effort, a tiny bit quicker, and quite lucky!",
        "success": "The mill stands, the flood is gone, and there is bread for all! You saved the village!",
        "partial": "The mill still turns, barely. We will survive the winter. Maybe.",
        "failure": "The flood took the mill! Everything is flour! Everything!",
    },
}
