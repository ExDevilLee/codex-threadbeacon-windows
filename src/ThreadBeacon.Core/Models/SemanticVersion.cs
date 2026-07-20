using System.Globalization;

namespace ThreadBeacon.Core.Models;

public readonly record struct SemanticVersion(
    int Major,
    int Minor,
    int Patch,
    string? Prerelease = null) : IComparable<SemanticVersion>
{
    public static bool TryParse(string? value, out SemanticVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim();
        if (normalized.StartsWith('v'))
        {
            normalized = normalized[1..];
        }

        string[] buildParts = normalized.Split('+', 2);
        string[] prereleaseParts = buildParts[0].Split('-', 2);
        string[] numbers = prereleaseParts[0].Split('.');
        if (numbers.Length != 3
            || !TryNumber(numbers[0], out int major)
            || !TryNumber(numbers[1], out int minor)
            || !TryNumber(numbers[2], out int patch))
        {
            return false;
        }

        string? prerelease = prereleaseParts.Length == 2 ? prereleaseParts[1] : null;
        if (prerelease is not null
            && (prerelease.Length == 0
                || prerelease.Split('.').Any(identifier => identifier.Length == 0)))
        {
            return false;
        }

        version = new SemanticVersion(major, minor, patch, prerelease);
        return true;
    }

    public int CompareTo(SemanticVersion other)
    {
        int numeric = Major.CompareTo(other.Major);
        if (numeric == 0) numeric = Minor.CompareTo(other.Minor);
        if (numeric == 0) numeric = Patch.CompareTo(other.Patch);
        if (numeric != 0) return numeric;
        if (Prerelease is null) return other.Prerelease is null ? 0 : 1;
        if (other.Prerelease is null) return -1;

        string[] left = Prerelease.Split('.');
        string[] right = other.Prerelease.Split('.');
        for (int index = 0; index < Math.Max(left.Length, right.Length); index++)
        {
            if (index >= left.Length) return -1;
            if (index >= right.Length) return 1;
            bool leftNumeric = int.TryParse(left[index], NumberStyles.None, CultureInfo.InvariantCulture, out int leftNumber);
            bool rightNumeric = int.TryParse(right[index], NumberStyles.None, CultureInfo.InvariantCulture, out int rightNumber);
            int result = leftNumeric && rightNumeric
                ? leftNumber.CompareTo(rightNumber)
                : leftNumeric ? -1
                : rightNumeric ? 1
                : string.Compare(left[index], right[index], StringComparison.Ordinal);
            if (result != 0) return result;
        }

        return 0;
    }

    public static bool operator <(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) < 0;

    public static bool operator >(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) > 0;

    public override string ToString() =>
        $"{Major}.{Minor}.{Patch}{(Prerelease is null ? string.Empty : $"-{Prerelease}")}";

    private static bool TryNumber(string value, out int number) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number)
        && number >= 0
        && (value == "0" || !value.StartsWith('0'));
}
