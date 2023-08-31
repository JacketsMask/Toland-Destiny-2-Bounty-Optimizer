using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace BountyVoiceTracker
{
    internal class Program
    {
        private const string LISTEN_WORD = "tracker";

        static readonly List<string> activeBounties = new List<string>();
        static Tuple<CommandType, string> lastCommand;
        static bool listening = true;

        static void Main(string[] args)
        {

            // Create an in-process speech recognizer for the en-US locale.  
            using (SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine( new System.Globalization.CultureInfo("en-US")))
            {

                // Create and load a dictation grammar.
                var d2BountyGrammar = BuildDestiny2Grammar();
                recognizer.LoadGrammar(d2BountyGrammar);

                // Add a handler for the speech recognized event.  
                recognizer.SpeechRecognized +=
                  new EventHandler<SpeechRecognizedEventArgs>(Recognizer_SpeechRecognized);

                // Configure input to the speech recognizer.  
                recognizer.SetInputToDefaultAudioDevice();

                // Start asynchronous, continuous speech recognition.  
                recognizer.RecognizeAsync(RecognizeMode.Multiple);

                // Display console
                UpdateConsole();

                // Keep the console window open.  
                while (true)
                {
                    Console.ReadLine();
                }
            }
        }

        static Grammar BuildDestiny2Grammar()
        {
            var toggleListeningChoices = new Choices(new string[] { "stop listening", "start listening" });
            var updateBountyChoices = new Choices(new string[] { "add", "remove" });
            var undoChoice = new Choices("undo");
            var clearChoice = new Choices("clear");

            var singeChoices = new Choices(new string[] { "arc", "solar", "void", "stasis", "strand" });
            var enemyChoices = new Choices(new string[] { "fallen", "vex", "taken", "scorn", "cabal", "hive", "lucent hive", "player" });

            var weaponChoices = new Choices(new string[]
            {
                "auto rifle", "bow", "scout rifle", "pulse rifle", "hand cannon", "side arm", "shotgun", "sniper rifle", "trace rifle", "sub machine gun", "smg", "machine gun", "grenade launcher", "fusion rifle", "linear fusion rifle", "rocket launcher", "sword",
            });
            var abilityChoice = new Choices(new string[]
            {
                "abilities", "melee", "grenade", "super", "finisher"
            });
            var killTypeChoice = new Choices(new string[]
            {
                "rapid", "groups", "precision", "single life", "void suppressed", "arc blinded"
            });
            var locationConnectiveChoice = new Choices(new string[] { "on", "in" });
            var activityChoices = new Choices(new string[] { "lost sector", "public events" });
            var locationChoices = new Choices(new string[] { "neptune", "europa", "throne world", "eternity", "dreaming city", "nessus", "moon", "edz", "ee dee zee", "cosmodrome", "pvp", "gambit", "vanguard", "event", "seasonal" });
            var playlistChoices = new Choices(new string[] { "crucible", "iron banner", "gambit", "vanguard", "strikes", "nightfall" });


            var listenPhrase = new Choices(new string[] { LISTEN_WORD });
            List<GrammarBuilder> phraseList = new List<GrammarBuilder>();

            // phrase to toggle whether the bounty tracker is listening or not - "tracker stop listening", "tracker start listening"
            GrammarBuilder toggleListeningPhrase = new GrammarBuilder(listenPhrase);
            toggleListeningPhrase.Append(toggleListeningChoices);
            phraseList.Add(toggleListeningPhrase);

            // phrase to undo the last command - "tracker undo"
            GrammarBuilder undoPhrase = new GrammarBuilder(listenPhrase);
            undoPhrase.Append(undoChoice);
            phraseList.Add(undoPhrase);

            // phrase to clear the bounty list - "tracker clear"
            GrammarBuilder clearPhrase = new GrammarBuilder(listenPhrase);
            clearPhrase.Append(clearChoice);
            phraseList.Add(clearPhrase);

            // phrase to kill a specific enemy type - "tracker vex", "tracker fallen on europa"
            GrammarBuilder enemyTypePhrase = new GrammarBuilder(listenPhrase);
            enemyTypePhrase.Append(updateBountyChoices);
            enemyTypePhrase.Append(enemyChoices);
            enemyTypePhrase.Append(locationConnectiveChoice, 0, 1); // optional connective location word
            enemyTypePhrase.Append(locationChoices, 0, 1); // optional location
            phraseList.Add(enemyTypePhrase);

            // phrase to kill targets in a specific way - "tracker rapid", "tracker precision on europa"
            GrammarBuilder killTypePhrase = new GrammarBuilder(listenPhrase);
            killTypePhrase.Append(updateBountyChoices);
            killTypePhrase.Append(killTypeChoice);
            killTypePhrase.Append(locationConnectiveChoice, 0, 1); // optional connective location word
            killTypePhrase.Append(locationChoices, 0, 1); // optional location
            phraseList.Add(killTypePhrase);

            // phase for weapon specific kills - "tracker add sword", "tracker add hand cannon on europa"
            GrammarBuilder weaponPhrase = new GrammarBuilder(listenPhrase);
            weaponPhrase.Append(updateBountyChoices);
            weaponPhrase.Append(weaponChoices);
            weaponPhrase.Append(locationConnectiveChoice, 0, 1); // optional connective location word
            weaponPhrase.Append(locationChoices, 0, 1); // optional location
            phraseList.Add(weaponPhrase);

            // phrase for singe specific kills - "tracker add arc", "tracker add solar on europa"
            GrammarBuilder genericSingePhrase = new GrammarBuilder(listenPhrase);
            genericSingePhrase.Append(updateBountyChoices);
            genericSingePhrase.Append(singeChoices);
            genericSingePhrase.Append(locationConnectiveChoice, 0, 1); // optional connective location word
            genericSingePhrase.Append(locationChoices, 0, 1); // optional location
            phraseList.Add(genericSingePhrase);

            // phrase for ability kills - "tracker add melee", "tracker add arc abilities", "tracker add grenade on throne world"
            GrammarBuilder abilityPhrase = new GrammarBuilder(listenPhrase);
            abilityPhrase.Append(updateBountyChoices);
            abilityPhrase.Append(singeChoices, 0, 1);
            abilityPhrase.Append(abilityChoice);
            abilityPhrase.Append(locationConnectiveChoice, 0, 1); // optional connective location word
            abilityPhrase.Append(locationChoices, 0, 1); // optional location
            phraseList.Add(abilityPhrase);

            // phrase for adding activity completions - "tracker add lost sector neptune", "tracker add public events cosmodrome"
            GrammarBuilder activityPhrase = new GrammarBuilder(listenPhrase);
            activityPhrase.Append(updateBountyChoices);
            activityPhrase.Append(activityPhrase);
            activityPhrase.Append(locationConnectiveChoice, 0, 1); // optional connective location word
            activityPhrase.Append(locationChoices, 0, 1); // optional location
            phraseList.Add(activityPhrase);

            // phrase for adding playlist completions - "tracker add crucible", "tracker add vanguard"
            GrammarBuilder playlistPhrase = new GrammarBuilder(listenPhrase);
            playlistPhrase.Append(updateBountyChoices);
            playlistPhrase.Append(playlistChoices);
            phraseList.Add(playlistPhrase);

            // combine grammar builder phrases into a new grammar
            Choices grammarChoices = new Choices(phraseList.ToArray());
            var completeGrammar = new Grammar(grammarChoices);
            completeGrammar.Name = "Bounty Voice Tracker Grammar";

            return completeGrammar;
        }

        // Handle the SpeechRecognized event.  
        static void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string command = e.Result.Text;

            // check if listening first
            if (!listening)
            {
                if (command.Contains("start listening"))
                {
                    listening = true;
                    UpdateConsole();
                }
                return;
            }

            // handle the special case of listening
            if (command.Contains("stop listening"))
            { 
                listening = false;
                Console.WriteLine("{ NOT LISTENING }");
                return;
            }

            else if (command.Contains("start listening"))
            {
                string newCommand = command.Replace($"{LISTEN_WORD} add", string.Empty);
                ProcessCommand(newCommand, CommandType.ADD);
                lastCommand = new Tuple<CommandType, string>(CommandType.ADD, newCommand);
            }

            if (command.Contains("add"))
            {
                string newCommand = command.Replace($"{LISTEN_WORD} add", string.Empty);
                ProcessCommand(newCommand, CommandType.ADD);
                lastCommand = new Tuple<CommandType, string>(CommandType.ADD, newCommand);
            }
            else if (command.Contains("remove"))
            {
                string newCommand = command.Replace($"{LISTEN_WORD} remove", string.Empty);
                ProcessCommand(newCommand, CommandType.REMOVE);
                lastCommand = new Tuple<CommandType, string>(CommandType.REMOVE, newCommand);
            }
            else if (command.Contains("undo"))
            {
                string newCommand = command.Replace($"{LISTEN_WORD} undo", string.Empty);
                ProcessCommand(newCommand, CommandType.UNDO);
                lastCommand = new Tuple<CommandType, string>(CommandType.UNDO, newCommand);
            }
            else if (command.Contains("clear"))
            {
                string newCommand = command.Replace($"{LISTEN_WORD} clear", string.Empty);
                ProcessCommand(newCommand, CommandType.CLEAR);
                lastCommand = new Tuple<CommandType, string>(CommandType.CLEAR, newCommand);
            }
            else
            { // unhandled text - doesn't fit into a recognized command somehow
                Console.WriteLine("Unrecognized text: " + e.Result.Text);
                return;
            }

            UpdateConsole();
            Console.WriteLine("Recognized text: " + e.Result.Text);
        }

        static void ProcessCommand(string commandText, CommandType commandType)
        {
            switch (commandType)
            {
                case CommandType.UNDO:
                    {
                        // lastCommand<CommandType,string>
                        switch (lastCommand.Item1)
                        {
                            case CommandType.UNDO:
                                {
                                    // special case: don't undo an undo
                                    break;
                                }
                            case CommandType.ADD:
                                {
                                    // lastCommand<CommandType,string>
                                    ProcessCommand(lastCommand.Item2, CommandType.REMOVE);
                                    break;
                                }
                            case CommandType.REMOVE:
                                {
                                    // lastCommand<CommandType,string>
                                    ProcessCommand(lastCommand.Item2, CommandType.ADD);
                                    break;
                                }
                        }
                        return;
                    }
                case CommandType.ADD:
                    {
                        activeBounties.Add(commandText);
                        return;
                    }
                case CommandType.REMOVE:
                    {
                        activeBounties.Remove(commandText);
                        return;
                    }
                case CommandType.CLEAR:
                    {
                        activeBounties.Clear();
                        return;
                    }
            }
        }

        static void UpdateConsole()
        {
            // clear the console before repopulating it
            Console.Clear();

            // make things pretty
            Console.WriteLine("*************");
            Console.WriteLine("*********");
            Console.WriteLine("******");
            Console.WriteLine();

            if (!activeBounties.Any())
            {
                Console.WriteLine("No tracked bounties. Try adding one by saying:");
                Console.WriteLine($"-->\"{LISTEN_WORD} add sword\".");
                Console.WriteLine($"-->\"{LISTEN_WORD} add arc in pvp\".");
                Console.WriteLine($"-->\"{LISTEN_WORD} add hand cannon on europa\".");
                Console.WriteLine($"-->\"{LISTEN_WORD} add crucible\".");
                Console.WriteLine($"-->\"{LISTEN_WORD} add arc in vanguard\".");
            }
            else
            {
                foreach (var bounty in activeBounties)
                {
                    Console.WriteLine(bounty);
                }
            }

            Console.WriteLine();
            Console.WriteLine("******");
            Console.WriteLine("*********");
            Console.WriteLine("*************");
        }

        enum CommandType
        {
            // START_LISTENING - implicit command, no type required
            // STOP_LISTENING - implicit command, no type required
            ADD,
            CLEAR,
            REMOVE,
            UNDO,
            UNHANDLED
        }
    }
}
