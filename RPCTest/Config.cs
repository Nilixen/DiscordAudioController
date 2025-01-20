using System.Text.Json;


namespace DiscordAudioController
{
    public class ConfigManager
    {
        public static readonly string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Nilixen\AudioController\");

        public class Config
        {
            public string client_id { get; set; } = string.Empty;
            public string client_secret { get; set; } = string.Empty;
            public string access_token { get; set; } = string.Empty;
            public DateTime access_token_expire { get; set; } = DateTime.Now;
            public string refresh_token { get; set; } = string.Empty;
            public string device_pid { get; set; } = string.Empty;
            public string device_vid { get; set; } = string.Empty;
        }
        public static void Load(ref Config config)
        {
            if (File.Exists(Path.Combine(path, "config.json")))
            {
                var text = File.ReadAllText(Path.Combine(path, "config.json"));
                config = JsonSerializer.Deserialize<Config>(text) ?? new Config();
            }
            else
            {
                Console.WriteLine("No config detected! Presenting the magic setup tool! <3");

                // config setup
                config.client_id = ConfigStringPrompt("Enter a valid Client Id:");
                config.client_secret = ConfigStringPrompt("Enter a valid Client Secret:");

                // now we need to find the device
                Console.WriteLine("So now we have everything we need to connect to Discord!");
                Console.WriteLine("But we still need to access the device.");
                Console.WriteLine("Select correct COM port of your device. We will store the device ID so that you can plug it into any USB port.");
                Console.WriteLine("You can later change the device using a config menu.");

                ConfigDeviceSelectorPrompt(ref config);


                ConfigManager.Save(ref config);
            }
        }
        public static string ConfigStringPrompt(string question)
        {
            do
            {
                Console.WriteLine(question);
                string? val = Console.ReadLine();
                if (string.IsNullOrEmpty(val)) continue;
                return val;
            } while (true);

        }
        public static void ConfigDeviceSelectorPrompt(ref Config config)
        {
            do
            {
                Console.WriteLine("Enter a valid id (ender invalid to refresh)");
                var ports = SerialPortManager.GetSerialPortsName();
                foreach (var port in ports)
                {
                    Console.WriteLine($"ID: {port.Key} \t NAME: {port.Value}");
                }
                string? val = Console.ReadLine();
                if (string.IsNullOrEmpty(val)) continue;
                if (ports.ContainsKey(val))
                {
                    var deviceIds = SerialPortManager.GetDeviceIds(val);
                    config.device_pid = deviceIds.PID;
                    config.device_vid = deviceIds.VID;
                    break;
                }


            } while (true);
        }

        public static void UpdateTokens(ref Config config, DiscordIPC.AuthTokens tokens)
        {
            Program.config.access_token = tokens.access_token;
            Program.config.access_token_expire = tokens.access_token_expire;
            Program.config.refresh_token = tokens.refresh_token;
        }
        public static void Save(ref Config config)
        {

            var text = JsonSerializer.Serialize(config);
            Console.WriteLine(text);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "config.json"),text);

        }

    }
}
