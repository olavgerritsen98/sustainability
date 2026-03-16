namespace GenAiIncubator.LlmUtils.Core.Enums;

/// <summary>
/// Enum representing the names of various plugins.
/// </summary>
public enum PluginNamesEnum
{
    /// <summary>
    /// Plugin for removing Personally Identifiable Information (PII).
    /// </summary>
    PIIRemoval,

    /// <summary>
    /// Plugin for classifying data.
    /// </summary>
    Classification,

    /// <summary>
    /// Plugin for summarizing conversations.
    /// </summary>
    ConversationSummarisation,
    
    /// <summary>
    /// Plugin for extracting the information concerning the last story from a conversation.
    /// </summary>
    ConversationLastStoryExtractor,

    /// <summary>
    /// Plugin to create user stories from input requirements.
    /// </summary>
    UserStoryCreation,
}