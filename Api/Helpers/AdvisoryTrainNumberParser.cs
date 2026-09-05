using System.Globalization;
using System.Text.RegularExpressions;

namespace Api.Helpers;

//Basically gets the train number from the advisory messages
public static partial class AdvisoryTrainNumberParser
{
    [GeneratedRegex(@"\btrains?\s*#?\s*(\d{2,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex LabelledTrainRegex();
    
    [GeneratedRegex(@"#\s*(\d{2,4})\b",
        RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex HashedNumberRegex();
    
    public static IReadOnlySet<int> Parse(params string?[] texts)
    {
        var numbers = new HashSet<int>();

        foreach (var text in texts)
        {
            if (string.IsNullOrWhiteSpace(text))
                continue;

            Collect(LabelledTrainRegex(), text, numbers);
            Collect(HashedNumberRegex(), text, numbers);
        }

        return numbers;
    }
    
    public static bool Mentions(int trainNumber, params string?[] texts) =>
        Parse(texts).Contains(trainNumber);

    private static void Collect(Regex pattern, string text, HashSet<int> numbers)
    {
        foreach (Match match in pattern.Matches(text))
        {
            if (int.TryParse(match.Groups[1].ValueSpan, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var number))
            {
                numbers.Add(number);
            }
        }
    }
}
