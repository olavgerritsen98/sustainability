namespace GenAiIncubator.LlmUtils.Core.Models;

/// <summary>
/// Represents a customer journey classification.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CustomerJourneyClassification"/> class.
/// </remarks>
/// <param name="id">The id of the category-subcategory pair.</param>
/// <param name="classification">The main topic of the customer journey classification.</param>
/// <param name="subcategory">The specific classification of the customer journey.</param>
public class CustomerJourneyClassification(int id, string classification, string subcategory)
{
    /// <summary>
    /// Gets or sets the identifier for the customer journey classification.
    /// </summary>
    public int Id { get; } = id;

    /// <summary>
    /// Gets or sets the main topic of the customer journey classification.
    /// </summary>
    public string Classification { get; set; } = classification;

    /// <summary>
    /// Gets or sets the specific classification of the customer journey.
    /// </summary>
    public string Subcategory { get; set; } = subcategory;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Classification: {Classification}, Subcategory: {Subcategory}";
    }
}

/// <summary>
/// Represents an invalid customer journey classification.
/// </summary>
public class InvalidClassification() : CustomerJourneyClassification(-1, "Invalid", "Invalid") { }