
namespace DiscordAudioController
{

    public class Program()
    {
        public static ConfigManager.Config config = new();
        private static Thread? discordIPCThread;
        private static Thread? comPortFinder;

        static void Main(string[] args){;
            ConfigManager.Load(ref config);

            discordIPCThread = new Thread(DiscordIPC.DiscordIPCThread);
            discordIPCThread.Start();

            comPortFinder = new Thread(SerialPortManager.SerialPortDeviceThread);
            comPortFinder.Start();


            while (true)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        {
                            bool loop = true;
                            ConsoleDisplay.SupressUpdates = true;
                            do
                            {
                                Console.WriteLine("1. Set Client Id");
                                Console.WriteLine("2. Set Client Secret");
                                Console.WriteLine("3. Select new device port");
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("4. Cancel");
                                Console.ForegroundColor = ConsoleColor.White;

                                var val = Console.ReadKey(true);
                                switch (val.Key)
                                {
                                    case ConsoleKey.D1:
                                        {
                                            config.client_id = ConfigManager.ConfigStringPrompt("Enter a valid Client Id:");
                                            ConfigManager.Save(ref config);
                                            break;
                                        }
                                    case ConsoleKey.D2:
                                        {
                                            config.client_secret = ConfigManager.ConfigStringPrompt("Enter a valid Secret Id:");
                                            ConfigManager.Save(ref config);
                                            break;
                                        }
                                    case ConsoleKey.D3:
                                        {
                                            ConfigManager.ConfigDeviceSelectorPrompt(ref config);
                                            ConfigManager.Save(ref config);
                                            break;
                                        }
                                    case ConsoleKey.D4:
                                        {
                                            loop = false;
                                            Console.WriteLine("Exited!");
                                            ConsoleDisplay.SupressUpdates = false;
                                            ConsoleDisplay.UpdateScreen();
                                            break;
                                        }
                                    default:
                                        {
                                            break;
                                        }
                                }

                            } while (loop);

                            break;
                        }
                    default:
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("To access config press Up Arrow key!");
                            Console.ForegroundColor = ConsoleColor.White;

                            break;
                        }
                }
            }

        }


    }
}

