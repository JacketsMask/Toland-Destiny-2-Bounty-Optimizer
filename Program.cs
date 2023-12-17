using BountyVoiceControl.Resources;
using SQLiteEZMode;
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
        private const string SAVE_FILE = "saved-bounties.txt";
        private static readonly string DATABASE_FILE = System.IO.Path.Combine("resources", "bounty-info.sqlite3");

        static readonly List<string> activeBounties = new List<string>();
        static Tuple<CommandType, string> lastCommand;
        static bool listening = true;

        /// Data loaded from the sqlite3 database file

        private static IEnumerable<AbilityTypes> abilityTypes;
        private static IEnumerable<ActivityTypes> activityTypes;
        private static IEnumerable<AmmoTypes> ammoTypes;
        private static IEnumerable<DestinationTypes> destinationTypes;
        private static IEnumerable<ElementTypes> elementTypes;
        private static IEnumerable<EliminationTypes> eliminationTypes;
        private static IEnumerable<EnemyModifierTypes> enemyModifierTypes;
        private static IEnumerable<EnemyTypes> enemyTypes;
        private static IEnumerable<PlayListTypes> playlistTypes;
        private static IEnumerable<WeaponTypes> weaponTypes;

        static void Main(string[] args)
        {
            LoadSqliteData();


            // Create an in-process speech recognizer for the en-US locale.  
            using (SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US")))
            {

                // Create and load a dictation grammar.
                var d2BountyGrammar = BuildDestiny2GrammarFromLoadedData();
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
        static void LoadSqliteData()
        {
            if (!System.IO.File.Exists(DATABASE_FILE))
            {
                throw new System.IO.FileNotFoundException($"Unable to load {DATABASE_FILE}.");
            }
            using (EzDb ezDb = new EzDb(DATABASE_FILE, OperationModes.EXPLICIT_TAGGING))
            {
                ezDb.VerifyType<WeaponTypes>();
                abilityTypes = ezDb.SelectAll<AbilityTypes>();
                activityTypes = ezDb.SelectAll<ActivityTypes>();
                ammoTypes = ezDb.SelectAll<AmmoTypes>();
                destinationTypes = ezDb.SelectAll<DestinationTypes>();
                elementTypes = ezDb.SelectAll<ElementTypes>();
                eliminationTypes = ezDb.SelectAll<EliminationTypes>();
                enemyModifierTypes = ezDb.SelectAll<EnemyModifierTypes>();
                enemyTypes = ezDb.SelectAll<EnemyTypes>();
                playlistTypes = ezDb.SelectAll<PlayListTypes>();
                weaponTypes = ezDb.SelectAll<WeaponTypes>();
            }
        }

        static Grammar BuildDestiny2GrammarFromLoadedData()
        {
            var toggleListeningChoices = new Choices(new string[] { "stop listening", "start listening" });
            var updateBountyChoices = new Choices(new string[] { "add", "remove" });
            var undoChoice = new Choices("undo");
            var clearChoice = new Choices("clear");
            var saveChoice = new Choices("save");
            var loadChoice = new Choices("load");

            //Choices singeChoices = GenerateChoices(elementTypes);
            Choices abilityChoice = CombineChoicesFromDbType(abilityTypes);
            Choices activityChoice = CombineChoicesFromDbType(activityTypes);
            Choices ammoChoice = CombineChoicesFromDbType(ammoTypes);
            Choices destinationChoice = CombineChoicesFromDbType(destinationTypes);
            Choices elementChoice = CombineChoicesFromDbType(elementTypes);
            Choices eliminationChoice = CombineChoicesFromDbType(eliminationTypes);
            Choices enemyModifierChoice = CombineChoicesFromDbType(enemyModifierTypes);
            Choices enemyChoice = CombineChoicesFromDbType(enemyTypes);
            Choices playListChoice = CombineChoicesFromDbType(playlistTypes);
            Choices weaponChoice = CombineChoicesFromDbType(weaponTypes);

            var destinationConnectiveChoice = new Choices(new string[] { "on", "in" });

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

            // phrase to save the bounty list
            GrammarBuilder savePhrase = new GrammarBuilder(listenPhrase);
            savePhrase.Append(saveChoice);
            phraseList.Add(savePhrase);

            // phrase to load the bounty list
            GrammarBuilder loadPhrase = new GrammarBuilder(listenPhrase);
            loadPhrase.Append(loadChoice);
            phraseList.Add(loadPhrase);

            // phrase to kill a specific enemy type - "tracker vex", "tracker fallen on europa"
            GrammarBuilder enemyTypePhrase = new GrammarBuilder(listenPhrase);
            enemyTypePhrase.Append(updateBountyChoices);
            enemyTypePhrase.Append(enemyModifierChoice, 0, 1);
            enemyTypePhrase.Append(enemyChoice);
            enemyTypePhrase.Append(destinationConnectiveChoice, 0, 1); // optional connective location word
            enemyTypePhrase.Append(destinationChoice, 0, 1); // optional location
            phraseList.Add(enemyTypePhrase);

            // phrase to kill targets in a specific way - "tracker rapid", "tracker precision on europa"
            GrammarBuilder killTypePhrase = new GrammarBuilder(listenPhrase);
            killTypePhrase.Append(updateBountyChoices);
            killTypePhrase.Append(eliminationChoice);
            killTypePhrase.Append(destinationConnectiveChoice, 0, 1); // optional connective location word
            killTypePhrase.Append(destinationChoice, 0, 1); // optional location
            phraseList.Add(killTypePhrase);

            // phrase to kill targets with a specific ammo - "tracker add primary", "tracker add heavy on nessus"
            GrammarBuilder ammoTypePhrase = new GrammarBuilder(listenPhrase);
            ammoTypePhrase.Append(updateBountyChoices);
            ammoTypePhrase.Append(ammoChoice);
            ammoTypePhrase.Append(destinationConnectiveChoice, 0, 1); // optional connective location word
            ammoTypePhrase.Append(destinationChoice, 0, 1); // optional location
            phraseList.Add(ammoTypePhrase);

            // phase for weapon specific kills - "tracker add sword", "tracker add hand cannon on europa"
            GrammarBuilder weaponPhrase = new GrammarBuilder(listenPhrase);
            weaponPhrase.Append(updateBountyChoices);
            weaponPhrase.Append(weaponChoice, 1, 4); //1 to four weapon types
            weaponPhrase.Append(destinationConnectiveChoice, 0, 1); // optional connective location word
            weaponPhrase.Append(destinationChoice, 0, 1); // optional location
            phraseList.Add(weaponPhrase);

            // phrase for singe specific kills - "tracker add arc", "tracker add solar on europa"
            GrammarBuilder genericSingePhrase = new GrammarBuilder(listenPhrase);
            genericSingePhrase.Append(updateBountyChoices);
            genericSingePhrase.Append(elementChoice, 1, 3);
            genericSingePhrase.Append(destinationConnectiveChoice, 0, 1); // optional connective location word
            genericSingePhrase.Append(destinationChoice, 0, 1); // optional location
            phraseList.Add(genericSingePhrase);

            // phrase for ability kills - "tracker add melee", "tracker add arc abilities", "tracker add grenade on throne world"
            GrammarBuilder abilityPhrase = new GrammarBuilder(listenPhrase);
            abilityPhrase.Append(updateBountyChoices);
            abilityPhrase.Append(elementChoice, 0, 3);
            abilityPhrase.Append(abilityChoice);
            abilityPhrase.Append(destinationConnectiveChoice, 0, 1); // optional connective location word
            abilityPhrase.Append(destinationChoice, 0, 1); // optional location
            phraseList.Add(abilityPhrase);

            // phrase for adding activity completions - "tracker add lost sector neptune", "tracker add arc public events cosmodrome"
            GrammarBuilder activityPhrase = new GrammarBuilder(listenPhrase);
            activityPhrase.Append(updateBountyChoices);
            activityPhrase.Append(elementChoice, 0, 2);
            activityPhrase.Append(activityChoice);
            activityPhrase.Append(destinationConnectiveChoice, 0, 1); // optional connective location word
            activityPhrase.Append(destinationChoice, 0, 1); // optional location
            phraseList.Add(activityPhrase);

            // phrase for adding playlist completions - "tracker add crucible", "tracker add vanguard"
            GrammarBuilder playlistPhrase = new GrammarBuilder(listenPhrase);
            playlistPhrase.Append(updateBountyChoices);
            playlistPhrase.Append(playListChoice);
            phraseList.Add(playlistPhrase);

            // combine grammar builder phrases into a new grammar
            Choices grammarChoices = new Choices(phraseList.ToArray());
            var completeGrammar = new Grammar(grammarChoices);
            completeGrammar.Name = "Bounty Voice Tracker Grammar";

            return completeGrammar;
        }

        static Choices CombineChoicesFromDbType(IEnumerable<BaseDbType> types)
        {
            Choices choices = new Choices();
            foreach (var type in types)
            {
                choices.Add(type.GenerateChoices());
            }
            return choices;
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
            else if (command.Contains("save"))
            {
                string newCommand = command.Replace($"{LISTEN_WORD} save", string.Empty);
                ProcessCommand(newCommand, CommandType.SAVE);
                lastCommand = new Tuple<CommandType, string>(CommandType.SAVE, newCommand);
            }
            else if (command.Contains("load"))
            {
                string newCommand = command.Replace($"{LISTEN_WORD} load", string.Empty);
                ProcessCommand(newCommand, CommandType.LOAD);
                lastCommand = new Tuple<CommandType, string>(CommandType.LOAD, newCommand);
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
                            case CommandType.SAVE:
                                {
                                    // special case: don't undo a save
                                    break;
                                }
                            case CommandType.LOAD:
                                {
                                    // special case: don't undo a load
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
                case CommandType.SAVE:
                    {
                        System.IO.File.WriteAllLines(SAVE_FILE, activeBounties);
                        return;
                    }
                case CommandType.LOAD:
                    {
                        if (System.IO.File.Exists(SAVE_FILE))
                        {
                            string[] loadedBounties = System.IO.File.ReadAllLines(SAVE_FILE);
                            if (loadedBounties.Length > 0)
                            {
                                activeBounties.Clear();
                                activeBounties.AddRange(loadedBounties);
                            }
                        }
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
            SAVE,
            LOAD,
            UNHANDLED,
        }
    }
}
