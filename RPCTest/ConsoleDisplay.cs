using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordAudioController
{
    public class ConsoleDisplay
    {
        
        public enum statusEnum
        {
            WORKING,
            FAILED,
            LOADING,
        }
        public class Status
        {
            public string Message { get; set; } = "Loading";
            public statusEnum ServiceStatus { get; set; } = statusEnum.LOADING;
        }
        public static class Statuses
        {
            public static Status Discord { get; set; } = new() { Message = "Trying to connect to Discord..." };
            public static Status Serial { get; set; } = new() { Message = "Looking for the device!" };
        };

        public static bool SupressUpdates = false;

        public static void UpdateScreen()
        {
            if (SupressUpdates) { return; }
            int w = Console.WindowWidth, h = Console.WindowHeight;

            Console.Clear();
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            // display headline
            string headline = "Nilix's Audio Controller";
            Console.SetCursorPosition(w / 2 - headline.Length/2, 0);
            Console.Write(headline);

            string ipcString = "Discord status:";
            Console.SetCursorPosition(1, 2);
            Console.Write(ipcString);

            string serialString = "Device status:";
            Console.SetCursorPosition(w - serialString.Length - 1, 2);
            Console.Write(serialString);

            Console.ForegroundColor = (Statuses.Discord.ServiceStatus == statusEnum.LOADING)? ConsoleColor.Cyan: (Statuses.Discord.ServiceStatus == statusEnum.FAILED) ? ConsoleColor.Red : ConsoleColor.Green;
            Console.SetCursorPosition(1, 3);
            Console.Write(Statuses.Discord.Message);

            Console.ForegroundColor = (Statuses.Serial.ServiceStatus == statusEnum.LOADING) ? ConsoleColor.Cyan : (Statuses.Serial.ServiceStatus == statusEnum.FAILED) ? ConsoleColor.Red : ConsoleColor.Green;
            Console.SetCursorPosition(w-1-Statuses.Serial.Message.Length, 3);
            Console.Write(Statuses.Serial.Message);

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.SetCursorPosition(0, 4);
        }
        
    }
}
