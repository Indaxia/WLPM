using System;

namespace wlpm
{

    public class ConsoleColorChanger
    {
        private static ConsoleColor Primary = ConsoleColor.Black;
        private static ConsoleColor Secondary = ConsoleColor.Blue;
        private static ConsoleColor Accent = ConsoleColor.Green;
        private static ConsoleColor Warning = ConsoleColor.DarkRed;
        
        public static void SetPrimary(ConsoleColor primary)
        {
            Primary = primary;
        }

        public static void SetAll(ConsoleColor primary, ConsoleColor secondary, ConsoleColor accent, ConsoleColor warning)
        {
            Primary = primary;
            Secondary = secondary;
            Accent = accent;
            Warning = warning;
        }

        public static void UsePrimary()
        {
            Console.ForegroundColor = Primary;
        }

        public static void UseSecondary()
        {
            Console.ForegroundColor = Secondary;
        }

        public static void UseAccent()
        {
            Console.ForegroundColor = Accent;
        }

        public static void UseWarning()
        {
            Console.ForegroundColor = Warning;
        }
    }
}