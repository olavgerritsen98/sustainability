using System.Text.Json;

namespace GenAiIncubator.LlmUtils.Tests.SustainabilityClaimsTests;

public static class SustainabilityClaimsTestData
{
    private const string DatasetRelativePath = "./static/sustainability_claims/labeled_dataset/sustainability_claims_labeled_dataset.json";

    private static readonly Lazy<Dictionary<string, List<SustainabilityClaimTestItem>>> ItemsByUrlLazy =
        new(LoadAndGroupByUrl);

    private static Dictionary<string, List<SustainabilityClaimTestItem>> LoadAndGroupByUrl()
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, DatasetRelativePath);
        var json = File.ReadAllText(fullPath);
        var items = JsonSerializer.Deserialize<List<SustainabilityClaimTestItem>>(json) ?? new List<SustainabilityClaimTestItem>();

        return items
            .Where(i => i.IsClaim)
            .GroupBy(i => i.SourceUrl)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public static IEnumerable<object[]> AllUrls()
    {
        foreach (var kvp in ItemsByUrlLazy.Value)
            yield return new object[] { kvp.Key };
    }

    public static IReadOnlyList<SustainabilityClaimTestItem> GetByUrl(string url)
    {
        return ItemsByUrlLazy.Value[url];
    }
}
