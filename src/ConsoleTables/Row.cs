using System;

namespace ConsoleTables
{
    public class Row : Colorable
    {
        public object[] Values { get; set; }

        public Row(object[] values, ConsoleColor? fg = null, ConsoleColor? bg = null) : base(fg, bg)
        {
            Values = values;
        }
    }
}