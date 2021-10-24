using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NameAnalyzer
{
    public class NameData
    {
        private static readonly List<int> availableYears = new List<int>();

        private readonly Dictionary<int, int> yearDictionary = new Dictionary<int, int>();

        private Dictionary<char, int> genderInstances = new Dictionary<char, int>();

        private int totalInstances = 0;
        private readonly string name;

        private const int OPTIMAL_NAME_LENGTH = 7;
        private const int OPTIMAL_NAME_LENGTH_WEIGHT = 6;
        private const int TARGET_TOTAL_USAGES = 10000;
        private const int OPTIMAL_TARGET_WEIGHT = 4;
        private const int AVERAGE_AGE_TARGET = 75;
        private const int AVERAGE_AGE_WEIGHT = 3;
        private const int TREND_0_PROPORTION = 5;
        private const int TREND_25_PROPORTION = 10;
        private const int TREND_50_PROPORTION = 70;
        private const int TREND_75_PROPORTION = 15;
        private const int TREND_0_WEIGHT = 1;
        private const int TREND_25_WEIGHT = 2;
        private const int TREND_50_WEIGHT = 3;
        private const int TREND_75_WEIGHT = 1;
        private const int FUTURE_TARGET_WEIGHT = 4;
        private const int FUTURE_YEARLY_USAGE_TARGET = 1000;
        private const double UNISEX_THRESHOLD = 0.8;



        static NameData()
        {
            foreach (string filePath in Directory.EnumerateFiles(Program.NAME_DATA_FOLDER))
            {
                availableYears.Add(ParseYear(filePath));
            }
            availableYears.Sort();
        }

        public NameData(string name)
        {
            this.name = name;
            yearDictionary = availableYears.ToDictionary(x => x, x => 0);
            genderInstances['M'] = 0;
            genderInstances['F'] = 0;
        }

        public void AccumulateNameData(int year, char gender, int instances)
        {
            yearDictionary[year] += instances;
            genderInstances[gender] += instances;
            totalInstances += instances;
        }


        private string GetGender()
        {
            double maleRatio = 1.0* genderInstances['M'] / totalInstances;
            if (maleRatio > UNISEX_THRESHOLD)
                return "male";
            return maleRatio > 1 - UNISEX_THRESHOLD ? "unisex" : "female";
        }

        private double AverageAgeOfName()
        {
            int currentYear = availableYears.Max();
            long totalValue = 0;
            foreach (KeyValuePair<int, int> keyValuePair in yearDictionary)
            {
                totalValue += (currentYear - keyValuePair.Key) * keyValuePair.Value;
            }

            return 1.0 * totalValue / totalInstances;
        }

        private double GetAverageInstancesInRange(int min, int max)
        {
            int total = 0;
            for (int i = min; i <= max; i++)
            {
                total += yearDictionary[i];
            }

            return 1.0 * total / max - min + 1;
        }

        private double GetCurrentSlope()
        {
            int currentYear = availableYears.Max();
            double oldAverage = GetAverageInstancesInRange(currentYear - 15, currentYear - 5);
            double newAverage = GetAverageInstancesInRange(currentYear - 5, currentYear);
            return (newAverage - oldAverage) / 7.5;
        }

        private double GetFutureYearlyInstances()
        {
            int currentYear = availableYears.Max();
            double oldAverage = GetAverageInstancesInRange(currentYear - 15, currentYear - 5);
            double newAverage = GetAverageInstancesInRange(currentYear - 5, currentYear);
            double slope = (newAverage - oldAverage) / 7.5;
            return currentYear + slope * 10;
        }

        private double GetPercentageOfUsagesInRange(double min, double max, double oldest)
        {
            int inTarget = 0;
            int inRange = 0;
            foreach (KeyValuePair<int, int> keyValuePair in yearDictionary)
            {
                if (keyValuePair.Key < max || keyValuePair.Key >= min)
                {
                    inTarget += keyValuePair.Value;
                }

                if (keyValuePair.Key >= oldest)
                {
                    inRange += keyValuePair.Value;
                }
            }

            return 1.0 * inTarget / inRange;
        }

        private double Get0Percent()
        {
            return GetPercentageOfUsagesInRange(availableYears.Max() - 25, availableYears.Max(), availableYears.Max() - 100);
        }
        private double Get25Percent()
        {
            return GetPercentageOfUsagesInRange(availableYears.Max() - 50, availableYears.Max() - 25, availableYears.Max() - 100);
        }

        private double Get50Percent()
        {
            return GetPercentageOfUsagesInRange(availableYears.Max() - 75, availableYears.Max() - 50, availableYears.Max() - 100);
        }

        private double Get75Percent()
        {
            return GetPercentageOfUsagesInRange(availableYears.Max() - 100, availableYears.Max() - 75, availableYears.Max() - 100);
        }

        public double AlgorithmScore()
        {
            double score = 0d;

            double lengthScore = OPTIMAL_NAME_LENGTH_WEIGHT * ScoreBasedOnTarget(OPTIMAL_NAME_LENGTH/2d, OPTIMAL_NAME_LENGTH *1.25, OPTIMAL_NAME_LENGTH, name.Length);
            score += lengthScore;

            double usageScore = OPTIMAL_TARGET_WEIGHT * ScoreBasedOnTarget(TARGET_TOTAL_USAGES / 5d, TARGET_TOTAL_USAGES * 5, TARGET_TOTAL_USAGES, totalInstances);
            score += usageScore;

            double ageScore = AVERAGE_AGE_WEIGHT * ScoreBasedOnTarget(AVERAGE_AGE_TARGET / 5d, AVERAGE_AGE_TARGET * 10, AVERAGE_AGE_TARGET, AverageAgeOfName());
            score += ageScore;

            double futureScore = FUTURE_TARGET_WEIGHT * ScoreBasedOnTarget(FUTURE_YEARLY_USAGE_TARGET / 5d, FUTURE_YEARLY_USAGE_TARGET * 5, FUTURE_YEARLY_USAGE_TARGET, GetFutureYearlyInstances());
            score += futureScore;

            double distro0Score = TREND_0_WEIGHT * ScoreBasedOnTarget(0, 100, TREND_0_PROPORTION, Get0Percent());
            double distro25Score = TREND_25_WEIGHT * ScoreBasedOnTarget(0, 100, TREND_25_PROPORTION, Get25Percent());
            double distro50Score = TREND_50_WEIGHT * ScoreBasedOnTarget(0, 100, TREND_50_PROPORTION, Get50Percent());
            double distro75Score = TREND_75_WEIGHT * ScoreBasedOnTarget(0, 100, TREND_75_PROPORTION, Get75Percent());
            score += distro0Score + distro25Score + distro50Score + distro75Score;

            return score;
        }

        public string GetOutput()
        {
            return $"{name},{GetGender()},{totalInstances},{AlgorithmScore()}";
        }

        public static string GetHeaders()
        {
            return "name,gender,total_instances,score";
        }
        public static int ParseYear(string filePath)
        {
            return int.Parse(Path.GetFileName(filePath).Substring(3, 4));
        }

        private static double ScoreBasedOnTarget(double worstLowValue, double worstHighValue, double targetValue, double actualValue)
        {
            double output;
            double diff = actualValue - targetValue;
            double downsideRoot = targetValue - worstLowValue;
            double upsideRoot = worstHighValue - targetValue;
            if (targetValue - actualValue == 0)
                return 1;
            if (actualValue < worstLowValue || actualValue > worstHighValue)
                return 0;
            if (targetValue - actualValue > 0)
            {
                output = 1 - Math.Pow(diff, 2) / Math.Pow(downsideRoot, 2);
            }
            else
            {
                output = 1 - Math.Pow(diff, 2) / Math.Pow(upsideRoot, 2);
            }

            return Math.Max(output, 0);
        }
    }
}
