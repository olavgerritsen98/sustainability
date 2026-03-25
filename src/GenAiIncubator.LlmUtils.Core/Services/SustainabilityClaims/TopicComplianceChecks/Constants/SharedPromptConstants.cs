namespace GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks.Constants;

public static class SharedPromptConstants
{
    public const string CopyHandboekRules = @"
1. STRICT OUTPUT RULE for 'duurzaam' (sustainable) in Suggestions:
   - In your 'SuggestedAlternative', you are FORBIDDEN from using vague terms like 'duurzaam', 'groen', or 'milieuvriendelijk' as standalone adjectives.
   - You MUST immediately follow such words with a factual explanation using 'omdat' (because) or 'doordat' (due to).
   - Example ALLOWED: ""...een duurzame keuze, omdat u hiermee directe CO2-uitstoot vermijdt.""
   - Example FORBIDDEN: ""...een duurzame keuze voor uw bedrijf.""
   - If no facts are available to support the explanation, remove the vague word entirely.

2. CONTEXT RULE for Energy Labels (Stroometiket):
   - Distinguish between 'General Technology' (e.g., E-boilers, Electrification potential) and 'Energy Supply Contracts'.
   - IF the text discusses technology in general: Do NOT flag a missing 'Stroometiket' link as an error.
   - IF the text implies supplying specific green electricity from Vattenfall: A link to the 'Stroometiket' IS required.

3. EVIDENCE RULE for Ambitions (Future Goals):
   - A reference to an external 'Pact', 'Agreement', or 'Vision' (e.g., Warmtepact) is NOT sufficient proof for a hard claim like ""CO2-neutral by 2040"".
   - COMPLIANCE CHECK: Mark as 'Non-Compliant' if there is no link to a concrete Vattenfall Action Plan or Roadmap describing *how* the goal will be reached.

4. COMPLETENESS & REPORTING RULE:
   - You MUST output ALL claims you have analyzed, regardless of their compliance status.
   - Do NOT skip or filter out a claim just because it is well-substantiated and compliant.
   - If a claim is correct, include it in your output with 'IsCompliant': true, an empty 'Violations' list, and leave the 'SuggestedAlternative' empty or mark it as 'None needed'.

5. COPYWRITING & FORMATTING (CRITICAL):
   - ACTIONABLE COPY: The 'SuggestedAlternative' MUST be ready-to-publish, customer-facing text. Do NOT write instructions for the user. Write the actual corrected sentence yourself. Use placeholders like [Link naar stroometiket] only if a specific URL is missing.
   - SPACING: You MUST format all text using strictly plain text. You MUST NOT use <br> or any HTML tags for line breaks or formatting. To create paragraphs or line breaks within the JSON string, you MUST use the exact escaped string literal \n (a backslash followed by the letter n).

6. SPECIFICITY RULE for Reasoning and Warnings:
   - Your 'Reasoning' and 'Warning' fields MUST be specific to the exact claim being evaluated. Do NOT write generic rule descriptions.
   - You MUST reference the actual words or phrases from the claim text that triggered the violation.
   - State exactly: (a) which word or phrase in the claim triggered the issue, (b) what specific information is missing, and (c) what the claim should have said instead.
   - Example FORBIDDEN: ""The claim does not clarify the benefit scope.""
   - Example ALLOWED: ""The phrase 'wij leveren 100% groene stroom' implies a company-wide benefit but does not specify whether this applies to all customer segments or only a specific product. The claim should name the applicable scope, e.g. 'voor klanten thuis en mkb' or reference the specific product name.""
   - Example FORBIDDEN: ""The claim is missing an explanation of the energy label.""
   - Example ALLOWED: ""The claim references 'ons stroometiket' but does not explain what the label shows (e.g. energy sources and their origin) nor mention the year it covers (e.g. 2024). Both are required.""
";

    // public const string CopyHandboekRules = "";
}
