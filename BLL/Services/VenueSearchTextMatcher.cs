using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BLL.Services;

/// <summary>Matches venue names, menu terms and catalog tags without relying on category codes.</summary>
public static class VenueSearchTextMatcher
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "tim", "kiem", "goi", "y", "cho", "toi", "minh", "muon", "can", "mot", "vai",
        "quan", "nha", "hang", "dia", "diem", "co", "de", "voi", "va", "o", "tai",
        "gan", "day", "quanh", "khu", "vuc", "nao", "giup", "duoc", "khong",
        "an", "uong", "buoi", "sang", "trua", "gia", "re", "duoi", "tren",
        "yen", "tinh", "hoc", "bai", "lam", "viec", "deadline", "am", "cung",
        "chill", "ngoai", "troi", "view", "dep", "wifi", "nhom", "ban",
        "phu", "hop", "nhu", "cau", "trong", "vong", "km", "tam", "khoang",
        "bao", "nhieu", "muc", "dien", "cam", "sac", "pin", "may", "lanh",
        "dieu", "hoa"
    };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var decomposed = value.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var letters = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                letters.Append(character);
        }

        var normalized = letters.ToString().Normalize(NormalizationForm.FormC);
        normalized = Regex.Replace(normalized, @"\b(?:cafe|coffee|caphe)\b", "ca phe");
        normalized = Regex.Replace(normalized, @"\bhotpot\b", "lau");
        normalized = Regex.Replace(normalized, @"\b(?:milktea|milk tea)\b", "tra sua");
        return Regex.Replace(normalized, @"[^\p{L}\p{N}]+", " ").Trim();
    }

    public static string[] GetTerms(string? prompt) => Normalize(prompt)
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Where(word => !StopWords.Contains(word) && !Regex.IsMatch(word, @"^\d+(?:k|vnd|d)?$"))
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    public static bool Matches(IReadOnlyList<string> terms, params string?[] fields)
    {
        if (terms.Count == 0) return false;

        var words = Normalize(string.Join(' ', fields))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
        return terms.All(words.Contains);
    }
}
