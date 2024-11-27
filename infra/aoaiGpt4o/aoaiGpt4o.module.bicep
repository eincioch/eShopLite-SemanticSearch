@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalType string

resource aoaiGpt4o 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: take('aoaiGpt4o-${uniqueString(resourceGroup().id)}', 64)
  location: location
  kind: 'OpenAI'
  properties: {
    customSubDomainName: toLower(take(concat('aoaiGpt4o', uniqueString(resourceGroup().id)), 24))
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
  sku: {
    name: 'S0'
  }
  tags: {
    'aspire-resource-name': 'aoaiGpt4o'
  }
}

resource aoaiGpt4o_CognitiveServicesOpenAIContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aoaiGpt4o.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a001fd3d-188f-4b5d-821b-7da978bf7442')
    principalType: principalType
  }
  scope: aoaiGpt4o
}

resource chat 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  name: 'chat'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o-mini'
      version: '2024-07-18'
    }
  }
  sku: {
    name: 'GlobalStandard'
    capacity: 10
  }
  parent: aoaiGpt4o
}

output connectionString string = 'Endpoint=${aoaiGpt4o.properties.endpoint}'