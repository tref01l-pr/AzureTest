// PdfShare — Azure Container Apps deployment.
//
//   az group create -n rg-pdfshare -l westeurope
//   az deployment group create -g rg-pdfshare -f deploy/azure/main.bicep \
//     -p jwtSecret=$(openssl rand -base64 48)
//
// First run deploys placeholder images; the CI workflow then pushes real images to ACR
// and calls `az containerapp update`. All app → Azure access uses one user-assigned
// managed identity (no storage keys, no SQL passwords).

targetScope = 'resourceGroup'

@description('Short prefix for resource names (lowercase letters/digits).')
@minLength(3)
@maxLength(11)
param namePrefix string = 'pdfshare'

param location string = resourceGroup().location

@description('HS256 signing key for access tokens (min 32 chars). Stored in Key Vault.')
@secure()
@minLength(32)
param jwtSecret string

@description('Image tag to deploy. Leave empty on the very first deployment (images do not exist in ACR yet).')
param imageTag string = ''

var suffix = uniqueString(resourceGroup().id)
var placeholderImage = 'mcr.microsoft.com/k8se/quickstart:latest'
var acrName = toLower('${namePrefix}acr${suffix}')
var images = {
  api: empty(imageTag) ? placeholderImage : '${acr.properties.loginServer}/pdfshare-api:${imageTag}'
  web: empty(imageTag) ? placeholderImage : '${acr.properties.loginServer}/pdfshare-web:${imageTag}'
  func: empty(imageTag) ? placeholderImage : '${acr.properties.loginServer}/pdfshare-func:${imageTag}'
}
var databaseName = 'PdfShare'

// Built-in role definition ids
var roles = {
  acrPull: '7f951dda-4ed3-4680-a7ca-43fe172d538d'
  blobDataOwner: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b' // Functions host (AzureWebJobsStorage) + app blobs
  blobDelegator: 'db58b8e5-c6ad-4a2a-8342-4190687cbf4a' // User Delegation SAS
  queueDataContributor: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
  tableDataContributor: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3' // Functions host bookkeeping
  keyVaultSecretsUser: '4633458b-17de-408a-b874-0445c86b69e6'
}

// ---------------------------------------------------------------- Observability
resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${namePrefix}-logs-${suffix}'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${namePrefix}-ai-${suffix}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
  }
}

// ---------------------------------------------------------------- Identity
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${namePrefix}-id-${suffix}'
  location: location
}

// ---------------------------------------------------------------- Container registry
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: false
  }
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, identity.id, roles.acrPull)
  scope: acr
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.acrPull)
  }
}

// ---------------------------------------------------------------- Storage (blobs + queue)
resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: toLower('${namePrefix}st${suffix}')
  location: location
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false // identity only; SAS URLs are User Delegation SAS
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }

  resource blobService 'blobServices' = {
    name: 'default'
    properties: {
      // Lets the browser download SAS URLs directly (GET only).
      cors: {
        corsRules: [
          {
            allowedOrigins: [ 'https://${webApp.properties.configuration.ingress.fqdn}' ]
            allowedMethods: [ 'GET', 'HEAD' ]
            allowedHeaders: [ '*' ]
            exposedHeaders: [ 'Content-Disposition' ]
            maxAgeInSeconds: 3600
          }
        ]
      }
    }

    resource pdfs 'containers' = {
      name: 'pdfs'
      properties: { publicAccess: 'None' }
    }

    resource thumbnails 'containers' = {
      name: 'thumbnails'
      properties: { publicAccess: 'None' }
    }
  }

  resource queueService 'queueServices' = {
    name: 'default'

    resource pdfUploaded 'queues' = {
      name: 'pdf-uploaded'
    }
  }
}

resource storageRoles 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for role in [roles.blobDataOwner, roles.blobDelegator, roles.queueDataContributor, roles.tableDataContributor]: {
  name: guid(storage.id, identity.id, role)
  scope: storage
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', role)
  }
}]

// ---------------------------------------------------------------- Azure SQL (Entra-only auth)
resource sql 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${namePrefix}-sql-${suffix}'
  location: location
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    // Test-assignment shortcut: the app identity is the Entra admin, so it can run EF migrations
    // without a manual CREATE USER step. Production: a separate admin group + least-privilege DB user.
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: identity.name
      sid: identity.properties.principalId
      tenantId: subscription().tenantId
      principalType: 'Application'
    }
  }

  resource allowAzureServices 'firewallRules' = {
    name: 'AllowAzureServices'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress: '0.0.0.0'
    }
  }

  resource database 'databases' = {
    name: databaseName
    location: location
    sku: {
      name: 'GP_S_Gen5_1'
      tier: 'GeneralPurpose'
    }
    properties: {
      autoPauseDelay: 60
      minCapacity: json('0.5')
      zoneRedundant: false
    }
  }
}

