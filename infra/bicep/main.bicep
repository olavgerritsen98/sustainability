@description('Function app name base')
param functionAppName string

@description('Environment DTAP')
@allowed([
  'dev'
  'tst'
  'acc'
  'prd'
])
param environmentDtap string 

@description('App service plan name')
param appServicePlanName string

@description('The Azure region for deployment.')
param location string = resourceGroup().location

@description('The storage SKU.')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_RAGRS'
])
param storageSku string = 'Standard_LRS'

@description('Name of the keyvault resource')
param keyVaultName string

var storageAccountName = 'llmutilsfunctions${environmentDtap}'

// Existing resources
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}


// New resources
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: storageSku
  }
  kind: 'StorageV2'
  tags: {
    policyexception_safw: 'FunctionApp'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: functionAppName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    size: 'Y1'
  }
  kind: 'functionapp'
}

resource functionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    clientAffinityEnabled: true
    siteConfig: {
      cors: {
        allowedOrigins: [
          '*'
        ]
        supportCredentials: false
      }
      use32BitWorkerProcess: false
      alwaysOn: false
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  parent: storageAccount
  name: 'default'
}

resource sustainabilityEmailInputContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-09-01' = {
  parent: blobService
  name: 'sustainability-email-input'
  properties: {
    publicAccess: 'None'
  }
}

// TODO: check if this can be done using a resource ref instead of using listKeys
var blobStorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'

resource functionAppSettings 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: functionApp
  name: 'appsettings'
  properties: {
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
    AzureWebJobsStorage: blobStorageConnectionString
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
    KernelOptions__DeploymentName: 'genaiinc-gpt-4.1'
    KernelOptions__AzureOpenAIApiKey: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/AzureOpenAI--ApiKey/)'
    KernelOptions__Endpoint: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/AzureOpenAI--Endpoint/)'
    LIBRECHAT_STORAGE_CONNECTION: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/LibreChat--StorageConnection/)'
    LIBRECHAT_SHARE_NAME: 'librechat-config'
    GraphOptions__TenantId: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/GraphOptions--TenantId/)'
    GraphOptions__ClientId: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/GraphOptions--ClientId/)'
    GraphOptions__ClientSecret: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/GraphOptions--ClientSecret/)'
  }
  dependsOn: [
    kvAccessPolicy
  ]
}

resource kvAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: functionApp.identity.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}
