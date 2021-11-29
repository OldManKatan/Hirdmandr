# Hirdmandr - NPC Villagers mod for Valheim

Hirdmandr is a mod created using the [Jötunn Valheim Library](https://github.com/Valheim-Modding/Jotunn). 

Become a Jarl of Valheim! Recruit hirdmandr (House-men) to your banner, grow your villages, care for your subjects, and elevate your holdings to glory. Your hirdmandr can be assigned a variety of roles, from Farmer to Furnace Operator, from Town Guard to Elite Warrior. Hirdmandr also have skills across a range of combat and non-combat pursuits. Any hirdmandr can grow to be a master of any skill over time by using those skills. Each hirdmandr also has a unique personality that includes tasks and objects they value or disdain. Be a good leader by learning about your valuable allies, assign them duties based on their aptitudes, and watch them florish or falter based on your choices!

## Vision 

This mod introduces functional NPCs to the world of Valheim. These NPCs can be found while exploring the 10th world and rescued by players. Rescued NPCs are then brought to live in any area of the game world that the player has improved with at least a workbench and a fire. On the surface, the goals of adding these NPCs is: to make the world feel more alive, to justify the structures found around the world, and help the player to feel less isolated. 

At a deeper level, introduction of NPCs add new mechanics to the game that encourages players to develop new intrinsic goals like, building more structures, increasing comfort in larger areas of their strongholds, and interacting with NPCs. The primary mechanic to drive these goals is using these NPCs to shift common or monotonous tasks away from direct player action to indirect player intervention. This trades tasks (like feeding kilns) away from the player, and trades in high level tasks

### Goals

* Allow players to build towns populated by Vikings, who go about their daily business and add a sense of business and liveliness to player strongholds and outposts.
* Give players interesting mechanics to engage with that prolong engagement without needless grinding.
* Provide more reasons for players to use and experiment with Valheim’s extensive building system. 
* Provide opportunities (especially on multiplayer servers) for players who spend less time exploring and adventuring to have an engaging experience being a Jarl, without a need to leave their base camp.
* Provide a method to automate some of the time-consuming and monotonous activities within the game, without breaking the balance or progression systems, and respecting the atmosphere of vanilla Valheim.

### What does this mod add to the Valheim?

* Adds the npc_entity to the game, this is a creature based on the Player prefab that can randomly appear with the entire span of Character Generation options including male or female models.
* A deep behvior system for the NPC Hirdmandr that sees them working, socializing, sleeping, and more based on the time of day, their job, and more.
* A mood and thought system that is strongly inspired by the depth and complexity of the personality system from the game [Dwarf Fortress](http://www.bay12games.com/dwarves/). Special thanks to Tarn and Zach Adams for their pioneering work!
* Several new placeable pieces (Fires, Chests, Beds, etc.) that are visually identical to existing pieces but affect the behavior of the NPCs.
* Hugin tutorials to help guide players.

# Mechanics Overview

The mod mechanics are designed to be nuianced enough to provide depth and interest for intrinsically motivated players. The surface mechanics try to be simple enough that players may ignore most of the nuiance and still have a satisfying (but less optimized) experience. Most mechanics are centered around improving the mood and raising the skills of your hirdmandr, which makes them more efficient at their assigned duties.

## Find and Rescue

Hirdmandr are warriors that were previously brought to Valheim by Valkyries, but failed in the tasks set before them by Odin. Players can find them by exploring Valheim and investigating the structures they encounter. All NPCs found in the world spawn as part of a location, and are defended by Biome-appropriate creatures. Examples include the towers in the Black Forest that are surrounded by swarms of Grey Dwarves, or Fuling Villages. Players can rescue these NPCs to acquire a new hirdmandr, but they must escort that NPC back to a safe place before the NPC will become one of that player’s Hirdmandr.

Once an NPC is found, a player can talk to them which presents a GUI that gives limited information about the NPC's skills and personality. Whichever player clicks the "Follow Me" button in the GUI first becomes that NPC's Jarl (King or Warlord). The NPC will follow that player (unless told to wait) and attempt to defend themselves. The player will need to lead the NPC back to one of their camps or strongholds with (at least) a workbench and a fire before the NPC will truly become a Hirdmandr.

## Hirdmandr

Once rescued, NPCs truly become hirdmandr, and Players can access their management menu. This menu gives you a brief overview of their thoughts, mood, personality, and skills. This menu is also where players set duties. Hirdmandr can be assigned a role of either Artisan or Warrior and this can be changed at any time. Once their role is set, additional menu options are available to customize their duties within each role.

### Schedules

Each hirdmandr follows a daily schedule. They will rest for a portion of the day, sleeping in an NPC Bed if one is provided. After waking, they will take care of themselves, eating if food is available before heading to a meeting area (defined by an NPC Fire) to socialize. Next, they start their work day which can take many forms, based on their assigned duties. At the end of the day, they spend a little time socializing again before heading back to rest.

### Artisans

Artisans fulfill jobs including Wood Burner (work Kilns), Furnace Operator (work smelters), Farmer (tend crops), Cook (work cauldrons), and Baker (work ovens). A hirdmandr may be assigned to multiple duties and will attempt to find a worksite for each duty, starting with their highest skilled. Worksites are defined by NPC Chests being placed near to the related pieces (kilns, cauldrons, etc). When checking worksites, if no raw materials are available the worksite is not considered valid and the next job is checked.

### Warriors

Warriors have two duties they can be assigned to: Thegen, which are town guards that defend your stronghold from wandering monsters and raids; and Himthiki, which are the elite warriors that accompany the player on their adventures. Both warrior duties may have the Gatherer job enabled, which allows them to collect pickable items from the ground whenever they pass near them (like stones, thistles, cloudberries, etc).

## Personality and Moods

All hirdmandr have a personality based on three virtues and three domains. Hirdmandr either **Value** these virtues and domains, **don't care** about them, or **Disdain** them. When engaging in activities hirdmandr will generate a **thought** about the activity, which can be anything from improving a skill, talking to a friend, or fighting a monster. Their personality determines whether this thought will make their mood better or worse. Hirdmandr with a good (high) mood will work faster, attack faster, and generally be more effective. Almost all NPCs will have more **Values** than **Disdains**, but not always.

**Virtues**
1. **Art**: This represents how the NPC feels about doing Artisan jobs and other non-combat activities.
2. **Valor**: This represents how the NPC feels about doing Warrior jobs and being in or around combat.
3. **Relationships**: This represents how important relationships with other NPCs and Players are to the NPC.

**Domains**
1. **Learning**: This domain applies any time a thought is related to raising a skill, changing a relationship, or learning something.
2. **Authority**: This domain applies any time a thought comes from interacting with their Jarl or doing assigned duties.
3. **Comfort**: This domain applies whenever surroundings or environment relate to a thought.

