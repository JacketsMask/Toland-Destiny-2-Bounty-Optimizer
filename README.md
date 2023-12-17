# BountyVoiceTracker

A prototype console application that utilizes the .NET Framework [System.Speech.Recognition library](https://learn.microsoft.com/en-us/dotnet/api/system.speech.recognition.speechrecognitionengine) and custom [Grammar](https://learn.microsoft.com/en-us/dotnet/api/system.speech.recognition.grammar) to allow for adding and removing Destiny 2 Bounties from a locally tracked list.

**NOTE:** This application does not interact with the Destiny 2 application, the Destiny API, or the internet in any way.

![bounty-voice-control-sample](https://github.com/JacketsMask/Destiny-2-Bounty-Voice-Control/assets/4825979/f2674af6-401c-4259-9735-03a1a48295af)

Planned Features:
- Expand bounty type data structures to include category data instead of just words.
	- Plan "route" given bounty list, recommending locations to visit and weapon combinations.
		- "I don't have that combination" weapon blacklist.

Features under consideration:
- UI implementation (more than a voice-controlled console app).
- Integration with Destiny API
	- Automatic bounty capture (alternative to voice).
	- Automatic local bounty completion (alternative to voice) (this would be delayed).
