# Sustainability Claims Compliance Assessment Framework
**Domain Expert Technical Report**

## Executive Summary

This document provides a comprehensive overview of the automated sustainability claims compliance assessment system currently implemented. The framework evaluates sustainability claims against regulatory requirements derived from the Dutch Consumer Claims Monitor (ACM) guidelines and Consumer Data Regulation (CDR), specifically designed for domain experts familiar with sustainability claims evaluation methodologies.

## Claim Classification Taxonomy

The system classifies sustainability claims into four distinct categories, each with specific compliance requirements:

### 1. Regular Claims (`SustainabilityClaimType.Regular`)
**Definition**: Direct statements about the current environmental or sustainability characteristics of a product, service, activity, or company.

**Typical Examples**:
- "This packaging is 100% recyclable"
- "Made from renewable energy"
- "Carbon-neutral production process"

**Applicable Requirements**: General requirements only (3 checks)

### 2. Ambition Claims (`SustainabilityClaimType.Ambition`)
**Definition**: Forward-looking statements about future sustainability goals, targets, or commitments.

**Typical Examples**:
- "We aim to be fully CO2 neutral by 2030"
- "Committed to 50% waste reduction by 2025"
- "Targeting 100% renewable energy by 2027"

**Applicable Requirements**: General requirements + 3 ambition-specific checks (6 total)

### 3. Comparison Claims (`SustainabilityClaimType.Comparison`)
**Definition**: Claims that compare the sustainability attributes of one product/service against another reference point.

**Typical Examples**:
- "30% less plastic than the previous packaging"
- "50% lower carbon footprint compared to industry average"
- "Uses 25% less water than competitor X"

**Applicable Requirements**: General requirements + 3 comparison-specific checks (6 total)

### 4. Label/Certification Claims (`SustainabilityClaimType.LabelOrCertification`)
**Definition**: Use of certificates, quality labels, logos, or third-party certifications that suggest sustainability credentials.

**Typical Examples**:
- FSC certification display
- Fairtrade logo usage
- ENERGY STAR certification
- EU Ecolabel presentation

**Applicable Requirements**: General requirements + 3 label-specific checks (6 total)

## Compliance Assessment Framework

### General Requirements (Applied to ALL Claim Types)

These three fundamental requirements form the baseline compliance assessment for every sustainability claim:

#### 1. Clear and Unambiguous (`General_ClearAndUnambiguous`)
- The claim is clear and unambiguous; it is immediately clear to the average consumer what the claim means.

#### 2. Factually Correct with Evidence (`General_FactuallyCorrectWithEvidence`)
- The claim is factually correct and supported by sufficient, up-to-date, and accessible evidence.

#### 3. Not Misleading (`General_NotMisleading`)
- The claim is not misleading; it does not give false impressions or hide important information.

### Type-Specific Requirements

#### Ambition-Specific Requirements

##### 1. Clearly Labeled as Ambition (`Ambition_ClearlyLabeledAsAmbition`)
- It is clearly stated that the claim is an ambition or future goal, not the current situation.

##### 2. Concrete Objective Verifiable Targets (`Ambition_ConcreteObjectiveVerifiableTargets`)
- The ambition is based on concrete, objective, and verifiable targets.

##### 3. Plans and Measures Present (`Ambition_PlansAndMeasuresPresent`)
- There are plans and measures described that support the feasibility of the ambition.

#### Comparison-Specific Requirements

##### 1. Reference Clearly Identified (`Comparison_ReferenceClearlyIdentified`)
- It is clear which product, service, or company the comparison is made with.

##### 2. Comparable and Complete Characteristics (`Comparison_ComparableAndCompleteCharacteristics`)
- The products/services are comparable and all relevant characteristics are included in the comparison.

##### 3. Current, Verifiable, Not Misleading (`Comparison_CurrentVerifiableNotMisleading`)
- The comparison is based on up-to-date data.

#### Label/Certification-Specific Requirements

##### 1. Independent and Reliable (`Label_IndependentAndReliable`)
- The label/certification is independent and reliable.

##### 2. Origin Clearly Stated (`Label_OriginClearlyStated`)
- The origin of the label/certification is clearly stated.

##### 3. No Confusing Self-Created Labels (`Label_NoConfusingSelfCreatedLabel`)
- There is no self-created label that could cause confusion with recognized labels.

## Assessment Methodology

### Automated Evaluation Process

1. **Claim Classification**: AI-powered categorization into one of the four claim types
2. **Requirement Mapping**: Automatic selection of applicable requirements based on claim type
3. **Compliance Assessment**: Systematic evaluation against each applicable requirement
4. **Violation Detection**: Identification of specific non-compliance issues
5. **Improvement Recommendations**: Generation of compliant alternative suggestions

### Evaluation Outputs

For each assessed claim, the system provides:
- **Compliance Status**: Overall compliance determination
- **Violation Details**: Specific requirement violations with explanatory messages
- **Suggested Alternatives**: Improved claim versions addressing identified issues
- **Requirement Coverage**: Complete assessment against all applicable requirements