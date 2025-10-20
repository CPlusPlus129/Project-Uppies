using System;

namespace DialogueModule
{
    public static partial class EnumNameExtension
    {
        private static readonly string[] commandIDNames;
        private static readonly string[] columnNames;

        static EnumNameExtension() 
        {
            commandIDNames = Enum.GetNames(typeof(CommandID));
            columnNames = Enum.GetNames(typeof(ColumnName));
        }

        public static string ToStringFast(this CommandID value)
        {
            int i = (int)value;
            if (i >= 0 && i < commandIDNames.Length)
                return commandIDNames[i];
            return value.ToString(); // fallback
        }

        public static string ToStringFast(this ColumnName value)
        {
            int i = (int)value;
            if (i >= 0 && i < columnNames.Length)
                return columnNames[i];
            return value.ToString(); // fallback
        }
    }
}