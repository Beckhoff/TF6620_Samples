using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace s7_symbol_client
{
    internal class Program
    {
        public static class Constants
        {
            public const int ServerPort = 20100;
        }
        enum Modus
        {
            List,
            Read,
            Write
        }
        struct Context
        {
            public Modus Modus;
            public AmsNetId Address;
            public List<SymbolContext> Symbols;
        }
        struct SymbolContext
        {
            public SymbolContext(string name) : this()
            {
                Name = name;
                Value = "";
            }
            public SymbolContext(string name, string value) : this()
            {
                Name = name;
                Value = value;
            }
            public string Name;
            public string Value;
        }


        static void Usage()
        {
            Console.Error.WriteLine("{0}", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            Console.Error.WriteLine("");
            Console.Error.WriteLine("Usage:");
            Console.Error.WriteLine("  {0} MODUS [OPTIONS] [ARGUMENTS]", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            Console.Error.WriteLine("  MODUS");
            Console.Error.WriteLine("    list                 List all symbols from server");
            Console.Error.WriteLine("    read                 Read symbol from server");
            Console.Error.WriteLine("    write                Write symbol value to server");
            Console.Error.WriteLine("  [OPTIONS]");
            Console.Error.WriteLine("    --NetId <AmsNetId>   AmsNetId of S7 communication server");
            Console.Error.WriteLine("                         Uses local AmsNetId if unspecified");
            Console.Error.WriteLine("    --Help               Prints this message");
            Console.Error.WriteLine("  [ARGUMENTS]");
            Console.Error.WriteLine("    The usage of arguments depends on the selected MODUS.");
            Console.Error.WriteLine("    list does not use [ARGUMENTS]");
            Console.Error.WriteLine("    read expects a list of symbols that sould be read");
            Console.Error.WriteLine("    write expects a list of symbols, followed by a value that sould be written");
            Console.Error.WriteLine("");
            Console.Error.WriteLine("Examples:");
            Console.Error.WriteLine("   01: List all symbols of local server");
            Console.Error.WriteLine("       > {0} list", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            Console.Error.WriteLine("   02: List all symbols of a remote server");
            Console.Error.WriteLine("       > {0} list --NetId 11.22.33.44.1.1", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            Console.Error.WriteLine("   03: Read one variable");
            Console.Error.WriteLine("       > {0} read S7CommVar_1", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            Console.Error.WriteLine("   04: Read two variables");
            Console.Error.WriteLine("       > {0} read S7CommVar_1 S7CommVar_2", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            Console.Error.WriteLine("   05: Read all variables");
            Console.Error.WriteLine("       > $variables = {0} list", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            Console.Error.WriteLine("       > $values    = {0} read $variables", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            Console.Error.WriteLine("   06: Write one variable");
            Console.Error.WriteLine("       > {0} read BOOL_1 true", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            Console.Error.WriteLine("   07: Write two variables");
            Console.Error.WriteLine("       > {0} read BOOL_1 true INT_1 42", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
        }

        class UsageException : Exception
        {
            public UsageException(string msg = "") : base(msg) { }
        }

        static Context _ctx = new Context();
        static AdsClient _client = new AdsClient();

        /// <summary>
        /// Main method
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            try
            {
                InitContext();
                ParseArgs(args);
                ConnectToServer();
                RunCommand();
            }
            catch (AdsErrorException e)
            {
                int errorCode = Convert.ToInt32(e.ErrorCode);
                if ((errorCode & 0x0000FFFF) == 0x0700) // is device error
                {
                    // s7 comm error code representation:
                    // 0x0000 No error
                    // 0x___1 Connection error
                    // 0x___2 Read var error
                    // 0x___4 Write var error
                    // 0x___8 Invalid pdu length
                    // 0x__1_ Hardware fault
                    // 0x__2_ Accessing the object not allowed
                    // 0x__3_ Address out of range
                    // 0x__4_ Data type not supported
                    // 0x__5_ Data type inconsistent
                    // 0x__6_ Object does not exist
                    // 0xXX__ Item ID if read or write error
                    //        Connection state if connection error
                    //
                    // for example 0xF301:
                    // -> 01: Connection error
                    // -> F3: TCP timeout
                    int s7error = (errorCode >> 16) & 0x0000FFFF;
                    Console.Error.WriteLine(string.Format("S7 Communication error 0x{0:X04}", s7error));
                    Console.Error.WriteLine(e.StackTrace);
                }
                else
                {
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.StackTrace);
                }

            }
            catch (UsageException e)
            {
                if (e.Message != "")
                    Console.Error.WriteLine(e.Message);
                Usage();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);
            }

        }

        /// <summary>
        /// Initializes the execution context with default values
        /// </summary>
        static void InitContext()
        {
            _ctx.Modus = Modus.List;
            _ctx.Address = AmsNetId.LocalHost;
            _ctx.Symbols = new List<SymbolContext>();
        }

        /// <summary>
        /// Parses command line arguments and prepares the command that should be executed
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void ParseArgs(string[] args)
        {
            if (args.Length == 0)
                throw new UsageException();

            _ctx.Modus = StringToModus(args[0]);

            for (uint i = 1; i < args.Length; i++)
            {
                if (args[i] == "--NetId")
                {
                    if (++i >= args.Length)
                        throw new UsageException("Missing value for --NetId");

                    _ctx.Address = new AmsNetId(args[i]);
                }
                else
                {
                    switch (_ctx.Modus)
                    {
                        case Modus.List:
                            throw new UsageException("Mode \"list\" doesn't use [ARGUMENTS]");

                        case Modus.Read:
                            _ctx.Symbols.Add(new SymbolContext(args[i]));
                            break;

                        case Modus.Write:
                            if (++i >= args.Length)
                                throw new UsageException(string.Format("Missing value for symbol {0}", args[i - 1]));

                            string name = args[i - 1];
                            string value = args[i];
                            _ctx.Symbols.Add(new SymbolContext(name, value));
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Helper method to interpret command
        /// </summary>
        /// <param name="m">Command from corresponding command line argument</param>
        /// <returns></returns>
        static Modus StringToModus(string m)
        {
            m = m.ToLower();

            if (m == "list")
                return Modus.List;
            if (m == "read")
                return Modus.Read;
            if (m == "write")
                return Modus.Write;

            throw new UsageException("Invalid modus: " + m);
        }

        /// <summary>
        /// Connects to symbol server of S7 Communication driver
        /// </summary>
        static void ConnectToServer()
        {
            // has to be greater than TCP total connection timeout (TcpMaxRetry*TcpTimeoutCon), otherwise ADS timeout (0x745) occurs if no TCP session can be established
            _client.Timeout = 60000;
            _client.Connect(_ctx.Address, Constants.ServerPort);
        }

        /// <summary>
        /// Executes the desired command
        /// </summary>
        static void RunCommand()
        {
            switch (_ctx.Modus)
            {
                case Modus.List: RunList(); break;
                case Modus.Write: RunWrite(); break;
                case Modus.Read: RunRead(); break;
            }
        }

        #region List

        /// <summary>
        /// Command 'list': prepares and loads symbol namespace
        /// </summary>
        static void RunList()
        {
            ISymbolLoader loader = SymbolLoaderFactory.Create(_client, SymbolLoaderSettings.Default);

            PrintSymbolsFullName(loader.Symbols);
        }

        /// <summary>
        /// Prints symbol namespace on command line
        /// </summary>
        /// <param name="symbols">Symbol collection</param>
        static void PrintSymbolsFullName(ISymbolCollection<ISymbol> symbols)
        {
            foreach (ISymbol symbol in symbols)
            {
                if (symbol.Size > 0)
                {
                    // Ok. This is symbole contains a datatype
                    Console.WriteLine(symbol.InstancePath);
                }
                else
                {
                    // This is a node
                    // try to print childs
                    PrintSymbolsFullName(symbol.SubSymbols);
                }
            }
        }

        #endregion

        #region Write

        /// <summary>
        /// Command 'write': prepares the write command and checks if a single or sum write should be performed
        /// </summary>
        static void RunWrite()
        {
            if (_ctx.Symbols.Count == 0)
            {
                throw new UsageException("Expect a variable to write");
            }
            else if (_ctx.Symbols.Count == 1)
            {
                WriteSingleValue();
            }
            else
            {
                WriteWithSumRequest();
            }
        }

        /// <summary>
        /// Executes a write command for a single variable
        /// </summary>
        static void WriteSingleValue()
        {
            IAdsSymbol symbol = _client.ReadSymbol(_ctx.Symbols[0].Name);
            object value = ValueStringToObject(_ctx.Symbols[0].Value, symbol.DataTypeId);
            _client.WriteValue(symbol, value);
        }

        /// <summary>
        /// Converts an input value from a specified ADS data type to an array of byte
        /// </summary>
        /// <param name="value">Input value</param>
        /// <param name="type">ADS data type</param>
        /// <returns>Returns input value as byte array</returns>
        static byte[] ValueStringToByteArray(string value, AdsDataTypeId type)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            NumberStyles format = NumberStyles.Integer;
            switch (type)
            {
                case AdsDataTypeId.ADST_INT8:
                case AdsDataTypeId.ADST_INT16:
                case AdsDataTypeId.ADST_INT32:
                case AdsDataTypeId.ADST_INT64:
                case AdsDataTypeId.ADST_UINT8:
                case AdsDataTypeId.ADST_UINT16:
                case AdsDataTypeId.ADST_UINT32:
                case AdsDataTypeId.ADST_UINT64:
                    if (ValueIsHex(value))
                    {
                        value = value.Remove(0, 2);
                        format = NumberStyles.HexNumber;
                    }
                    break;
            }

            switch (type)
            {
                case AdsDataTypeId.ADST_BIT:
                    bw.Write(bool.Parse(value));
                    break;

                case AdsDataTypeId.ADST_INT8:
                    bw.Write(sbyte.Parse(value, format));
                    break;

                case AdsDataTypeId.ADST_UINT8:
                    bw.Write(byte.Parse(value, format));
                    break;

                case AdsDataTypeId.ADST_INT16:
                    bw.Write(short.Parse(value, format));
                    break;

                case AdsDataTypeId.ADST_UINT16:
                    bw.Write(ushort.Parse(value, format));
                    break;

                case AdsDataTypeId.ADST_INT32:
                    bw.Write(int.Parse(value, format));
                    break;

                case AdsDataTypeId.ADST_UINT32:
                    bw.Write(uint.Parse(value, format));
                    break;

                case AdsDataTypeId.ADST_INT64:
                    bw.Write(long.Parse(value, format));
                    break;

                case AdsDataTypeId.ADST_UINT64:
                    bw.Write(ulong.Parse(value, format));
                    break;

                case AdsDataTypeId.ADST_REAL32:
                    bw.Write(Single.Parse(value));
                    break;

                case AdsDataTypeId.ADST_REAL64:
                    bw.Write(double.Parse(value));
                    break;

                case AdsDataTypeId.ADST_STRING:
                    byte[] b = Encoding.ASCII.GetBytes(value);
                    bw.Write(b);
                    bw.Write((byte)0);

                    break;

                case AdsDataTypeId.ADST_VOID:
                case AdsDataTypeId.ADST_WSTRING:
                case AdsDataTypeId.ADST_REAL80:
                case AdsDataTypeId.ADST_MAXTYPES:
                case AdsDataTypeId.ADST_BIGTYPE:
                default:
                    throw new Exception(string.Format("Ads datatype {0} is not implemented", type.ToString()));
            }

            return ms.ToArray();
        }

        /// <summary>
        /// Checks if input value is in hax format
        /// </summary>
        /// <param name="value">Input value</param>
        /// <returns>Returns true if input value is in hex format</returns>
        static bool ValueIsHex(string value)
        {
            return value.StartsWith("0x") || value.StartsWith("0X");
        }

        /// <summary>
        /// Converts an input value from a specified ADS data type to an object
        /// </summary>
        /// <param name="value">Input value</param>
        /// <param name="type">ADS data type</param>
        /// <returns>Returns input value as object</returns>
        static object ValueStringToObject(string value, AdsDataTypeId type)
        {
            NumberStyles format = NumberStyles.Integer;

            switch (type)
            {
                case AdsDataTypeId.ADST_INT8:
                case AdsDataTypeId.ADST_INT16:
                case AdsDataTypeId.ADST_INT32:
                case AdsDataTypeId.ADST_INT64:
                case AdsDataTypeId.ADST_UINT8:
                case AdsDataTypeId.ADST_UINT16:
                case AdsDataTypeId.ADST_UINT32:
                case AdsDataTypeId.ADST_UINT64:
                    if (ValueIsHex(value))
                    {
                        value = value.Remove(0, 2);
                        format = NumberStyles.HexNumber;
                    }
                    break;
            }

            switch (type)
            {
                case AdsDataTypeId.ADST_BIT:
                    return bool.Parse(value);

                case AdsDataTypeId.ADST_INT8:
                    return sbyte.Parse(value, format);

                case AdsDataTypeId.ADST_UINT8:
                    return byte.Parse(value, format);

                case AdsDataTypeId.ADST_INT16:
                    return short.Parse(value, format);

                case AdsDataTypeId.ADST_UINT16:
                    return ushort.Parse(value, format);

                case AdsDataTypeId.ADST_INT32:
                    return int.Parse(value, format);

                case AdsDataTypeId.ADST_UINT32:
                    return uint.Parse(value, format);

                case AdsDataTypeId.ADST_INT64:
                    return long.Parse(value, format);

                case AdsDataTypeId.ADST_UINT64:
                    return ulong.Parse(value, format);

                case AdsDataTypeId.ADST_REAL32:
                    return Single.Parse(value);

                case AdsDataTypeId.ADST_REAL64:
                    return double.Parse(value);

                case AdsDataTypeId.ADST_STRING:
                    return value;

                case AdsDataTypeId.ADST_VOID:
                case AdsDataTypeId.ADST_WSTRING:
                case AdsDataTypeId.ADST_REAL80:
                case AdsDataTypeId.ADST_MAXTYPES:
                case AdsDataTypeId.ADST_BIGTYPE:
                default:
                    throw new Exception(string.Format("Ads datatype {0} is not implemented", type.ToString()));
            }
        }

        /// <summary>
        /// Executes a write command for multiple variables (ADS sum command)
        /// </summary>
        static void WriteWithSumRequest()
        {
            List<IAdsSymbol> symbols = new List<IAdsSymbol>();
            List<uint> handles = new List<uint>();


            // get symbols and handles
            foreach (SymbolContext symbol in _ctx.Symbols)
            {
                symbols.Add(_client.ReadSymbol(symbol.Name));
                handles.Add(_client.CreateVariableHandle(symbol.Name));
            }

            // we expect n * ads return values
            int readLen = _ctx.Symbols.Count * sizeof(UInt32);

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            // build request
            for (int i = 0; i < symbols.Count; i++)
            {
                bw.Write((uint)AdsReservedIndexGroup.SymbolValueByHandle);
                bw.Write(handles[i]);
                bw.Write(symbols[i].ByteSize);
            }

            // Write data to send to PLC behind the structure
            for (int i = 0; i < _ctx.Symbols.Count; i++)
            {
                byte[] value = ValueStringToByteArray(_ctx.Symbols[i].Value, symbols[i].DataTypeId);

                bw.Write(value);
            }

            // sum write
            byte[] readBuffer = new byte[readLen];
            _client.ReadWrite((uint)AdsReservedIndexGroup.SumCommandWrite, (uint)symbols.Count, readBuffer, ms.ToArray());
            BinaryReader br = new BinaryReader(new MemoryStream(readBuffer));

            // read return values
            for (int i = 0; i < symbols.Count; i++)
            {
                UInt32 errcode = br.ReadUInt32();

                if (errcode != 0)
                {
                    throw new Exception("On Symbol " + i.ToString() + " errorcode: 0x" + errcode.ToString("0X8"));
                }
            }
        }

        #endregion

        #region Read

        /// <summary>
        /// Command 'read': prepares the read command and checks if a single or sum read should be performed
        /// </summary>
        static void RunRead()
        {
            if (_ctx.Symbols.Count == 0)
            {
                throw new UsageException("Expect a value to read");
            }
            else if (_ctx.Symbols.Count == 1)
            {
                ReadSingleValue();
            }
            else
            {
                ReadWithSumRequest();
            }
        }

        /// <summary>
        /// Executes a read command a single variable
        /// </summary>
        static void ReadSingleValue()
        {
            IAdsSymbol symbol = _client.ReadSymbol(_ctx.Symbols[0].Name);
            var value = _client.ReadValue(symbol);

            Console.WriteLine(value.ToString());
        }

        /// <summary>
        /// Executes a read command for multiple variables (ADS sum command)
        /// </summary>
        private static void ReadWithSumRequest()
        {
            List<IAdsSymbol> symbols = new List<IAdsSymbol>();
            List<uint> handles = new List<uint>();

            // get symbol and handles
            foreach (SymbolContext symbol in _ctx.Symbols)
            {
                symbols.Add(_client.ReadSymbol(symbol.Name));
                handles.Add(_client.CreateVariableHandle(symbol.Name));
            }

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            // we expect n * ads return values
            int readLen = _ctx.Symbols.Count * sizeof(UInt32);

            // build request
            for (int i = 0; i < symbols.Count; i++)
            {
                bw.Write((uint)AdsReservedIndexGroup.SymbolValueByHandle);
                bw.Write(handles[i]);
                bw.Write(symbols[i].ByteSize);

                // increase expected read length
                readLen += symbols[i].ByteSize;
            }

            // sum read
            byte[] readBuffer = new byte[readLen];
            _client.ReadWrite((uint)AdsReservedIndexGroup.SumCommandRead, (uint)symbols.Count, readBuffer, ms.ToArray());
            BinaryReader br = new BinaryReader(new MemoryStream(readBuffer));

            // read return values
            for (int i = 0; i < symbols.Count; i++)
            {
                UInt32 errcode = br.ReadUInt32();

                if (errcode != 0)
                {
                    throw new Exception("On Symbol " + _ctx.Symbols[i].Name + " errorcode: 0x" + errcode.ToString("0X8"));
                }
            }

            // read and print the values
            foreach (IAdsSymbol symbol in symbols)
            {
                switch (symbol.DataTypeId)
                {
                    case AdsDataTypeId.ADST_BIT:
                        if (br.ReadByte() > 0)
                            Console.WriteLine("true");
                        else
                            Console.WriteLine("false");
                        break;
                    case AdsDataTypeId.ADST_INT8:
                        Console.WriteLine(br.ReadSByte().ToString());
                        break;
                    case AdsDataTypeId.ADST_UINT8:
                        Console.WriteLine(br.ReadByte().ToString());
                        break;
                    case AdsDataTypeId.ADST_INT16:
                        Console.WriteLine(br.ReadInt16().ToString());
                        break;
                    case AdsDataTypeId.ADST_UINT16:
                        Console.WriteLine(br.ReadUInt16().ToString());
                        break;
                    case AdsDataTypeId.ADST_INT32:
                        Console.WriteLine(br.ReadInt32().ToString());
                        break;
                    case AdsDataTypeId.ADST_UINT32:
                        Console.WriteLine(br.ReadUInt32().ToString());
                        break;
                    case AdsDataTypeId.ADST_INT64:
                        Console.WriteLine(br.ReadInt64().ToString());
                        break;
                    case AdsDataTypeId.ADST_UINT64:
                        Console.WriteLine(br.ReadUInt64().ToString());
                        break;
                    case AdsDataTypeId.ADST_REAL32:
                        Console.WriteLine(br.ReadSingle().ToString());
                        break;
                    case AdsDataTypeId.ADST_REAL64:
                        Console.WriteLine(br.ReadDouble().ToString());
                        break;
                    case AdsDataTypeId.ADST_STRING:
                        byte[] zeroTermString = br.ReadBytes(symbol.ByteSize);
                        int i = 0;
                        foreach (byte b in zeroTermString)
                        {
                            if (b == 0)
                                break;
                            i++;
                        }
                        string s = Encoding.ASCII.GetString(zeroTermString, 0, i);
                        Console.WriteLine(s);
                        break;
                    case AdsDataTypeId.ADST_VOID:
                    case AdsDataTypeId.ADST_WSTRING:
                    case AdsDataTypeId.ADST_REAL80:
                    case AdsDataTypeId.ADST_MAXTYPES:
                    case AdsDataTypeId.ADST_BIGTYPE:
                        Console.WriteLine("Ads datatype {0} is not implemented", symbol.DataTypeId.ToString());
                        break;
                }
            }
        }
        #endregion
    }
}
