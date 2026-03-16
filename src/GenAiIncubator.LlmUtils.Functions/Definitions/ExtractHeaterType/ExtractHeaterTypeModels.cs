using GenAiIncubator.LlmUtils.Core.Enums;

namespace GenAiIncubator.LlmUtils_Functions.Definitions.ExtractHeaterType;

public class ExtractHeaterTypeRequest
{
}

public class ExtractHeaterTypeResponse
{
    public HeaterTypesEnum HeaterType { get; set; } = HeaterTypesEnum.Unknown;
    public List<HeaterTypesEnum> HeaterTypeAlt { get; set; } = [];
    public string Reason { get; set; } = string.Empty;
}

