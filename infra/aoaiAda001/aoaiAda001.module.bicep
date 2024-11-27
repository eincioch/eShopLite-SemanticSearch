@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalType string

resource aoaiAda001 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: take('aoaiAda001-${uniqueString(resourceGroup().id)}', 64)
  location: location
  kind: 'OpenAI'
  properties: {
    customSubDomainName: toLower(take(concat('aoaiAda001', uniqueString(resourceGroup().id)), 24))
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
  sku: {
    name: 'S0'
  }
  tags: {
    'aspire-resource-name': 'aoaiAda001'
  }
}

resource aoaiAda001_CognitiveServicesOpenAIContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aoaiAda001.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442')
    principalType: principalType
  }
  scope: aoaiAda001
}

resource embeddings 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  name: 'embeddings'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-ada-002'
      version: '2'
    }
  }
  sku: {
    name: 'Standard'
    capacity: 8
  }
  parent: aoaiAda001
}

output connectionString string = 'Endpoint=${aoaiAda001.properties.endpoint}'