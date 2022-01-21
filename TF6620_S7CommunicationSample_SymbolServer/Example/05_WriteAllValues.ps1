

Set-Location $global:PSScriptRoot+"\..\Client\s7-symbol-client\s7-symbol-client\bin\Release\net5.0\"

$AmsNetId= "127.0.0.1.1.1"

$SymbolAndValues = @( 
  "TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BOOL_1",    "true",
  "TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BOOL_2",    "false",
  "TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BOOL_3",    "true",
  "TCP/UDP RT.S7 Connector B.S7 Symbol Server B.INT_1",     "0x4242",
  "TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BYTE_1",    "0x11",
  "TCP/UDP RT.S7 Connector B.S7 Symbol Server B.DINT_1",    "-42",
  "TCP/UDP RT.S7 Connector B.S7 Symbol Server B.DWORD_1",   "0x12345678"
  "TCP/UDP RT.S7 Connector B.S7 Symbol Server B.LREAL_1",   "3.14",
  "TCP/UDP RT.S7 Connector B.S7 Symbol Server B.STRING_1",  "Hello S7Comm"
)

######################################################################
# variant 1: single write
# write all values to server

for ($i = 0; $i -lt $SymbolAndValues.Count; $i+=2)
{
  $Symbole = $SymbolAndValues[$i]
  $Value   = $SymbolAndValues[$i + 1]
  &"./s7-symbol-client.exe" write --NetId $AmsNetId $Symbole $Value
}

######################################################################
# variant 2: sum write
# write all values to server
 &"./s7-symbol-client.exe" write --NetId $AmsNetId $SymbolAndValues