// ---------------------------------------------------------------- Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: toLower('${namePrefix}kv${suffix}')
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }

  resource jwt 'secrets' = {
    name: 'JWT-SECRET'
    properties: { value: jwtSecret }
  }
}

resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, identity.id, roles.keyVaultSecretsUser)
  scope: keyVault
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.keyVaultSecretsUser)
  }
}

// ---------------------------------------------------------------- Container Apps
resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${namePrefix}-env-${suffix}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      { name: 'Consumption', workloadProfileType: 'Consumption' }
    ]
  }
}

var sqlConnectionString = 'Server=tcp:${sql.properties.fullyQualifiedDomainName},1433;Database=${databaseName};Authentication=Active Directory Managed Identity;User Id=${identity.properties.clientId};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;'
var blobServiceUri = storage.properties.primaryEndpoints.blob
var queueServiceUri = storage.properties.primaryEndpoints.queue
var registries = empty(imageTag) ? [] : [ { server: acr.properties.loginServer, identity: identity.id } ]
var commonEnv = [
  { name: 'DATABASE_URL', value: sqlConnectionString }
  { name: 'BLOB_SERVICE_URI', value: blobServiceUri }
  { name: 'QUEUE_SERVICE_URI', value: queueServiceUri }
  { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
  { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
]

resource apiApp 'Microsoft.App/containerApps@2024-10-02-preview' = {
  name: '${namePrefix}-api'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      // Internal only: reachable from the web app's nginx, never directly from the internet.
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
      }
      registries: registries
      secrets: [
        { name: 'jwt-secret', keyVaultUrl: keyVault::jwt.properties.secretUri, identity: identity.id }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: images.api
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: concat(commonEnv, [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'JWT_SECRET', secretRef: 'jwt-secret' }
            { name: 'RUN_MIGRATIONS', value: 'true' }
          ])
          probes: empty(imageTag) ? [] : [
            { type: 'Startup', httpGet: { path: '/health', port: 8080 }, initialDelaySeconds: 5, periodSeconds: 10, failureThreshold: 30 }
            { type: 'Readiness', httpGet: { path: '/health', port: 8080 }, periodSeconds: 15 }
          ]
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 3 }
    }
  }
  dependsOn: [ acrPull, storageRoles, keyVaultSecretsUser ]
}

resource webApp 'Microsoft.App/containerApps@2024-10-02-preview' = {
  name: '${namePrefix}-web'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 80
        transport: 'http'
        allowInsecure: false
      }
      registries: registries
    }
    template: {
      containers: [
        {
          name: 'web'
          image: images.web
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            // Container Apps in one environment reach each other by app name (internal ingress, port 80 → 8080).
            { name: 'API_UPSTREAM', value: 'http://${namePrefix}-api' }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
        rules: [ { name: 'http', http: { metadata: { concurrentRequests: '50' } } } ]
      }
    }
  }
  dependsOn: [ acrPull ]
}

resource funcApp 'Microsoft.App/containerApps@2024-10-02-preview' = {
  name: '${namePrefix}-func'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      registries: registries
    }
    template: {
      containers: [
        {
          name: 'func'
          image: images.func
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: concat(commonEnv, [
            { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
            // Host storage + trigger connection "PdfStorage", both identity-based.
            { name: 'AzureWebJobsStorage__accountName', value: storage.name }
            { name: 'AzureWebJobsStorage__credential', value: 'managedidentity' }
            { name: 'AzureWebJobsStorage__clientId', value: identity.properties.clientId }
            { name: 'PdfStorage__queueServiceUri', value: queueServiceUri }
            { name: 'PdfStorage__credential', value: 'managedidentity' }
            { name: 'PdfStorage__clientId', value: identity.properties.clientId }
          ])
        }
      ]
      // Scale to zero; KEDA adds replicas when "pdf-uploaded" has messages (auth via the managed identity).
      scale: {
        minReplicas: 0
        maxReplicas: 5
        rules: [
          {
            name: 'pdf-uploaded-queue'
            custom: {
              type: 'azure-queue'
              identity: identity.id
              metadata: {
                accountName: storage.name
                queueName: 'pdf-uploaded'
                queueLength: '5'
              }
            }
          }
        ]
      }
    }
  }
  dependsOn: [ acrPull, storageRoles ]
}

output webUrl string = 'https://${webApp.properties.configuration.ingress.fqdn}'
output acrLoginServer string = acr.properties.loginServer
output acrName string = acr.name
output apiAppName string = apiApp.name
output webAppName string = webApp.name
output funcAppName string = funcApp.name
output identityClientId string = identity.properties.clientId
