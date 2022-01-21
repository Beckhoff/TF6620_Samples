# Symbol server sample

## Overview
This sample demonstrates how to access the symbol server feature from within a .NET application. 
The sample application uses the ADS .NET library to establish a connection with the symbol server 
and execute different commands. The sample consists of the following components:
- Client: .NET application
- Server: TwinCAT project with pre-configured S7 Communication device
- Examples: sample Powershell scripts that demonstrates how to execute the client application for different purposes

## Instructions
Please perform the following steps to get the sample running:
- Activate the TwinCAT project from the "Server" directory
- Open the client application from the "Client" directory. 
  Please note that a full Visual Studio (2019 with .NET Core 5) is required because this is a .NET application.
- Compile the client application and execute it

## .NET application usage
The usage of the .Net application is defined as follows:

```
s7-symbol-client MODUS [OPTIONS] [ARGUMENTS]
  MODUS
    list                 List all symbols from server
    read                 Read data from server
    write                Write data to server
  [OPTIONS]
    --NetId <AmsNetId>   AmsNetId of S7 communication server
                         Use local AmsNetId if unused
    --Help               Prints this message
  [ARGUMENTS]
    The usage of arguments depens on the selected MODUS.
    list does not use [ARGUMENTS]
    read expect a list of symols that sould be read
    write expect a list of symols, followed by a vallue that sould be read
```

You can also check the Powershell script files in the "Examples" directory to see sample calls of the executable.