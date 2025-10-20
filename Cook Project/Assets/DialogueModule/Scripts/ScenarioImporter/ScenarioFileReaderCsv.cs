using System.IO;
using UnityEngine;

namespace DialogueModule
{
    public class ScenarioFileReaderCsv : IScenarioFileReader
    {
        private CsvParser csvParser = new CsvParser();

        public bool TryReadFile(string path, out StringGridDictionary stringGridDictionary)
        {
            stringGridDictionary = new StringGridDictionary();

            if (!File.Exists(path))
            {
                Debug.LogError($"CSV file not found: {path}");
                return false;
            }

            try
            {
                var csvData = csvParser.ReadFile(path);
                if (csvData == null || csvData.Count == 0)
                {
                    Debug.LogWarning($"Empty or invalid CSV file: {path}");
                    return false;
                }

                string sheetName = Path.GetFileNameWithoutExtension(path);
                var grid = new StringGrid(sheetName);

                foreach (var rowData in csvData)
                {
                    var row = new StringGridRow(rowData);
                    grid.AddRow(row);
                }

                stringGridDictionary.Add(sheetName, grid);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to read CSV file {path}: {e.Message}");
                return false;
            }
        }
    }
}
