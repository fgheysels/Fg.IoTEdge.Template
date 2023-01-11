$deploymentFiles = Get-ChildItem -Path ./.. -File -Recurse -Include "deployment.template.json" -ErrorAction SilentlyContinue

if ($deploymentFiles.Count -eq 0) {
    Write-Warning "Could not modify the deployment.template.json file"
    Write-Error "deployment.template.json file not found."    
    return
}

if ($deploymentFiles.Count -gt 1) {
    Write-Warning "Could not modify the deployment.template.json file"
    Write-Warning "Multiple deployment.template.json files found:"
    $deploymentFiles    
    return
}

$deploymentManifestJson = Get-Content $deploymentFiles[0].FullName | ConvertFrom-Json

$modulesSection = $deploymentManifestJson.modulesContent.'$edgeAgent'.'properties.desired'.modules

if( $null -eq $modulesSection ) {
    Write-Warning "Could not modify the deployment.template.json file"
    Write-Warning "$($deploymentFiles[0].FullName) does not have expected content."
    return
}

$moduleSettings = [PSCustomObject] @{
    image           = '${MODULEDIR<../FgModule}>'
    createOptions   = New-Object PSObject
}

$moduleDescription = [PSCustomObject] @{
    version         = "1.0.0"
    type            = "docker"
    status          = "running"
    restartPolicy   = "always"
    settings        = $moduleSettings
}

$modulesSection | Add-Member -Name "FgModule" -Value $moduleDescription -MemberType NoteProperty

Set-Content -Path $deploymentFiles[0].FullName -Value ($deploymentManifestJson | ConvertTo-Json -Depth 100)

Remove-Item $PSCommandPath -Force 


