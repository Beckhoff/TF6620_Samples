
Set-Location $global:PSScriptRoot+"\..\Client\s7-symbol-client\s7-symbol-client\bin\Release\net5.0\"

$AmsNetId= "127.0.0.1.1.1"
$SymbolAndValues_1 = @( 
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BOOL_1",    "true"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BOOL_2",    "false"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BOOL_3",    "true"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.INT_1",     "42"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BYTE_1",    "55"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.DINT_1",    "-42"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.DWORD_1",   "1000000"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.LREAL_1",   "3,14"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.STRING_1",  "String Nr1")
)

$SymbolAndValues_2 = @( 
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BOOL_1",    "false"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BOOL_2",    "true"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BOOL_3",    "false"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.INT_1",     "32"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BYTE_1",    "44"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.DINT_1",    "-32"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.DWORD_1",   "2000000"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.LREAL_1",   "-3,14"),
  ("TCP/UDP RT.S7 Connector B.S7 Symbol Server B.STRING_1",  "String Nr2")
)

# write all values to server
foreach ($SymbolValue in $SymbolAndValues_1)
{
  &"./s7-symbol-client.exe" write --NetId $AmsNetId $SymbolValue
}
# read all values to server
foreach ($SymbolValue in $SymbolAndValues_1)
{
  $value = &"./s7-symbol-client.exe" read --NetId $AmsNetId $SymbolValue[0]

  if ($value -ne $SymbolValue[1])
  {
    $message = "Write " + $SymbolValue[0]  + " failed!`r`n" + "Value is "  + $value + "`r`n" + "But should be " + $SymbolValue[1]
    Write-Error $message
    exit 1
  }
}

# write all values to server
foreach ($SymbolValue in $SymbolAndValues_2)
{
  &"./s7-symbol-client.exe" write --NetId $AmsNetId $SymbolValue
}
# read all values to server
foreach ($SymbolValue in $SymbolAndValues_2)
{
  $value = &"./s7-symbol-client.exe" read --NetId $AmsNetId $SymbolValue[0]

  if ($value -ne $SymbolValue[1])
  {
    $message = "Write " + $SymbolValue[0]  + " failed!`r`n" + "Value is "  + $value + "`r`n" + "But should be " + $SymbolValue[1]
    Write-Error $message
    exit 1
  }
}

