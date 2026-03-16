# Sustainability Claims Compliance Testing

This folder contains test data and configuration for testing the sustainability claims compliance evaluation system.

## Structure

- `general_requirements/` - Test cases for general sustainability claims requirements
- `reports/` - Generated test reports (CSV and TSV format)

## Test Data Format

Each test folder should contain a `sustainability_claims_test_set.json` file with the following structure:

```json
[
  {
    "claimText": "The actual claim text to be tested",
    "shouldBeCompliant": true, 
    "requirementCode": "General_ClearAndUnambiguous"
  }
]
```

### Fields

- `claimText`: The sustainability claim text to evaluate
- `shouldBeCompliant`: Boolean indicating whether the claim should be compliant with the requirement (true = should pass, false = should violate)
- `requirementCode`: The specific RequirementCode enum value being tested (e.g., "General_ClearAndUnambiguous", "Ambition_ClearlyLabeledAsAmbition")

### Available RequirementCode Values

**General Requirements (apply to all claim types):**
- `General_ClearAndUnambiguous` - Is the claim clear and unambiguous?
- `General_FactuallyCorrectWithEvidence` - Is the claim factually correct, with sufficient evidence?
- `General_NotMisleading` - Is the claim not misleading?

**Ambition-Specific Requirements:**
- `Ambition_ClearlyLabeledAsAmbition` - Clearly stated as an ambition/future goal
- `Ambition_ConcreteObjectiveVerifiableTargets` - Based on concrete, objective targets
- `Ambition_PlansAndMeasuresPresent` - Plans and measures are described

**Comparison-Specific Requirements:**
- `Comparison_ReferenceClearlyIdentified` - The reference for comparison is clearly identified
- `Comparison_ComparableAndCompleteCharacteristics` - Products/services are comparable
- `Comparison_CurrentVerifiableNotMisleading` - The comparison is current and verifiable

**Label/Certification-Specific Requirements:**
- `Label_IndependentAndReliable` - The label/certification is independent and reliable
- `Label_OriginClearlyStated` - The origin of the label/certification is clearly stated
- `Label_NoConfusingSelfCreatedLabel` - No confusing self-created labels

## Running Tests

The test suite can be run using the `SustainabilityClaimsComplianceTests` class:

1. **Single Test**: `TestSingleClaimCompliance()` - Tests a single predefined claim
2. **Full Test Suite**: `TestSustainabilityClaimsCompliance()` - Runs all test cases and validates accuracy
3. **Generate Reports**: `CreateSustainabilityClaimsComplianceReport()` - Generates detailed CSV and TSV reports

## Generated Reports

The test suite generates two types of reports:

### Summary Report (`SustainabilityClaimsReport_Summary.tsv`)
- Overall statistics (total tests, accuracy, errors)
- Per-requirement breakdown with accuracy metrics

### Details Report (`SustainabilityClaimsReport_Details.tsv`)
- Individual test results with all details
- Expected vs actual results
- Violation codes and suggested alternatives
- Notes and error information

## Adding New Test Cases

1. Create the test data JSON file in the appropriate folder
2. Follow the JSON schema above
3. Add the folder path to the `testDataFolders` dictionary in `SustainabilityClaimsComplianceTests.cs`
4. Run the tests to generate reports

## Notes

- The test system focuses specifically on requirement compliance evaluation
- Claims are tested with a default `Regular` claim type 
- Rate limiting (30-second delays) is applied between test batches to avoid API limits
- Minimum acceptable accuracy is set to 75% (configurable via `AcceptableRatio`)
