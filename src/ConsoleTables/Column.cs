using System;

namespace ConsoleTables
{
    public class Column : Colorable
    {
        public object Value { get; set; }

        public Column(object value, ConsoleColor? fg = null, ConsoleColor? bg = null) : base(fg, bg)
        {
            Value = value;
        }
    }
}