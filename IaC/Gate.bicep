@description('Name of the Service Bus namespace')
param serviceBusNamespaceName string = '${appShortName}${uniqueString(resourceGroup().id)}'

@description('Name of the Processing Queue')
@allowed([ 'issuesProcessing','deployHoursProcessing' ])
param serviceBusQueueName string

@description('Service Bus SKU')
@allowed([ 'Basic', 'Standard', 'Premium' ])
param serviceBusSku string = 'Basic'

@description('The name of the function app to run the gate. You probably want to override this to have a predictable name for the webhook. needs to be globally unique within azure.')
param appName string = '${appShortName}${uniqueString(resourceGroup().id)}'

@description('App short name (used for prefixing resources')
@maxLength(6)
param appShortName string

@description('Storage Account type')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_RAGRS'
])
param appStorageAccountType string = 'Standard_LRS'

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Location for Application Insights')
param appInsightsLocation string = resourceGroup().location

@description('Application ID')
param GHApplicationId string

@description('The name of the key vault to be created.')
param vaultName string = '${appShortName}${uniqueString(resourceGroup().id)}'

@description('The SKU of the vault to be created.')
@allowed(['standard','premium'])
param vaultSKU string = 'standard'

@secure()
@description('The PEM certificate for the GitHub App. Pass as a file because of multiline string.')
param certificate string 

@secure()
@description('The webhook secret for the GitHub App')
param webHookSecret string = ''

//////////////////////////////////////// Service Bus

// You can use the same service bus for all gates.
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  sku: {
    name: serviceBusSku
  }
  properties: {
    disableLocalAuth: true // Since we use managed identity, we don't need local auth.    
  }
}

// Each gate requires it's own queue.
resource serviceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2022-01-01-preview' = {
  parent: serviceBusNamespace
  name: serviceBusQueueName
  properties: {
    lockDuration: 'PT5M'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    requiresSession: false  
    deadLetteringOnMessageExpiration: false
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    maxDeliveryCount: 10    
    enablePartitioning: false
    enableExpress: false    
  }
}


// Roles as defined in https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference?tabs=servicebus#common-properties-for-identity-based-connections
var roleDefinitionIDServiceBusDataSender = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
var roleDefinitionIDServiceBusDataReceiver = '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
var roleDefinitionIDServiceBusDataOwner = '090c5cfd-751d-490a-894a-3ce6f1109419'

resource roleAssignmentDataSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusQueue.id, functionApp.name, 'sender')
  scope: serviceBusQueue
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions/', roleDefinitionIDServiceBusDataSender)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    description: 'Service Bus Data Sender'
  }
}

resource roleAssignmentDataListener 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusQueue.id, functionApp.name, 'listener')
  scope: serviceBusQueue
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions/', roleDefinitionIDServiceBusDataReceiver)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    description: 'Service Bus Data Listener'
  }
}

resource roleAssignmentDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusQueue.id, functionApp.name, 'owner')
  scope: serviceBusQueue
  properties: {
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions/', roleDefinitionIDServiceBusDataOwner)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
    description: 'Service Bus Data Listener'
  }
}


//////////////////////////////////////// FUNCTION APP

var functionAppName = appName
var hostingPlanName = appName
var applicationInsightsName = appName
var storageAccountName = toLower('${appShortName}${uniqueString(resourceGroup().id)}')
var functionWorkerRuntime = 'dotnet'

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: appStorageAccountType
  }
  kind: 'Storage'
}

resource hostingPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    size: 'Y1'
    family: 'Y'
  }
}

resource functionApp 'Microsoft.Web/sites@2021-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

// More info how to reference keyvault secrets: https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references
// Managed identities in service bus https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-managed-service-identity
// SB triggers with managed identities https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-service-bus-trigger?tabs=python-v2%2Cin-process%2Cextensionv5&pivots=programming-language-csharp#identity-based-connections
var serviceBusHost = split(uri(serviceBusNamespace.properties.serviceBusEndpoint,''), '/')[2]
resource functionAppSettings 'Microsoft.Web/sites/config@2022-03-01' = {
  name: 'appsettings'
  parent: functionApp
  properties: {
      // AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
      // WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
      AzureWebJobsStorage: '@Microsoft.KeyVault(SecretUri=${KeyFunctionStorageConnectionString.properties.secretUri})'
      AzureWebJobsDisableHomepage: 'true'
      WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: '@Microsoft.KeyVault(SecretUri=${KeyFunctionStorageConnectionString.properties.secretUri})'
      WEBSITE_CONTENTSHARE: toLower(functionAppName)
      FUNCTIONS_EXTENSION_VERSION : '~4'
      APPINSIGHTS_INSTRUMENTATIONKEY: applicationInsights.properties.InstrumentationKey
      FUNCTIONS_WORKER_RUNTIME: functionWorkerRuntime
      SERVICEBUS_CONNECTION__fullyQualifiedNamespace: serviceBusHost
      GHAPP_ID: GHApplicationId
      GHAPP_PEMCERTIFICATE: '@Microsoft.KeyVault(SecretUri=${keyCertificate.properties.secretUri})'
      GHAPP_WEBHOOKSECRET: '@Microsoft.KeyVault(SecretUri=${keyWebHookSecret.properties.secretUri})'
    }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: appInsightsLocation
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

//////////////////////////////////////// Keyvault

resource vault 'Microsoft.KeyVault/vaults@2021-11-01-preview' = {
  name: vaultName
  location: location
  properties: {
    accessPolicies:[
      {
        tenantId: subscription().tenantId
        objectId: functionApp.identity.principalId
        permissions: {
          keys: []
          secrets: [ 'get' ]
          certificates: []
        }
      }
    ]
    enableRbacAuthorization: false
    enableSoftDelete: false
    enabledForDeployment: true
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    tenantId: subscription().tenantId
    sku: {
      name: vaultSKU
      family: 'A'
    }
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

var pemCertificateName = '${appShortName}-PEM-Certificate'
var webHookSecretName = '${appShortName}-webhooksecret'

resource keyCertificate 'Microsoft.KeyVault/vaults/secrets@2022-11-01' = {
  parent: vault
  name: pemCertificateName
  properties: {
    value: certificate
  }  
}

resource keyWebHookSecret 'Microsoft.KeyVault/vaults/secrets@2022-11-01' = {
  parent: vault
  name: webHookSecretName
  properties: {
    value: webHookSecret
  }  
}

resource KeyFunctionStorageConnectionString 'Microsoft.KeyVault/vaults/secrets@2022-11-01' = {
  parent: vault
  name: 'function-storage-connectionstring'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
  }  
}

//////////////////////////////// Output
output functionAppId string = functionApp.id
