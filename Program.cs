using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Threading;
using Dota2GSI;
using Dota2GSI.Nodes;

namespace Webmilio.Dota2.AdmiralBulldog.CustomVoice
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Instance = new Program(args);
            Instance.Start();
        }

        private readonly Random _random = new Random();
        private DateTime _lastRunesSound = new DateTime(0);
        private int _midasTimer = 0;

        public Program(string[] args)
        {
            Args = args;
        }

        internal void Start()
        {
            Process[] dota2s = null;

            while (dota2s == null || dota2s.Length == 0)
            {
                dota2s = Process.GetProcessesByName("Dota2");

                Thread.Sleep(3000);
            }

            // Create the GSI file in the directory of the current Dota 2 instance.
            CreateGSIFile(dota2s[0]);

            GSL = new GameStateListener(4000);
            GSL.NewGameState += GSL_OnNewGameState;

            // TODO Remove if useless : FillArray(_recentHealth, 100f);

            Console.WriteLine("Starting GameState Integration Engine.");
            if (!GSL.Start())
            {
                Console.WriteLine("GameStateListener could not start. Try running this program as Administrator.\n\rPress any key to exit.");
                Console.ReadLine();

                Environment.Exit(0);
            }

            Console.WriteLine("Listening for game integration calls...");
            Console.WriteLine("Press ESC to quit.");

            do
                while (!Console.KeyAvailable)
                    Thread.Sleep(1000);
            while (Console.ReadKey().Key != ConsoleKey.Escape);
        }

        private void GSL_OnNewGameState(GameState gameState)
        {
            if (FirstState)
            {
                Console.WriteLine("Current Dota version: {0}", gameState.Provider.Version);
                Console.WriteLine("Steam Name: {0}", gameState.Provider.Name);

                FirstState = false;
            }

            try
            {
                // Check if there is no on-going match.
                if (gameState.Map.MatchID == -1)
                {
                    PreviousGameState = null;
                    return;
                }
            }
            catch (ArgumentException ex)
            {
                return;
            }

            // On Game Start
            if (PreviousGameState == null) // Check if the previous match's id is the same as the current one.
            {
                UnmanagedMemoryStream audioStream = Properties.Resources.ResourceManager.GetStream(gameState.Hero.Name);

                if (audioStream != null) // Check if the audio stream actually exists; without this, it would play windows default alert (\a).
                    PlaySound(audioStream);
            }

            // Runes Timer
            // The sound is limited to 1 per 2 minutes since it will otherwise repeat about 5 times ever 5 minutes.
            if (gameState.Map.GameState == DOTA_GameState.DOTA_GAMERULES_STATE_GAME_IN_PROGRESS && DateTime.Now - _lastRunesSound > TimeSpan.FromMinutes(2) && (gameState.Map.ClockTime + 30) % (5 * 60) == 0)
            {
                PlaySound(Properties.Resources.roons);
                _lastRunesSound = DateTime.Now;
            }

            // On Player Died
            if (PreviousGameState != null && !gameState.Hero.IsAlive && PreviousGameState.Hero.IsAlive)
            {
                // TODO Remove if useless : FillArray(_recentHealth, 100f);
            }

            // On Player Win/Lose
            if (gameState.Map.Win_team != PlayerTeam.None)
            {
                if (PreviousGameState.Map.Win_team == PlayerTeam.None) // This is to make sure the code is only run once.
                    if (gameState.Player.Team == gameState.Map.Win_team)
                        PlaySound(Properties.Resources.vivon);
                    else
                        PlaySound(Properties.Resources.wefuckinglost);
            }

            int healthPercentage = gameState.Hero.HealthPercent;

            if (PreviousGameState != null)
            {
                // On Player Heal
                if (gameState.Previously.Hero.Health != 0 && // Previous State health must be higher than 0 (not dead/wraith king passive).
                    gameState.Hero.Health - PreviousGameState.Hero.Health > 200 && // Hero must have gained at least 200 health.
                    healthPercentage - PreviousGameState.Hero.HealthPercent >= 5) // Hero must have gained at least 5% of its health.
                    PlaySound(Properties.Resources.eel);
            }

            // Midas Check
            foreach (Item item in gameState.Items.Inventory)
            {
                if (item.Name == "item_hand_of_midas") // We're doing it inverted from what DatGuy1 did since we might want to add timers for other items.
                {
                    if (item.Cooldown > 0)
                        _midasTimer = 0;

                    else
                    {
                        if (_midasTimer >= 50)
                        {
                            PlaySound(Properties.Resources.useyourmidas);
                            _midasTimer -= 225;
                        }

                        _midasTimer++;
                    }
                }
            }

            PreviousGameState = gameState;
        }


        private void FillArray(float[] array, float value)
        {
            // Populate all recent health with 100%.
            for (int i = 0; i < array.Length; i++)
                array[i] = value;
        }

        private static void PlaySound(UnmanagedMemoryStream sound) => new SoundPlayer(sound).Play();


        private static void CreateGSIFile(Process dota2Process)
        {
            string[] dota2ProcessPath = FindDota2Folder(dota2Process).Split('\\');
            string dota2Path = string.Join("\\", dota2ProcessPath, 0, dota2ProcessPath.Length - 3);

            if (string.IsNullOrWhiteSpace(dota2Path))
            {
                Console.WriteLine("Registry key for steam not found, cannot create Gamestate Integration file");
                Console.ReadLine();
                Environment.Exit(0);
            }
            string gsiFolder = dota2Path + @"\dota\cfg\gamestate_integration";

            try
            {
                if (!Directory.Exists(gsiFolder))
                    Directory.CreateDirectory(gsiFolder);
            }
            catch (UnauthorizedAccessException ex)
            {
                UnauthorizedAccessExceptionDisplay(ex);

                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
                Environment.Exit(0);
            }

            string gsiFile = gsiFolder + @"\gamestate_integration_admiralbulldogVoice.cfg";

            if (File.Exists(gsiFile)) return;

            string[] gsiSettings =
            {
                    "\"Dota 2 Integration Configuration\"",
                    "{",
                    "    \"uri\"           \"http://localhost:4000\"",
                    "    \"timeout\"       \"5.0\"",
                    "    \"buffer\"        \"0.1\"",
                    "    \"throttle\"      \"0.1\"",
                    "    \"heartbeat\"     \"30.0\"",
                    "    \"data\"",
                    "    {",
                    "        \"provider\"      \"1\"",
                    "        \"map\"           \"1\"",
                    "        \"player\"        \"1\"",
                    "        \"hero\"          \"1\"",
                    "        \"abilities\"     \"1\"",
                    "        \"items\"         \"1\"",
                    "    }",
                    "}",

                };

            try
            {
                File.WriteAllLines(gsiFile, gsiSettings);
            }
            catch (UnauthorizedAccessException ex)
            {
                UnauthorizedAccessExceptionDisplay(ex);

                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }

        private static void UnauthorizedAccessExceptionDisplay(UnauthorizedAccessException ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine("\n\rHey! It seems you didn't launch this application in administrator!");
            Console.WriteLine("Sadly, this is required for the app to work.");
            Console.WriteLine("Please launch the application by right-clicking on it and clicking \"Run as Administrator\".\n\r");
        }

        private static string FindDota2Folder(Process dota2Process)
        {
            // I personally feel like this method is outdone by the process-location approach.
            /*RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            if (regKey == null)
                return null;

            // Search for Dota in the main folder.
            string dota2Path = regKey.GetValue("SteamPath") + @"\steamapps\common\dota 2 beta";

            if (Directory.Exists(dota2Path))
                return dota2Path;*/

            // Here is the nasty detection code using the libraryfolders.vdf file.
            return dota2Process.MainModule.FileName;
        }


        public bool FirstState { get; private set; } = true;

        public GameState PreviousGameState { get; private set; }


        public string[] Args { get; }

        public GameStateListener GSL { get; private set; }


        public static Program Instance { get; private set; }
    }
}
