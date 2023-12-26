# Toland - Destiny 2 Bounty Completion Optimizer

A prototype console application that utilizes the .NET Framework [System.Speech.Recognition library](https://learn.microsoft.com/en-us/dotnet/api/system.speech.recognition.speechrecognitionengine) and custom [Grammar](https://learn.microsoft.com/en-us/dotnet/api/system.speech.recognition.grammar) to allow for adding and removing Destiny 2 Bounties from a locally tracked list.

**NOTE:** This application does not interact with the Destiny 2 application, the Destiny API, or the internet in any way.

![bounty-voice-control-sample](https://github.com/JacketsMask/Destiny-2-Bounty-Voice-Control/assets/4825979/f2674af6-401c-4259-9735-03a1a48295af)

# Introduction
There are several ways to gather and manage Destiny 2 Bounty information. Here in this living design document I'll detail some of the options considered and thoughts for each. As a reminder the core application focus is on resolving a "daily rotation", where bounties are:
- added for a character
- saved/exported
- a route is optimized
- the route is run by the player
- the route is loaded/imported for the next character 

# Bounty Data
## Format
Here's an example of how bounty data is managed and the criteria that may be attached to each bounty. Each form of data entry must support populating these criteria. This format will expand, and is customizable via the [bounty-info.sqlite3 db file](https://github.com/JacketsMask/Destiny-2-Bounty-Voice-Control/blob/master/resources/bounty-info.sqlite3).
- Kill
    - Modifiers:
        - enemy type (fallen, lucent hive)
        - enemy modifier type (powerful)
        - ability type (grenade, melee, finisher, super)
        - ammo type (kinetic, special, heavy, primary, energy)
        - weapon type (pulse rifle, sword)
        - elimination type (rapid, precision, arc-blinded)
        - element type (solar, void, stasis)
        - activity type (lost sector, public event, patrol)
        - destination (europa, throne world, crucible, iron banner)
- Complete
    - Modifiers:
        - activity type (lost sector, public event, patrol)
        - destination (europa, throne world, crucible, iron banner)

## Gathering methods
### Voice - **[Complete]**
This is is implemented from the above sqlite DB file and [the BuildDestiny2GrammarFromLoadedData() method](https://github.com/JacketsMask/Destiny-2-Bounty-Voice-Control/blob/master/Program.cs#L91). Notable shortcomings to this method is the accuracy of the default Windows recognizer used, and the occasional stigma of talking to oneself while playing Destiny. I plan to continue supporting this method of bounty entry, but want to expand functionality to other requested use cases. As an upside, this is a highly accessible approach that doesn't require switching focus from the game.

### Text-input - **[Planned]**
Most bounties in Destiny 2 can be summarized with brief text blurbs. Using a common shorthand syntax will support rapidly adding bounties with a focus on speed of entry. The upside to this approach is that it may be quickly done while picking up Bounties from the Destiny 2 official mobile app.

### GUI-selected - **[Planned]**
This would involve defining an active bounty by clicking elements within a developed C# application frontend. This approach would allow for similar functionality with improved ease-of-use at the cost of more inputs to create each bounty. This may be folded into the "text-input" bounty entry system or used for advanced bounty editing depending on development.

### Bungie API - **[Under Consideration]**
In some ways this is the ultimate expression of data entry - simply pulling from another source. Obviously this would cut down manual entry time drastically in the immediate case. This may require additional complexity:
- Potential impact of API caching 
- Potential impact of API uptime
- Understanding of bounty API data structures
- Interpretation of bounty text resulting from API result
- OAuth implementation and configuration for an applet that (as of this writing) is entirely offline

### OCR/Pattern matching - **[Rejected]**
While an early consideration for data entry, variation in opacity and mixed results for accuracy make this a complicated choice for gathering bounty information. For the time being this approach is considered nonviable. 

## Resolution methods
### Voice - **[In Progress]**
As of now with the console applet, you can resolve bounties with your voice, but there is room for improvement by aliasing the tracked bounties instead of requiring 1:1 phrase matching.

### GUI-selected - **[Planned]**
It should be relatively quick to alt-tab to mark a specific bounty as cleared, especially if they are presented in an accessible format. This should regenerate the suggested bounty route.

# Route Planning
At it's core, the application is intended to optimize the Destiny 2 playing experience to quick and efficiently clear bounties. Given the existing bounties, this is the optimization plan:
1. Destination: If a bounty requires completion at a specific destination, that is a must. These be prioritized the highest in route planning. Destinations will be suggested as the "next stop" in the route until all bounties for a location are resolved. For each location, bounties specific to that location will be listed beneath it. This may include loadout recommendations if it informs them.
1. Loadout recommendations: Pair up weapons required to clear bounties and the elements that must be required. Example: "solar glaive"
    - Ignore list: List of combinations of weapons and elements to ignore. It could be that the user has no strand machine gun, or stasis trace rifle.
    - Favorite list: List of combinations to prefer (in order), if they are possible, and an optional alias. Examples may be solar hand cannon "Sunshot", or arc submachine gun "Ikelos SMG". The recommendation would be for specific weapon types that excel at clearing enemies to quickly complete bounties.
