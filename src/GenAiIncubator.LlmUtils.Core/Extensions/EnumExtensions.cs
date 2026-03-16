using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace GenAiIncubator.LlmUtils.Core.Extensions;

/// <summary>
/// Provides extension methods for working with enumeration types.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Retrieves the description of an enumeration value. If the enumeration value
    /// is decorated with a <see cref="DescriptionAttribute"/>, the description from
    /// the attribute is returned. Otherwise, the string representation of the value
    /// is returned.
    /// </summary>
    /// <param name="value">The enumeration value.</param>
    /// <returns>
    /// The description of the enumeration value if a <see cref="DescriptionAttribute"/>
    /// is present; otherwise, the string representation of the enumeration value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided enumeration value is null.
    /// </exception>
    public static string GetDescription(this Enum value)
    {
        var fi = value.GetType()
                      .GetField(value.ToString())!;
        var attr = fi.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? value.ToString();
    }

    /// <summary>
    /// Generates a formatted string listing all values of the specified enumeration type
    /// along with their descriptions.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <returns>A formatted string listing all enumeration values and their descriptions.</returns>
    public static string GetFormattedDescriptionList<TEnum>() where TEnum : struct, Enum
    {
        StringBuilder sb = new();
        foreach (TEnum enumValue in Enum.GetValues<TEnum>())
        {
            string description = enumValue.GetDescription();
            if (string.IsNullOrEmpty(description))
                sb.AppendLine($"\t- {enumValue}");
            else
                sb.AppendLine($"\t- {enumValue}: {enumValue.GetDescription()}");
        }
        return sb.ToString();
    }
}