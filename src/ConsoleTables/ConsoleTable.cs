using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleTables
{
    public class ConsoleTable
    {
        public IList<Column> Columns { get; set; }
        public IList<Row> Rows { get; protected set; }

        public ConsoleTableOptions Options { get; protected set; }
        public Type[] ColumnTypes { get; private set; }

        public static HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(int),  typeof(double),  typeof(decimal),
            typeof(long), typeof(short),   typeof(sbyte),
            typeof(byte), typeof(ulong),   typeof(ushort),
            typeof(uint), typeof(float)
        };

        public ConsoleTable(params string[] columns)
            : this(new ConsoleTableOptions { Columns = columns.Select(c => new Column(c)) })
        {
        }

        public ConsoleTable(ConsoleTableOptions options)
        {
            Options = options ?? throw new ArgumentNullException("options");
            Rows = new List<Row>();
            Columns = new List<Column>(options.Columns);
        }

        public ConsoleTable AddColumn(params string[] names)
        {
            return AddColumn(null, null, names);
        }

        public ConsoleTable AddColumn(ConsoleColor? fg, ConsoleColor? bg, params string[] names)
        {
            (Columns as List<Column>).AddRange(names.Select(n => new Column(n, fg, bg)));
            return this;
        }

        public ConsoleTable AddRow(params object[] values)
        {
            return AddRow(null, null, values);
        }

        public ConsoleTable AddRow(ConsoleColor? fg, ConsoleColor? bg, params object[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (!Columns.Any())
                throw new Exception("Please set the columns first");

            if (Columns.Count != values.Length)
                throw new Exception(
                    $"The number columns in the row ({Columns.Count}) does not match the values ({values.Length}");

            Rows.Add(new Row(values, fg, bg));
            return this;
        }

        public ConsoleTable Configure(Action<ConsoleTableOptions> action)
        {
            action(Options);
            return this;
        }

        public static ConsoleTable From<T>(IEnumerable<T> values)
        {
            var table = new ConsoleTable
            {
                ColumnTypes = GetColumnsType<T>().ToArray()
            };

            var columns = GetColumns<T>();

            table.AddColumn(columns.ToArray());

            foreach (
                var propertyValues
                in values.Select(value => columns.Select(column => GetColumnValue<T>(value, column)))
            ) table.AddRow(propertyValues.ToArray());

            return table;
        }

        private void SetColors(ConsoleColor? fg, ConsoleColor? bg)
        {
            if (fg.HasValue)
                Console.ForegroundColor = fg.Value;
            if (bg.HasValue)
                Console.BackgroundColor = bg.Value;
        }

        private void SetColors(Colorable colorable)
        {
            SetColors(colorable.ForegroundColor, colorable.BackgroundColor);
        }

        public void WriteTable()
        {
            var currentFg = Console.ForegroundColor;
            var currentBg = Console.BackgroundColor;

            // find the longest column by searching each row
            var columnLengths = ColumnLengths();

            // set right alinment if is a number
            var columnAlignment = Enumerable.Range(0, Columns.Count)
                .Select(GetNumberAlignment)
                .ToList();

            var format = Enumerable.Range(0, Columns.Count)
                .Select(i => " | {" + i + "," + columnAlignment[i] + columnLengths[i] + "}")
                .Aggregate((s, a) => s + a) + " |";

            // find the longest formatted line
            var maxRowLength = Math.Max(0, Rows.Any() ? Rows.Select(r => r.Values).Max(row => string.Format(format, row).Length) : 0);
            var columnHeaders = string.Format(format, Columns.Select(c => c.Value).ToArray());

            // longest line is greater of formatted columnHeader and longest row
            var longestLine = Math.Max(maxRowLength, columnHeaders.Length);

            // create the divider
            var divider = " " + string.Join("", Enumerable.Repeat("-", longestLine - 1)) + " ";

            Console.WriteLine(divider);

            SetColors(currentFg, currentBg);
            Console.Write(" | ");
            for (int i = 0; i < Columns.Count(); i++)
            {
                var col = Columns[i];

                if (!String.IsNullOrWhiteSpace(col.Value.ToString()))
                    SetColors(col);
                Console.Write(String.Format("{0," + columnAlignment[i] + columnLengths[i] + "}", col.Value));

                SetColors(currentFg, currentBg);
                Console.Write(" | ");
            }
            Console.Write(Environment.NewLine);

            foreach (var row in Rows)
            {
                Console.WriteLine(divider);

                SetColors(currentFg, currentBg);
                Console.Write(" | ");

                for(int i = 0; i < row.Values.Count(); i++)
                {
                    var value = row.Values[i];

                    if (!String.IsNullOrWhiteSpace(value.ToString()))
                    {
                        SetColors(Columns[i]);
                        SetColors(row);
                    }
                    Console.Write(String.Format("{0," + columnAlignment[i] + columnLengths[i] + "}", value));

                    SetColors(currentFg, currentBg);
                    Console.Write(" | ");
                }
                Console.Write(Environment.NewLine);
            }

            Console.WriteLine(divider);

            if (Options.EnableCount)
            {
                Console.WriteLine("");
                Console.WriteLine(" Count: {0}", Rows.Count);
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            // find the longest column by searching each row
            var columnLengths = ColumnLengths();

            // set right alinment if is a number
            var columnAlignment = Enumerable.Range(0, Columns.Count)
                .Select(GetNumberAlignment)
                .ToList();

            // create the string format with padding
            var format = Enumerable.Range(0, Columns.Count)
                .Select(i => " | {" + i + "," + columnAlignment[i] + columnLengths[i] + "}")
                .Aggregate((s, a) => s + a) + " |";

            // find the longest formatted line
            var maxRowLength = Math.Max(0, Rows.Any() ? Rows.Select(r => r.Values).Max(row => string.Format(format, row).Length) : 0);
            var columnHeaders = string.Format(format, Columns.Select(c => c.Value).ToArray());

            // longest line is greater of formatted columnHeader and longest row
            var longestLine = Math.Max(maxRowLength, columnHeaders.Length);

            // create the divider
            var divider = " " + string.Join("", Enumerable.Repeat("-", longestLine - 1)) + " ";

            builder.AppendLine(divider);
            builder.AppendLine(columnHeaders);

            foreach (var row in Rows)
            {
                builder.AppendLine(divider);
                builder.AppendLine(string.Format(format, row.Values));
            }

            builder.AppendLine(divider);

            if (Options.EnableCount)
            {
                builder.AppendLine("");
                builder.AppendFormat(" Count: {0}", Rows.Count);
            }

            return builder.ToString();
        }

        public string ToMarkDownString()
        {
            return ToMarkDownString('|');
        }

        private string ToMarkDownString(char delimiter)
        {
            var builder = new StringBuilder();

            // find the longest column by searching each row
            var columnLengths = ColumnLengths();

            // create the string format with padding
            var format = Format(columnLengths, delimiter);

            // find the longest formatted line
            var columnHeaders = string.Format(format, Columns.Select(c => c.Value).ToArray());

            // add each row
            var results = Rows.Select(row => string.Format(format, row.Values)).ToList();

            // create the divider
            var divider = Regex.Replace(columnHeaders, @"[^|]", "-");

            builder.AppendLine(columnHeaders);
            builder.AppendLine(divider);
            results.ForEach(row => builder.AppendLine(row));

            return builder.ToString();
        }

        public string ToMinimalString()
        {
            return ToMarkDownString(char.MinValue);
        }

        public string ToStringAlternative()
        {
            var builder = new StringBuilder();

            // find the longest column by searching each row
            var columnLengths = ColumnLengths();

            // create the string format with padding
            var format = Format(columnLengths);

            // find the longest formatted line
            var columnHeaders = string.Format(format, Columns.Select(c => c.Value).ToArray());

            // create the divider
            var divider = Regex.Replace(columnHeaders, @"[^|]", "-");
            var dividerPlus = divider.Replace("|", "+");

            builder.AppendLine(dividerPlus);
            builder.AppendLine(columnHeaders);

            foreach (var row in Rows)
            {
                builder.AppendLine(dividerPlus);
                builder.AppendLine(string.Format(format, row.Values));
            }
            builder.AppendLine(dividerPlus);

            return builder.ToString();
        }

        private string Format(List<int> columnLengths, char delimiter = '|')
        {
            // set right alinment if is a number
            var columnAlignment = Enumerable.Range(0, Columns.Count)
                .Select(GetNumberAlignment)
                .ToList();

            var delimiterStr = delimiter == char.MinValue ? string.Empty : delimiter.ToString();
            var format = (Enumerable.Range(0, Columns.Count)
                .Select(i => " " + delimiterStr + " {" + i + "," + columnAlignment[i] + columnLengths[i] + "}")
                .Aggregate((s, a) => s + a) + " " + delimiterStr).Trim();
            return format;
        }

        private string GetNumberAlignment(int i)
        {
            return Options.NumberAlignment == Alignment.Right
                    && ColumnTypes != null
                    && NumericTypes.Contains(ColumnTypes[i])
                ? ""
                : "-";
        }

        private List<int> ColumnLengths()
        {
            var columnLengths = Columns
                .Select((t, i) => Rows.Select(x => x.Values[i])
                    .Union(new[] { Columns[i].Value })
                    .Where(x => x != null)
                    .Select(x => x.ToString().Length).Max())
                .ToList();
            return columnLengths;
        }

        public void Write(Format format = ConsoleTables.Format.Default)
        {
            switch (format)
            {
                case ConsoleTables.Format.Default:
                    WriteTable();
                    break;
                case ConsoleTables.Format.MarkDown:
                    Console.WriteLine(ToMarkDownString());
                    break;
                case ConsoleTables.Format.Alternative:
                    Console.WriteLine(ToStringAlternative());
                    break;
                case ConsoleTables.Format.Minimal:
                    Console.WriteLine(ToMinimalString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private static IEnumerable<string> GetColumns<T>()
        {
            return typeof(T).GetProperties().Select(x => x.Name).ToArray();
        }

        private static object GetColumnValue<T>(object target, string column)
        {
            return typeof(T).GetProperty(column).GetValue(target, null);
        }

        private static IEnumerable<Type> GetColumnsType<T>()
        {
            return typeof(T).GetProperties().Select(x => x.PropertyType).ToArray();
        }
    }

    public class ConsoleTableOptions
    {
        public IEnumerable<Column> Columns { get; set; } = new List<Column>();
        public bool EnableCount { get; set; } = true;

        /// <summary>
        /// Enable only from a list of objects
        /// </summary>
        public Alignment NumberAlignment { get; set; } = Alignment.Left;
    }

    public enum Format
    {
        Default = 0,
        MarkDown = 1,
        Alternative = 2,
        Minimal = 3
    }

    public enum Alignment
    {
        Left,
        Right
    }
}
