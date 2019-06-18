set ResGrpName=%1
set ResLocation=%2
set botappname=%3
set botpswd=%4

az login
az 

az group create --name %ResGrpName% --location %ResLocation%
az storage account create --resource-group $ENV:ResGrpName --name %botappname% --https-only -l %ResLocation%
az ad app create --display-name %botappname% --password %botpswd% --available-to-other-tenants

az bot prepare-deploy --lang Csharp --code-dir "." --proj-file-path "SampleFunctionBot.csproj"
az webapp deployment source config-zip --resource-group "%ResGrpName%" --name "%botappname%" --src "code.zip"