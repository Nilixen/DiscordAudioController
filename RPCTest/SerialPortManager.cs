using System.IO.Ports;
using System.Management;


namespace DiscordAudioController
{
    public class SerialPortManager
    {
        public static SerialPort serialPort = new();
        public static Dictionary<string,string> GetSerialPortsName()
        {
            //Console.WriteLine("Scanning serial ports...");

            using (var searcher = new ManagementObjectSearcher
                            ("SELECT * FROM WIN32_SerialPort"))
            {
                string[] portnames = SerialPort.GetPortNames();

                var ports = searcher.Get().Cast<ManagementBaseObject>().ToList();
                var tList = (from n in portnames
                             join p in ports on n equals p["DeviceID"].ToString()
                             select new { PortName = n, Caption = p["Caption"].ToString() })
                             .GroupBy(x => x.PortName)
                             .Select(g => g.First())
                             .ToDictionary(x => x.PortName, x => x.Caption);

                return tList;
            }
        }
        public static ManagementBaseObject FindByComPort(string comPort)
        {
            using (var searcher = new ManagementObjectSearcher
                            ($"SELECT * FROM WIN32_SerialPort WHERE Caption like '%({comPort}%'"))
            { 
                return searcher.Get().Cast<ManagementBaseObject>().First();
            }
        }

        public static void Open(ref SerialPort serialPort, string port)
        {
            serialPort = new SerialPort(port);
            serialPort.ReadTimeout = 200;
            serialPort.WriteTimeout = 200;
            serialPort.BaudRate = 115200;
            serialPort.ReadBufferSize = 4096;
            serialPort.RtsEnable = true;
            serialPort.Open();
        }

        public static void Close(SerialPort port)
        {
            port.Close();
        }

        public class DeviceIds
        {
            public string PID { get; set; } = string.Empty;
            public string VID { get; set; } = string.Empty;
        }
        public static string GetComPortByDeviceIds(DeviceIds deviceIds)
        {
            Dictionary<string,string> ports = GetSerialPortsName();
            foreach (var port in ports)
            {
                var lookupIds = GetDeviceIds(port.Key);
                if (deviceIds.VID == lookupIds.VID && deviceIds.PID == lookupIds.PID)
                {
                    return port.Key;
                }
            }
            return "";
        }
        public static DeviceIds GetDeviceIds(string comPort)
        {
            ManagementBaseObject port = FindByComPort(comPort);
            string pnpdeviceid = port["PNPDeviceID"]?.ToString() ?? "";
            int vidPos = pnpdeviceid.IndexOf("VID_");
            string vid = "";
            if (vidPos >= 0)
            {
                int vidEndPos = pnpdeviceid.IndexOf('&', vidPos);
                vid = pnpdeviceid.Substring(vidPos, vidEndPos - vidPos);
            }
            int pidPos = pnpdeviceid.IndexOf("PID_");
            string pid = "";
            if (pidPos >= 0)
            {
                int pidEndPos = pnpdeviceid.IndexOf('&', pidPos);
                pid = pnpdeviceid.Substring(pidPos, pidEndPos - pidPos);
            }
            return new DeviceIds()
            {
                PID = pid,
                VID = vid
            };
        }

        /// <summary>
        /// Thread that tries to find device defined in config and connect to it
        /// </summary>
        public static void SerialPortDeviceThread()
        {
            
            while (true)
            {

                if (!serialPort.IsOpen)
                {
                    //Console.WriteLine("Looking for device...");
                    var port = SerialPortManager.GetComPortByDeviceIds(new SerialPortManager.DeviceIds() { PID = Program.config.device_pid, VID = Program.config.device_vid });

                    if (!string.IsNullOrEmpty(port))
                    {
                        Open(ref serialPort, port);
                        serialPort.DataReceived += SerialRead;
                        // send current settings
                        SendVoiceSettings();

                        ConsoleDisplay.Statuses.Serial.ServiceStatus = ConsoleDisplay.statusEnum.WORKING;
                        ConsoleDisplay.Statuses.Serial.Message = "Connected!";
                        ConsoleDisplay.UpdateScreen();

                        continue;
                    }
                    else
                    {
                        ConsoleDisplay.Statuses.Serial.ServiceStatus = ConsoleDisplay.statusEnum.FAILED;
                        ConsoleDisplay.Statuses.Serial.Message = "Couldn't find the device!";
                        ConsoleDisplay.UpdateScreen();
                        Thread.Sleep(1000);
                        ConsoleDisplay.Statuses.Serial.ServiceStatus = ConsoleDisplay.statusEnum.LOADING;
                        ConsoleDisplay.Statuses.Serial.Message = "Looking for the device!";
                        ConsoleDisplay.UpdateScreen();
                    }

                    Thread.Sleep(4000);
                }
            }
        }

        private static async void SerialRead(object sender, SerialDataReceivedEventArgs e)
        {
            var line = serialPort.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                if (line[0] == '1')
                {
                    var payload = new
                    {
                        cmd = "SET_VOICE_SETTINGS",
                        args = new { mute = !DiscordIPC.VoiceSettings.mute },
                        nonce = Guid.NewGuid().ToString() // Unikalne ID
                    };
                    try
                    {
                        await DiscordIPC.client.SendCommandWeakTypeAsync(payload);
                    }
                    catch(Exception){
                        //Console.WriteLine($"Something went wrong with the IPC: Prolly Nonce issue, to be investigated. But it works am I right?");
                    }
                }
                if (line[1] == '1')
                {
                    var payload = new
                    {
                        cmd = "SET_VOICE_SETTINGS",
                        args = new { deaf = !DiscordIPC.VoiceSettings.deaf },
                        nonce = Guid.NewGuid().ToString() // Unikalne ID
                    };
                    try
                    {
                        await DiscordIPC.client.SendCommandWeakTypeAsync(payload);
                    }
                    catch (Exception)
                    {
                        //Console.WriteLine($"Something went wrong with the IPC: Prolly Nonce issue, to be investigated. But it works am I right?");
                    }

                }
                if (line[2] == '1')
                {

                    var payload = new
                    {
                        cmd = "SET_VOICE_SETTINGS",
                        args = new {
                            mode = new {
                                type = (DiscordIPC.VoiceSettings.mode_type == "PUSH_TO_TALK"? "VOICE_ACTIVITY":"PUSH_TO_TALK")
                            }, 
                        },
                        nonce = Guid.NewGuid().ToString() // Unikalne ID
                    };
                    try
                    {
                        await DiscordIPC.client.SendCommandWeakTypeAsync(payload);
                    }
                    catch (Exception)
                    {
                        //Console.WriteLine($"Something went wrong with the IPC: Prolly Nonce issue, to be investigated. But it works am I right?");
                    }


                }

            }
        }

        public static void SendVoiceSettings()
        {
            if (serialPort.IsOpen)
            {
                serialPort.WriteLine($"{(DiscordIPC.VoiceSettings.mute ? 1 : 0)}{(DiscordIPC.VoiceSettings.deaf ? 1 : 0)}{(DiscordIPC.VoiceSettings.mode_type == "PUSH_TO_TALK" ? 1 : 0)}");
            }
        }
    }
}
