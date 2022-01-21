
# go to release folder
Set-Location $global:PSScriptRoot+"\..\Client\s7-symbol-client\s7-symbol-client\bin\Release\net5.0\"

# init ams netid
$AmsNetId = "127.0.0.1.1.1"

# read all symboles from server
$Symbols = &"./s7-symbol-client.exe" list --NetId $AmsNetId

# read all values from server
foreach ($Symbol in $Symbols)
{
  Write-Host $Symbol
}