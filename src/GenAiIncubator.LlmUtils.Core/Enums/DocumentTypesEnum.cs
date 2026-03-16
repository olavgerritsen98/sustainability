using System.ComponentModel;
using System.Text.Json.Serialization;

namespace GenAiIncubator.LlmUtils.Core.Enums;

/// <summary>
/// Represents the different types of documents that can be classified.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentTypesEnum
{
    /// <summary>
    /// Represents an identification document.
    /// </summary>
    [Description("ID card.")]
    ID,

    /// <summary>
    /// Represents a driver's license document.
    /// </summary>
    [Description("Driver's license.")]
    DriversLicense,

    /// <summary>
    /// Represents a passport document.
    /// </summary>
    [Description("Passport.")]
    Passport,

    /// <summary>
    /// Represents a photo or image with meter readings.
    /// </summary>
    // [Description("Photo or image with meter readings.")]
    // MeterReadings,

    /// <summary>
    /// Represents a meter exchange voucher.
    /// </summary>
    // [Description("Meter exchange voucher.")]
    // MeterExchangeVoucher,

    /// <summary>
    /// Represents a delivery report.
    /// </summary>
    // [Description("Delivery report.")]
    // DeliveryReport,

    /// <summary>
    /// Represents an authorization form.
    /// </summary>
    // [Description("Authorization form.")]
    // AuthorizationForm, 

    /// <summary>
    /// Represents a complaint.
    /// </summary>
    // [Description("Complaint.")]
    // Complaint,

    /// <summary>
    /// Represents a letter.
    /// </summary>
    // [Description("Letter.")]
    // Letter,

    /// <summary>
    /// Represents a financial assistance power of attorney or order.
    /// </summary>
    [Description("Dept or Financial assistance power of attorney or order.")]
    DebtOrFinancialAssistance,

    /// <summary>
    /// Represents judicial documents (e.g., information request authority, declaration of health).
    /// </summary>
    // [Description("Judicial documents (e.g., information request authority, declaration of health).")]
    // JudicialDocument,

    /// <summary>
    /// Represents other Service Desk Survivors (SDN) documents.
    /// </summary>
    // [Description("Service Desk Survivors (SDN) documents.")]
    // SDN,

    
    /// <summary>
    /// Represents an unknown document type.
    /// </summary>
    Unknown
}