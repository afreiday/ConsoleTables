using System;

namespace ConsoleTables
{
    public abstract class Colorable
    {
        public ConsoleColor? ForegroundColor { get; set; }
        public ConsoleColor? BackgroundColor { get; set; }

        public Colorable(ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            ForegroundColor = fg;
            BackgroundColor = bg;
        }
    }
}