

# go to release folder
Set-Location $global:PSScriptRoot+"\..\Client\s7-symbol-client\s7-symbol-client\bin\Release\net5.0\"

# init ams netid
$AmsNetId = "127.0.0.1.1.1"

# read all symboles from server
$Symbols = &"./s7-symbol-client.exe" list --NetId $AmsNetId


######################################################################
# variant 1: single read

# read all values from server
foreach ($Symbol in $Symbols)
{
  $Value= &"./s7-symbol-client.exe" read --NetId $AmsNetId $Symbol
  Write-Host $Symbol : $Value
}


######################################################################
# variant 2: sum request

# read all values from server
$Values = &"./s7-symbol-client.exe" read --NetId $AmsNetId $Symbols

# check if all values are read
if ($Values.Count -ne $Symbols.Count)
{
  Write-Error "Number of read values is not equal to symbols"
  exit 1
}

# print all values
for ($i = 0; $i -lt $Values.Count; $i++)
{
  Write-Host $Symbols[$i] : $Values[$i]
}