using System;
using System.Collections.Generic;
using System.IO;
using ColorzCore.DataTypes;

namespace ColorzCore.IO
{
    public static class LoggerExtensions
    {
        // shorthand helpers

        public static void Message(this Logger self, Location? location, string message) => self.MessageTrace(Logger.MessageKind.MESSAGE, location, message);
        public static void Warning(this Logger self, Location? location, string message) => self.MessageTrace(Logger.MessageKind.WARNING, location, message);
        public static void Error(this Logger self, Location? location, string message) => self.MessageTrace(Logger.MessageKind.ERROR, location, message);

        private static void MessageTrace(this Logger self, Logger.MessageKind kind, Location? location, string message)
        {
            if (location is Location myLocation && myLocation.macroLocation != null)
            {
                MacroLocation macroLocation = myLocation.macroLocation;
                self.MessageTrace(kind, macroLocation.Location, message);
                self.Message(Logger.MessageKind.NOTE, location, $"From inside of macro `{macroLocation.MacroName}`.");
            }
            else
            {
                string[] messages = message.Split('\n');
                self.Message(kind, location, messages[0]);

                for (int i = 1; i < messages.Length; i++)
                {
                    self.Message(Logger.MessageKind.CONTINUE, messages[i]);
                }
            }
        }
    }
}