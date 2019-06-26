using System;
using System.Text;

namespace Payload.Utilities
{
    public static class Logger
    {
        public static StringBuilder sbLog = new StringBuilder("");

        public static void Write(string message)
        {
            Console.ResetColor();
            Console.Write(message);
            sbLog.Append(message);
        }

        public static void Write(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
            sbLog.Append(message);
        }


        public static void WriteLine(string message)
        {
            Console.ResetColor();
            Console.WriteLine(message);
            sbLog.AppendLine(message);
        }
        public static void WriteLine(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            sbLog.AppendLine(message);
        }
        public static void WriteLine(string message, ConsoleColor color, string addtionalText)
        {
            Write(message, color);
            WriteLine(addtionalText);
        }
    }
}
