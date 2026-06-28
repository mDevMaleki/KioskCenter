Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole -All -NoRestart
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer -All -NoRestart
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures -All -NoRestart
Enable-WindowsOptionalFeature -Online -FeatureName IIS-StaticContent -All -NoRestart
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DefaultDocument -All -NoRestart
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors -All -NoRestart
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DirectoryBrowsing -All -NoRestart

iisreset
