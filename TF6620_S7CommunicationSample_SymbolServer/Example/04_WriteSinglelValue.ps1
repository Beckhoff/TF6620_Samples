

# go to release folder
Set-Location $global:PSScriptRoot+"\..\Client\s7-symbol-client\s7-symbol-client\bin\Release\net5.0\"

# write singel symbole from server
&"./s7-symbol-client.exe" write --NetId "127.0.0.1.1.1" "TCP/UDP RT.S7 Connector B.S7 Symbol Server B.BOOL_1" "true"
