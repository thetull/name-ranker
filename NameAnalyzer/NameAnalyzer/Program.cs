using System;
using System.Collections.Generic;
using System.IO;

namespace NameAnalyzer
{
    class Program
    {
        public const string NAME_DATA_FOLDER = @"M:\temp\names";
        public const string OUTPUT_DATA = @"M:\temp\nameScores.csv";
        static void Main(string[] args)
        {
            Dictionary<string, NameData> database = new Dictionary<string, NameData>();
            foreach (string filePath in Directory.EnumerateFiles(NAME_DATA_FOLDER))
            {
                int year = NameData.ParseYear(filePath);
                foreach (string nameLines in File.ReadAllLines(filePath))
                {
                    string[] splitLines = nameLines.Split(',');
                    string name = splitLines[0];
                    char gender = splitLines[1][0];
                    int instances = int.Parse(splitLines[2]);
                    if (!database.ContainsKey(name))
                    {
                        database[name] = new NameData(name);
                    }
                    database[name].AccumulateNameData(year, gender, instances);
                }
            }

            List<string> output = new List<string> {NameData.GetHeaders()};
            foreach (NameData data in database.Values)
            {
                output.Add(data.GetOutput());
            }
            File.WriteAllLines(OUTPUT_DATA, output);
        }
    }
}
