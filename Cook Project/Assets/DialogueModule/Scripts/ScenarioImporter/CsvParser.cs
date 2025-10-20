using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DialogueModule
{
    public class CsvParser
    {
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public char Delimiter { get; set; } = ',';
        const char DoubleQuotes = '"';

        public List<List<string>> ReadFile(string path)
        {
            var sheetName = Path.GetFileNameWithoutExtension(path);
            var result = new List<List<string>>();
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding);
            int lineNo = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null) continue;
                try
                {
                    var row = ReadLine(line);
                    result.Add(row);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Invalid CSV format. Line:{lineNo} {e.Message}");
                    return null;
                }

                ++lineNo;
            }

            return result;
        }

        public List<string> ReadLine(string line)
        {
            var result = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == DoubleQuotes)
                    {
                        // Check if next character is also a quote (escaped quote)
                        if (i + 1 < line.Length && line[i + 1] == DoubleQuotes)
                        {
                            currentField.Append(DoubleQuotes);
                            i++; // Skip the next quote
                        }
                        else
                        {
                            // End of quoted field
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
                else
                {
                    if (c == DoubleQuotes)
                    {
                        inQuotes = true;
                    }
                    else if (c == Delimiter)
                    {
                        result.Add(currentField.ToString());
                        currentField.Clear();
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
            }

            // Add the last field
            result.Add(currentField.ToString());

            // Check for unclosed quotes
            if (inQuotes)
            {
                throw new Exception("Unclosed quotes in CSV line");
            }

            return result;
        }
    }
}