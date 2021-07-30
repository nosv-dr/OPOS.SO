// code portions from http://monroecs.com/opos.htm
// also thanks to MSDN & RSDN

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using OposSmartCardRW_CCO; // OPOSSmartCardRW.ocx

namespace OPOSSmartCardRWSO
{
    [Guid("12345678-1234-1234-1234-123456789ABC")]
    [ComVisible(true), ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class Service : OPOSSmartCardRWServiceObject_Interface, IDisposable
    {
        private IOPOSSmartCardRW FCO;
        private SerialPort scPort;
        private Queue<byte[]> dataQueue;
        private StringBuilder inputBuffer;
        private delegate void BufferAppendedHandler();
        private event BufferAppendedHandler BufferAppended;
        private ManualResetEvent cardInserted;
        private ManualResetEvent cardRemoved;
        private long deviceEnabled;


        public Service()
        {
            State = OPOS_S_CLOSED;
            ResultCode = OPOS_E_CLOSED;
            PowerState = OPOS_PS_UNKNOWN;
            PowerNotify = OPOS_PN_DISABLED;

            dataQueue = new Queue<byte[]>();

            inputBuffer = new StringBuilder();

            BufferAppended += new BufferAppendedHandler(SCardRW_BufferAppended);

            cardInserted = new ManualResetEvent(false);
            cardRemoved = new ManualResetEvent(false);
        }

        ~Service()
        {
            Dispose(false);
        }

        #region IDisposable Members

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                CloseService();
            }
            
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region OPOSSmartCardRWSO_Interface members

        public long COFreezeEvents(bool Freeze)
        {
            FreezeEvents = Freeze ? 1 : 0;

            return OPOS_SUCCESS;
        }

        public long GetPropertyNumber(long PropIndex)
        {
            if (IsNumericPidx(PropIndex))
            {
                switch (PropIndex)
                {
                    case PIDX_AutoDisable:
                        return AutoDisable;

                    case PIDX_BinaryConversion:
                        return BinaryConversion;

                    case PIDX_CapPowerReporting:
                        return CapPowerReporting;

                    case PIDX_Claimed:
                        return Claimed;
                    
                    case PIDX_DataCount:
                        return DataCount;

                    case PIDX_DataEventEnabled:
                        return DataEventEnabled;

                    case PIDX_DeviceEnabled:
                        return DeviceEnabled;

                    case PIDX_FreezeEvents:
                        return FreezeEvents;

                    case PIDX_OutputID:
                        return OutputID;

                    case PIDX_PowerNotify:
                        return PowerNotify;

                    case PIDX_PowerState:
                        return PowerState;

                    case PIDX_ResultCode:
                        return ResultCode;

                    case PIDX_ResultCodeExtended:
                        return ResultCodeExtended;
                    
                    case PIDX_State:
                        return State;

                    case PIDX_ServiceObjectVersion:
                        return ServiceObjectVersion;

                    case PIDX_CapStatisticsReporting:
                        return CapStatisticsReporting;

                    case PIDX_CapUpdateStatistics:
                        return CapUpdateStatistics;

                    case PIDX_CapCompareFirmwareVersion:
                        return CapCompareFirmwareVersion;

                    case PIDX_CapUpdateFirmware:
                        return CapUpdateFirmware;
                    
                    case PIDXScrw_CapCardErrorDetection:
                        return CapCardErrorDetection;

                    case PIDXScrw_CapInterfaceMode:
                        return CapInterfaceMode;

                    case PIDXScrw_CapIsoEmvMode:
                        return CapIsoEmvMode;

                    case PIDXScrw_CapSCPresentSensor:
                        return CapSCPresentSensor;

                    case PIDXScrw_CapSCSlots:
                        return CapSCSlots;

                    case PIDXScrw_CapTransmissionProtocol:
                        return CapTransmissionProtocol;

                    case PIDXScrw_InterfaceMode:
                        return InterfaceMode;

                    case PIDXScrw_IsoEmvMode:
                        return IsoEmvMode;

                    case PIDXScrw_SCPresentSensor:
                        return SCPresentSensor;

                    case PIDXScrw_SCSlot:
                        return SCSlot;

                    case PIDXScrw_TransactionInProgress:
                        return TransactionInProgress;

                    case PIDXScrw_TransmissionProtocol:
                        return TransmissionProtocol;

                    default:
                        break;
                }
            }
            return 0;
        }

        public void SetPropertyNumber(long PropIndex, long Number)
        {
            if (IsNumericPidx(PropIndex))
            {
                switch (PropIndex)
                {
                    case PIDX_AutoDisable:
                        AutoDisable = Number;
                        break;

                    case PIDX_BinaryConversion:
                        BinaryConversion = Number;
                        break;
                    
                    case PIDX_DataEventEnabled:
                        DataEventEnabled = Number;
                        break;

                    case PIDX_DeviceEnabled:
                        DeviceEnabled = Number;
                        break;

                    case PIDX_FreezeEvents:
                        FreezeEvents = Number;
                        break;

                    case PIDX_PowerNotify:
                        PowerNotify = Number;
                        break;

                    case PIDXScrw_InterfaceMode:
                        InterfaceMode = Number;
                        break;

                    case PIDXScrw_IsoEmvMode:
                        IsoEmvMode = Number;
                        break;

                    case PIDXScrw_SCSlot:
                        SCSlot = Number;
                        break;

                    default:
                        break;
                }
            }
        }

        public string GetPropertyString(long PropIndex)
        {
            if (IsStringPidx(PropIndex))
            {
                switch (PropIndex)
                {
                    case PIDX_CheckHealthText:
                        return CheckHealthText;

                    case PIDX_ServiceObjectDescription:
                        return ServiceObjectDescription;

                    case PIDX_DeviceDescription:
                        return DeviceDescription;

                    case PIDX_DeviceName:
                        return DeviceName;

                    default:
                        break;
                }
            }

            return "";
        }

        public void SetPropertyString(long PropIndex, string String)
        {
            if (IsStringPidx(PropIndex))
            {
                switch (PropIndex)
                {
                    case PIDX_CheckHealthText:
                        break; //CheckHealthText = String;

                    case PIDX_DeviceDescription:
                        break; //DeviceDescription = String;

                    case PIDX_DeviceName:
                        break; //DeviceName = String;

                    case PIDX_ServiceObjectDescription:
                        break; //ServiceObjectDescription = String;

                    default:
                        break;
                }
            }
        }

        public long OpenService(string DeviceClass, string DeviceName, object pDispatch)
        {
            if (DeviceClass != OPOS_CLASSKEY_SCRW)
                return OPOS_E_ILLEGAL;

            FCO = (IOPOSSmartCardRW)pDispatch;

            RegistryKey soKey = Registry.LocalMachine.OpenSubKey(OPOS_ROOTKEY + "\\" + OPOS_CLASSKEY_SCRW + "\\" + DeviceName);

            String DevicePath = "NULL";

            if (soKey.ValueCount > 0)
            {
                DevicePath = (String)soKey.GetValue("DevicePath");
            }

            if (!DevicePath.StartsWith("COM"))
                return OPOS_E_ILLEGAL;

            scPort = new SerialPort(DevicePath, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);

            scPort.Handshake = System.IO.Ports.Handshake.None;

            scPort.DataReceived += new SerialDataReceivedEventHandler(scPort_DataReceived);

            dataQueue.Clear();
            inputBuffer.Remove(0, inputBuffer.Length);
            cardInserted.Reset();
            cardRemoved.Reset();

            deviceEnabled = 0;

            SCPresentSensor = 0;

            State = OPOS_S_IDLE;

            return OPOS_SUCCESS;
        }

        public long CheckHealth(long Level)
        {
            switch (Level)
            {
                case OPOS_CH_INTERNAL:
                    if (deviceEnabled > 0)
                    {
                        if (!scPort.IsOpen)
                            return OPOS_E_OFFLINE;

                        CheckHealthText = scPort.PortName;
                    }
                    else
                        return OPOS_E_OFFLINE;

                    break;

                case OPOS_CH_EXTERNAL :                    
                    if (deviceEnabled > 0)
                    {
                        if (!scPort.IsOpen)
                            return OPOS_E_OFFLINE;

                        CheckHealthText = "";

                        scPort.WriteLine("i");

                        Thread.Sleep(1000);

                        if (CheckHealthText.Length == 0)
                            return OPOS_E_OFFLINE;
                    }
                    else
                        return OPOS_E_OFFLINE;

                    break;

                case OPOS_CH_INTERACTIVE :
                    return OPOS_E_ILLEGAL;
            }
            
            return OPOS_SUCCESS;
        }

        public long ClaimDevice(long Timeout)
        {
            Claimed = 1;
            return OPOS_SUCCESS;
        }

        public long ClearInput()
        {
            inputBuffer.Remove(0, inputBuffer.Length);

            dataQueue.Clear();
            
            return OPOS_SUCCESS;
        }

        public long ClearOutput()
        {
            return OPOS_SUCCESS;
        }

        public long CloseService()
        {
            if (scPort.IsOpen)
            {
                try
                {
                    // Close the serial port
                    scPort.Close();
                }
                catch
                {
                    return OPOS_E_FAILURE;
                }
            }

            SCPresentSensor = 0;

            Claimed = 0;
             
            State = OPOS_S_CLOSED;

            deviceEnabled = 0;

            return OPOS_SUCCESS;
        }

        public long DirectIO(long Command, ref long pData, out string pString)
        {
            pString = "";
            return OPOS_E_ILLEGAL;
        }

        public long ReleaseDevice()
        {
            SCPresentSensor = 0;

            Claimed = 0;

            deviceEnabled = 0;

            return OPOS_SUCCESS;
        }

        public long ResetStatistics(string StatisticsBuffer)
        {
            return OPOS_E_ILLEGAL;
        }

        public long RetrieveStatistics(ref string StatisticsBuffer)
        {
            return OPOS_E_ILLEGAL;
        }

        public long UpdateStatistics(string StatisticsBuffer)
        {
            return OPOS_E_ILLEGAL;
        }

        public long BeginInsertion(long Timeout)
        {
            cardInserted.Reset();

            if (SCPresentSensor != 0)
            {
                ResultCode = OPOS_E_ILLEGAL;
                if (FreezeEvents == 0)
                    FCO.SOError((int)OPOS_E_ILLEGAL, 0, 0, 0);

                return OPOS_E_ILLEGAL;
            }

            cardInserted.WaitOne((Int32)Timeout);

            if (SCPresentSensor == 0)
            {
                ResultCode = OPOS_E_TIMEOUT;
                if (FreezeEvents == 0)
                    FCO.SOError((int)OPOS_E_TIMEOUT, 0, 0, 0);

                return OPOS_E_TIMEOUT;
            }
            
            ResultCode = OPOS_SUCCESS;
            return OPOS_SUCCESS;
        }

        public long BeginRemoval(long Timeout)
        {
            cardRemoved.Reset();

            if (SCPresentSensor == 0)
            {
                ResultCode = OPOS_E_ILLEGAL;
                if (FreezeEvents == 0)
                    FCO.SOError((int)OPOS_E_ILLEGAL, 0, 0, 0);

                return OPOS_E_ILLEGAL;
            }

            cardRemoved.WaitOne((Int32)Timeout);

            if (SCPresentSensor != 0)
            {
                ResultCode = OPOS_E_TIMEOUT;
                if (FreezeEvents == 0)
                    FCO.SOError((int)OPOS_E_TIMEOUT, 0, 0, 0);

                return OPOS_E_TIMEOUT;
            }
            
            ResultCode = OPOS_SUCCESS;
            return OPOS_SUCCESS;
        }

        public long EndInsertion()
        {
            return OPOS_SUCCESS;
        }

        public long EndRemoval()
        {
            return OPOS_SUCCESS;
        }

        public long ReadData(long Action, ref long pCount, ref string pData)
        {
            if (Action != SC_READ_DATA)
            {
                return OPOS_E_ILLEGAL;
            }

            pCount = 0;
            pData = "";

            if (dataQueue.Count > 0)
            {
                Byte[] data = dataQueue.Dequeue();

                pCount = data.Length;

                switch (BinaryConversion)
                {
                    case OPOS_BC_NONE : 
                        pData = Encoding.Default.GetString(data);
                        break;
                    case OPOS_BC_NIBBLE:
                        pData = "";
                        foreach (Byte dataByte in data)
                        {
                            pData = "" + (Char)(((dataByte & 0xf0) >> 4) | 0x30) + (Char)((dataByte & 0x0f) | 0x30) + pData;
                        }
                        break;
                    case OPOS_BC_DECIMAL:
                        pData = "";
                        foreach (Byte dataByte in data)
                        {
                            pData = (((int)dataByte)).ToString().PadLeft(3) + pData;
                        }
                        break;
                    default :
                        pData = Convert.ToBase64String(data);
                        break;
                }
            }
                    
            return OPOS_SUCCESS;
        }

        public long WriteData(long Action, long Count, string Data)
        {
            return OPOS_E_ILLEGAL;
        }

        public long CompareFirmwareVersion(string FirmwareFileName, out long pResult)
        {
            pResult = OPOS_CFV_FIRMWARE_UNKNOWN;
            
            return OPOS_E_ILLEGAL;
        }

        public long UpdateFirmware(string FirmwareFileName)
        {
            return OPOS_E_ILLEGAL;
        }

        public long ClearInputProperties()
        {
            return OPOS_E_ILLEGAL;
        }

        #endregion OPOSSmartCardRWSO_Interface members

        #region Serial port methods

        void scPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Ignore input if we're in the Error state
            if (State == OPOS_S_ERROR)
            {
                return;
            }

            TransactionInProgress = 1;

            string readLine = scPort.ReadExisting();

            inputBuffer.Append(readLine);

            OnBufferAppended();
        }

        protected void OnBufferAppended()
        {
            if (BufferAppended != null)
                BufferAppended();
        }

        private void SCardRW_BufferAppended()
        {
            String[] lines = inputBuffer.ToString().Split(scPort.Encoding.GetChars(scPort.Encoding.GetBytes(scPort.NewLine)));

            inputBuffer.Remove(0, inputBuffer.Length - lines[lines.Length - 1].Length);

            for (int i = 0; i < lines.Length - 1; i++)
            {
                String line = lines[i];

                if (line.Contains("No card"))
                {
                    CardRemoved();
                    break;
                }
                else if (line.Contains("Em-Marin") || line.Contains("Mifare"))
                {
                    ParseReadLine(line);
                }
                else
                {
                    CheckHealthText += line + "\r\n";
                }
            }

            TransactionInProgress = 0;
        }

        private void CardRemoved()
        {
            if (FreezeEvents == 0)
                FCO.SOStatusUpdate((int)SC_SUE_NO_CARD);
            
            dataQueue.Clear();
            SCPresentSensor = 0;
            cardRemoved.Set();
        }

        private void ParseReadLine(String readLine)
        {
            Byte[] cardData = null;

            try
            {
                String[] split1 = readLine.Split(new Char[] { ' ', '\r', '\n' });

                if (split1.Length >= 2)
                {
                    String[] split2 = split1[1].Split(',');

                    if (split2.Length == 2)
                    {
                        UInt32 facility = (UInt32)(UInt16.Parse(split2[0]) << 16);
                        UInt16 number = UInt16.Parse(split2[1]);
                        UInt32 fullnumber = (UInt32)(facility + number);

                        cardData = BitConverter.GetBytes(fullnumber);
                    }
                }
            }
            catch
            {
            }            

            if (cardData.Length == 4)
            {
                dataQueue.Enqueue(cardData);

                if (FreezeEvents == 0 && DataEventEnabled > 0)
                    FCO.SOData(4);

                CardInserted();
            }
        }

        private void CardInserted()
        {
            if (FreezeEvents == 0)
                FCO.SOStatusUpdate((int)SC_SUE_CARD_PRESENT);

            SCPresentSensor = 1;
            cardInserted.Set();
        }

        #endregion Serial port methods

        #region POS properties

        public long AutoDisable { get; set; }

        public long BinaryConversion { get; set; }
        
        public long CapPowerReporting { get { return OPOS_PR_NONE; } }

        public string CheckHealthText { get; private set; }

        public long Claimed { get; private set; } 

        public long DataCount { get; private set; }

        public long DataEventEnabled { get; set; }

        public long DeviceEnabled 
        {
            // Device State checking done in base class
            get { return deviceEnabled; }
            set
            {
                if (value != deviceEnabled)
                {
                    // Call base class first because it will handle cases such as the
                    // device isn't claimed etc.
                    deviceEnabled = value;

                    // Start/Stop reading from the device
                    if (deviceEnabled == 0)
                    {
                        scPort.Close(); 
                        
                        if (scPort.IsOpen)
                        {
                            State = OPOS_S_ERROR;

                            ResultCode = OPOS_E_FAILURE;
                            if (FreezeEvents == 0)
                                FCO.SOError((int)OPOS_E_FAILURE, 0, 0, 0);
                        }
                        else
                        {
                            ResultCode = OPOS_SUCCESS;
                        }                     
                    }
                    else
                    {
                        // Try to open the serial port
                        try
                        {
                            scPort.Open();
                        }
                        catch
                        {
                            // disable device
                            deviceEnabled = 0;
                            State = OPOS_S_ERROR;

                            ResultCode = OPOS_E_NOHARDWARE;
                            if (FreezeEvents == 0)
                                FCO.SOError((int)OPOS_E_NOHARDWARE, 0, 0, 0);
                        }
                        finally
                        {
                            // If we have successfully opened the port, enable/disable buttons
                            if (scPort.IsOpen)
                            {
                                scPort.RtsEnable = true;
                                scPort.DtrEnable = true;

                                // Set read & write timeout
                                scPort.ReadTimeout = 0;
                                scPort.WriteTimeout = 5000;

                                ResultCode = OPOS_SUCCESS;
                            }
                            else
                            {
                                deviceEnabled = 0;
                                State = OPOS_S_ERROR;

                                ResultCode = OPOS_E_OFFLINE;
                                if (FreezeEvents == 0)
                                    FCO.SOError((int)OPOS_E_OFFLINE, 0, 0, 0);
                            }
                        }
                    }
                }
            }
        }

        public long FreezeEvents { get; set; }

        public long OutputID { get; private set; }

        public long PowerNotify { get; set; }
        
        public long PowerState { get; private set; }

        public long ResultCode { get; private set; }

        public long ResultCodeExtended { get; private set; }

        public long State { get; private set; }

        public string ServiceObjectDescription { get { return "IL_Z2USB SO"; } }
        
        public long ServiceObjectVersion { get { return 1013000; } }

        public string DeviceDescription { get { return "Service object for IronLogic Z2USB card reader"; } }

        public string DeviceName { get { return "IL_Z2USB"; } }

        public long CapStatisticsReporting { get { return 0; } }

        public long CapUpdateStatistics { get { return 0; } }

        public long CapCompareFirmwareVersion { get { return 0; } }

        public long CapUpdateFirmware { get { return 0; } }

        public long CapCardErrorDetection { get { return 0; } }

        public long CapInterfaceMode { get { return SC_CMODE_TRANS; } }

        public long CapIsoEmvMode { get { return SC_CMODE_ISO; } }

        public long CapSCPresentSensor { get { return 1; } }

        public long CapSCSlots { get { return 1; } }

        public long CapTransmissionProtocol { get { return SC_CTRANS_PROTOCOL_T0; } }

        public long InterfaceMode { get { return SC_MODE_TRANS; } set { } }

        public long IsoEmvMode { get { return SC_MODE_ISO; } set { } }  

        public long SCPresentSensor { get; private set; }

        public long SCSlot { get { return 1; } set { } }
     
        public long TransactionInProgress { get; private set; }

        public long TransmissionProtocol { get { return SC_TRANS_PROTOCOL_T0; } }
        
        #endregion POS properties

        #region OPOS constants

        /////////////////////////////////////////////////////////////////////
        // OPOS "State" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long OPOS_S_CLOSED = 1;
        const long OPOS_S_IDLE = 2;
        const long OPOS_S_BUSY = 3;
        const long OPOS_S_ERROR = 4;


        /////////////////////////////////////////////////////////////////////
        // OPOS "ResultCode" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long OPOS_SUCCESS = 0;
        const long OPOS_E_CLOSED = 101;
        const long OPOS_E_CLAIMED = 102;
        const long OPOS_E_NOTCLAIMED = 103;
        const long OPOS_E_NOSERVICE = 104;
        const long OPOS_E_DISABLED = 105;
        const long OPOS_E_ILLEGAL = 106;
        const long OPOS_E_NOHARDWARE = 107;
        const long OPOS_E_OFFLINE = 108;
        const long OPOS_E_NOEXIST = 109;
        const long OPOS_E_EXISTS = 110;
        const long OPOS_E_FAILURE = 111;
        const long OPOS_E_TIMEOUT = 112;
        const long OPOS_E_BUSY = 113;
        const long OPOS_E_EXTENDED = 114;
        const long OPOS_E_DEPRECATED = 115; // (added in 1.11)

        const long OPOSERR = 100; // Base for ResultCode errors.
        const long OPOSERREXT = 200; // Base for ResultCodeExtendedErrors.


        /////////////////////////////////////////////////////////////////////
        // OPOS "ResultCodeExtended" Property Constants
        /////////////////////////////////////////////////////////////////////

        // The following applies to ResetStatistics and UpdateStatistics.
        const long OPOS_ESTATS_ERROR = 280; // (added in 1.8)
        const long OPOS_ESTATS_DEPENDENCY = 282; // (added in 1.10)
        // The following applies to CompareFirmwareVersion and UpdateFirmware.
        const long OPOS_EFIRMWARE_BAD_FILE = 281; // (added in 1.9)


        /////////////////////////////////////////////////////////////////////
        // OPOS "OpenResult" Property Constants (added in 1.5)
        /////////////////////////////////////////////////////////////////////

        // The following can be set by the control object.
        //  - Control Object already open.
        const long OPOS_OR_ALREADYOPEN = 301;
        //  - The registry does not contain a key for the specified device name.
        const long OPOS_OR_REGBADNAME = 302;
        //  - Could not read the device name key's default value, or
        //     could not convert this Prog ID to a valid Class ID.
        const long OPOS_OR_REGPROGID = 303;
        //  - Could not create a service object instance, or
        //     could not get its IDispatch interface.
        const long OPOS_OR_CREATE = 304;
        //  - The service object does not support one or more of the
        //     method required by its release.
        const long OPOS_OR_BADIF = 305;
        //  - The service object returned a failure status from its
        //     open call, but doesn't have a more specific failure code.
        const long OPOS_OR_FAILEDOPEN = 306;
        //  - The service object major version number is not 1.
        const long OPOS_OR_BADVERSION = 307;

        // The following can be returned by the service object if it
        // returns a failure status from its open call.
        //  - Port access required at open, but configured port
        //     is invalid or inaccessible.
        const long OPOS_ORS_NOPORT = 401;
        //  - Service Object does not support the specified device.
        const long OPOS_ORS_NOTSUPPORTED = 402;
        //  - Configuration information error.
        const long OPOS_ORS_CONFIG = 403;
        //  - Errors greater than this value are SO-specific.
        const long OPOS_ORS_SPECIFIC = 450;


        /////////////////////////////////////////////////////////////////////
        // OPOS "BinaryConversion" Property Constants (added in 1.2)
        /////////////////////////////////////////////////////////////////////

        const long OPOS_BC_NONE = 0;
        const long OPOS_BC_NIBBLE = 1;
        const long OPOS_BC_DECIMAL = 2;


        /////////////////////////////////////////////////////////////////////
        // "CheckHealth" Method: "Level" Parameter Constants
        /////////////////////////////////////////////////////////////////////

        const long OPOS_CH_INTERNAL = 1;
        const long OPOS_CH_EXTERNAL = 2;
        const long OPOS_CH_INTERACTIVE = 3;


        /////////////////////////////////////////////////////////////////////
        // OPOS "CapPowerReporting", "PowerState", "PowerNotify" Property
        //   Constants (added in 1.3)
        /////////////////////////////////////////////////////////////////////

        const long OPOS_PR_NONE = 0;
        const long OPOS_PR_STANDARD = 1;
        const long OPOS_PR_ADVANCED = 2;

        const long OPOS_PN_DISABLED = 0;
        const long OPOS_PN_ENABLED = 1;

        const long OPOS_PS_UNKNOWN = 2000;
        const long OPOS_PS_ONLINE = 2001;
        const long OPOS_PS_OFF = 2002;
        const long OPOS_PS_OFFLINE = 2003;
        const long OPOS_PS_OFF_OFFLINE = 2004;


        /////////////////////////////////////////////////////////////////////
        // "CompareFirmwareVersion" Method: "Result" Parameter Constants
        //   (added in 1.9)
        /////////////////////////////////////////////////////////////////////

        const long OPOS_CFV_FIRMWARE_OLDER = 1;
        const long OPOS_CFV_FIRMWARE_SAME = 2;
        const long OPOS_CFV_FIRMWARE_NEWER = 3;
        const long OPOS_CFV_FIRMWARE_DIFFERENT = 4;
        const long OPOS_CFV_FIRMWARE_UNKNOWN = 5;


        /////////////////////////////////////////////////////////////////////
        // "ErrorEvent" Event: "ErrorLocus" Parameter Constants
        /////////////////////////////////////////////////////////////////////

        const long OPOS_EL_OUTPUT = 1;
        const long OPOS_EL_INPUT = 2;
        const long OPOS_EL_INPUT_DATA = 3;


        /////////////////////////////////////////////////////////////////////
        // "ErrorEvent" Event: "ErrorResponse" Constants
        /////////////////////////////////////////////////////////////////////

        const long OPOS_ER_RETRY = 11;
        const long OPOS_ER_CLEAR = 12;
        const long OPOS_ER_CONTINUEINPUT = 13;


        /////////////////////////////////////////////////////////////////////
        // "StatusUpdateEvent" Event: Common "Status" Constants
        /////////////////////////////////////////////////////////////////////

        const long OPOS_SUE_POWER_ONLINE = 2001; // (added in 1.3)
        const long OPOS_SUE_POWER_OFF = 2002; // (added in 1.3)
        const long OPOS_SUE_POWER_OFFLINE = 2003; // (added in 1.3)
        const long OPOS_SUE_POWER_OFF_OFFLINE = 2004; // (added in 1.3)

        const long OPOS_SUE_UF_PROGRESS = 2100; // (added in 1.9)
        const long OPOS_SUE_UF_COMPLETE = 2200; // (added in 1.9)
        const long OPOS_SUE_UF_COMPLETE_DEV_NOT_RESTORED = 2205; // (added in 1.9)
        const long OPOS_SUE_UF_FAILED_DEV_OK = 2201; // (added in 1.9)
        const long OPOS_SUE_UF_FAILED_DEV_UNRECOVERABLE = 2202; // (added in 1.9)
        const long OPOS_SUE_UF_FAILED_DEV_NEEDS_FIRMWARE = 2203; // (added in 1.9)
        const long OPOS_SUE_UF_FAILED_DEV_UNKNOWN = 2204; // (added in 1.9)


        /////////////////////////////////////////////////////////////////////
        // General Constants
        /////////////////////////////////////////////////////////////////////

        const long OPOS_FOREVER = -1; // (added in 1.2)


        //////////////////////////////////////////////////////////////////////
        // Registry Keys
        //////////////////////////////////////////////////////////////////////

        #region Registry Keys

        // OPOS_ROOTKEY is the key under which all OPOS Service Object keys
        //   and values are placed.  This key will contain device class keys.
        //   (The base key is HKEY_LOCAL_MACHINE.)
        const string OPOS_ROOTKEY = "SOFTWARE\\OLEforRetail\\ServiceOPOS";

        // Device Class Keys
        //   Release 1.0
        const string  OPOS_CLASSKEY_CASH  =  "CashDrawer";
        const string  OPOS_CLASSKEY_COIN  =  "CoinDispenser";
        const string  OPOS_CLASSKEY_TOT   =  "HardTotals";
        const string  OPOS_CLASSKEY_LOCK  =  "Keylock";
        const string  OPOS_CLASSKEY_DISP  =  "LineDisplay";
        const string  OPOS_CLASSKEY_MICR  =  "MICR";
        const string  OPOS_CLASSKEY_MSR   =  "MSR";
        const string  OPOS_CLASSKEY_PTR   =  "POSPrinter";
        const string  OPOS_CLASSKEY_SCAL  =  "Scale";
        const string  OPOS_CLASSKEY_SCAN  =  "Scanner";
        const string  OPOS_CLASSKEY_SIG   =   "SignatureCapture";
        //   Release 1.1
        const string  OPOS_CLASSKEY_KBD   =  "POSKeyboard";
        //   Release 1.2
        const string  OPOS_CLASSKEY_CHAN  =  "CashChanger";
        const string  OPOS_CLASSKEY_TONE  =  "ToneIndicator";
        //   Release 1.3
        const string  OPOS_CLASSKEY_BB    =   "BumpBar";
        const string  OPOS_CLASSKEY_FPTR  =  "FiscalPrinter";
        const string  OPOS_CLASSKEY_PPAD  =   "PINPad";
        const string  OPOS_CLASSKEY_ROD   =   "RemoteOrderDisplay";
        //   Release 1.4
        const string  OPOS_CLASSKEY_CAT   =   "CAT";
        //   Release 1.5
        const string  OPOS_CLASSKEY_PCRW  =  "PointCardRW";
        const string  OPOS_CLASSKEY_PWR   =  "POSPower";
        //   Release 1.7
        const string  OPOS_CLASSKEY_CHK   =  "CheckScanner";
        const string  OPOS_CLASSKEY_MOTION = "MotionSensor";
        //   Release 1.8
        const string  OPOS_CLASSKEY_SCRW  =  "SmartCardRW";
        //   Release 1.10
        const string  OPOS_CLASSKEY_BIO  =   "Biometrics";
        const string  OPOS_CLASSKEY_EJ   =   "ElectronicJournal";
        //   Release 1.11
        const string  OPOS_CLASSKEY_BACC =   "BillAcceptor";
        const string  OPOS_CLASSKEY_BDSP  =  "BillDispenser";
        const string  OPOS_CLASSKEY_CACC  =  "CoinAcceptor";
        const string  OPOS_CLASSKEY_IMG   =  "ImageScanner";
        //   Release 1.12
        const string  OPOS_CLASSKEY_BELT  =  "Belt";
        const string  OPOS_CLASSKEY_EVRW  =  "ElectronicValueRW";
        const string  OPOS_CLASSKEY_GATE  =  "Gate";
        const string  OPOS_CLASSKEY_ITEM  =  "ItemDispenser";
        const string  OPOS_CLASSKEY_LGT   =  "Lights";
        const string  OPOS_CLASSKEY_RFID  =  "RFIDScanner";

        // OPOS_ROOTKEY_PROVIDER is the key under which a Service Object
        //   provider may place provider-specific information.
        //   (The base key is HKEY_LOCAL_MACHINE.)
        const string  OPOS_ROOTKEY_PROVIDER = "SOFTWARE\\OLEforRetail\\ServiceInfo";

        #endregion Registry Keys

        //////////////////////////////////////////////////////////////////////
        // Common Property Base Index Values.
        //////////////////////////////////////////////////////////////////////

        #region Common Property Base Index Values

        // * Base Indices *

        const long PIDX_NUMBER                  =        0;
        const long PIDX_STRING                  =  1000000; // 1,000,000

        // * Range Test Functions *

        bool IsNumericPidx(long Pidx)
        {
          return ( Pidx < PIDX_STRING ) ? true : false;
        }
        bool IsStringPidx(long Pidx)
        {
            return (Pidx >= PIDX_STRING) ? true : false;
        }

        // **Warning**
        //   OPOS property indices may not be changed by future releases.
        //   New indices must be added sequentially at the end of the
        //     numeric, capability, and string sections.

        // Note: The ControlObjectDescription and ControlObjectVersion
        //   properties are processed entirely by the CO. Therefore, no
        //   property index values are required below.


        //////////////////////////////////////////////////////////////////////
        // Common Numeric Property Index Values.
        //////////////////////////////////////////////////////////////////////

        // * Properties *

        const long PIDX_Claimed                 =   1 + PIDX_NUMBER;
        const long PIDX_DataEventEnabled        =   2 + PIDX_NUMBER;
        const long PIDX_DeviceEnabled           =   3 + PIDX_NUMBER;
        const long PIDX_FreezeEvents            =   4 + PIDX_NUMBER;
        const long PIDX_OutputID                =   5 + PIDX_NUMBER;
        const long PIDX_ResultCode              =   6 + PIDX_NUMBER;
        const long PIDX_ResultCodeExtended      =   7 + PIDX_NUMBER;
        const long PIDX_ServiceObjectVersion    =   8 + PIDX_NUMBER;
        const long PIDX_State                   =   9 + PIDX_NUMBER;

        //      Added for Release 1.2:
        const long PIDX_AutoDisable             =  10 + PIDX_NUMBER;
        const long PIDX_BinaryConversion        =  11 + PIDX_NUMBER;
        const long PIDX_DataCount               =  12 + PIDX_NUMBER;

        //      Added for Release 1.3:
        const long PIDX_PowerNotify             =  13 + PIDX_NUMBER;
        const long PIDX_PowerState              =  14 + PIDX_NUMBER;


        // * Capabilities *

        //      Added for Release 1.3:
        const long PIDX_CapPowerReporting       = 501 + PIDX_NUMBER;

        //      Added for Release 1.8:
        const long PIDX_CapStatisticsReporting  = 502 + PIDX_NUMBER;
        const long PIDX_CapUpdateStatistics     = 503 + PIDX_NUMBER;

        //      Added for Release 1.9:
        const long PIDX_CapCompareFirmwareVersion = 504 + PIDX_NUMBER;
        const long PIDX_CapUpdateFirmware       = 505 + PIDX_NUMBER;


        //////////////////////////////////////////////////////////////////////
        // Common String Property Index Values.
        //////////////////////////////////////////////////////////////////////

        // * Properties *

        const long PIDX_CheckHealthText             =   1 + PIDX_STRING;
        const long PIDX_DeviceDescription           =   2 + PIDX_STRING;
        const long PIDX_DeviceName                  =   3 + PIDX_STRING;
        const long PIDX_ServiceObjectDescription    =   4 + PIDX_STRING;

        //////////////////////////////////////////////////////////////////////
        // Numeric Property Index Values.
        //////////////////////////////////////////////////////////////////////

        // * Properties *

        const long PIDXScrw_InterfaceMode           = 1 + PIDX_SCRW + PIDX_NUMBER;
        const long PIDXScrw_IsoEmvMode              = 2 + PIDX_SCRW + PIDX_NUMBER;
        const long PIDXScrw_SCPresentSensor         = 3 + PIDX_SCRW + PIDX_NUMBER;
        const long PIDXScrw_SCSlot                  = 4 + PIDX_SCRW + PIDX_NUMBER;
        const long PIDXScrw_TransactionInProgress   = 5 + PIDX_SCRW + PIDX_NUMBER;
        const long PIDXScrw_TransmissionProtocol    = 6 + PIDX_SCRW + PIDX_NUMBER;


        // * Capabilities *

        const long PIDXScrw_CapCardErrorDetection   = 501 + PIDX_SCRW + PIDX_NUMBER;
        const long PIDXScrw_CapInterfaceMode        = 502 + PIDX_SCRW + PIDX_NUMBER;
        const long PIDXScrw_CapIsoEmvMode           = 503 + PIDX_SCRW + PIDX_NUMBER;
        const long PIDXScrw_CapSCPresentSensor      = 504 + PIDX_SCRW + PIDX_NUMBER;
        const long PIDXScrw_CapSCSlots              = 505 + PIDX_SCRW + PIDX_NUMBER;
        const long PIDXScrw_CapTransmissionProtocol = 506 + PIDX_SCRW + PIDX_NUMBER;
        
        #endregion Common Property Base Index Values

        //////////////////////////////////////////////////////////////////////
        // Class Property Base Index Values.
        //////////////////////////////////////////////////////////////////////

        #region Class Property Base Index Values

        //   Release 1.0
        const long PIDX_CASH    =  1000;  // Cash Drawer.
        const long PIDX_COIN    =  2000;  // Coin Dispenser.
        const long PIDX_TOT     =  3000;  // Hard Totals.
        const long PIDX_LOCK    =  4000;  // Keylock.
        const long PIDX_DISP    =  5000;  // Line Display.
        const long PIDX_MICR    =  6000;  // Magnetic Ink Character Recognition.
        const long PIDX_MSR     =  7000;  // Magnetic Stripe Reader.
        const long PIDX_PTR     =  8000;  // POS Printer.
        const long PIDX_SCAL    =  9000;  // Scale.
        const long PIDX_SCAN    = 10000;  // Scanner - Bar Code Reader.
        const long PIDX_SIG     = 11000;  // Signature Capture.
        //   Release 1.1
        const long PIDX_KBD     = 12000;  // POS Keyboard.
        //   Release 1.2
        const long PIDX_CHAN    = 13000;  // Cash Changer.
        const long PIDX_TONE    = 14000;  // Tone Indicator.
        //   Release 1.3
        const long PIDX_BB      = 15000;  // Bump Bar.
        const long PIDX_FPTR    = 16000;  // Fiscal Printer.
        const long PIDX_PPAD    = 17000;  // PIN Pad.
        const long PIDX_ROD     = 18000;  // Remote Order Display.
        //   Release 1.4
        const long PIDX_CAT     = 19000;  // CAT.
        //   Release 1.5
        const long PIDX_PCRW    = 20000;  // Point Card Reader Writer.
        const long PIDX_PWR     = 21000;  // POS Power.
        //   Release 1.7
        const long PIDX_CHK     = 22000;  // Check Scanner.
        const long PIDX_MOTION  = 23000;  // Motion Sensor.
        //   Release 1.8
        const long PIDX_SCRW    = 24000;  // Smart Card Reader Writer.
        //   Release 1.10
        const long PIDX_BIO     = 25000;  // Biometrics.
        const long PIDX_EJ      = 26000;  // Electronic Journal.
        //   Release 1.11
        const long PIDX_BACC    = 27000;  // Bill Acceptor.
        const long PIDX_BDSP    = 28000;  // Bill Dispenser.
        const long PIDX_CACC    = 29000;  // Coin Acceptor.
        const long PIDX_IMG     = 30000;  // Image Scanner.
        //   Release 1.12
        const long PIDX_BELT    = 31000;  // Belt.
        const long PIDX_EVRW    = 32000;  // Electronic Value Reader Writer.
        const long PIDX_GATE    = 33000;  // Gate.
        const long PIDX_ITEM    = 34000;  // Item Dispenser.
        const long PIDX_LGT     = 35000;  // Lights.
        const long PIDX_RFID    = 36000;  // RFID Scanner.

        #endregion Class Property Base Index Values

        // SmartCardRW constants -->

        /////////////////////////////////////////////////////////////////////
        // "CapInterfaceMode" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long SC_CMODE_TRANS = 1;  // Simple Transaction Command and Data Mode.
        const long SC_CMODE_BLOCK = 2;  // Block Data Mode.
        const long SC_CMODE_APDU = 4;  // Same as Block Data Mode except APDU Standard Commands are used.
        const long SC_CMODE_XML = 8;  // XML Block Data Mode.


        /////////////////////////////////////////////////////////////////////
        // "CapIsoEmvMode" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long SC_CMODE_ISO = 1;  // APDU messaging format conforms to the ISO standard.
        const long SC_CMODE_EMV = 2;  // APDU messaging format conforms to the EMV standard.


        /////////////////////////////////////////////////////////////////////
        // "CapTransmissionProtocol" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long SC_CTRANS_PROTOCOL_T0 = 1;  // Asynchronous, Half Duplex, Character, Transmission Protocol Mode.
        const long SC_CTRANS_PROTOCOL_T1 = 2;  // Asynchronous, Half Duplex, Block Transmission Protocol Mode.


        /////////////////////////////////////////////////////////////////////
        // "InterfaceMode" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long SC_MODE_TRANS = 1;  // Simple Transaction Command and Data Mode.
        const long SC_MODE_BLOCK = 2;  // Block Data Mode.
        const long SC_MODE_APDU = 4;  // Same as Block Data Mode except APDU Standard Defines the Commands and data.
        const long SC_MODE_XML = 8;  // XML Block Data Mode.


        /////////////////////////////////////////////////////////////////////
        // "IsoEmvMode" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long SC_MODE_ISO = 1;  // APDU messaging format currently in use conforms to the ISO standard.
        const long SC_MODE_EMV = 2;  // APDU messaging format currently in use conforms to the EMV standard.


        /////////////////////////////////////////////////////////////////////
        // "TransmissionProtocol" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long SC_TRANS_PROTOCOL_T0 = 1;  // Asynchronous, Half Duplex, Character, Transmission Protocol Mode.
        const long SC_TRANS_PROTOCOL_T1 = 2;  // Asynchronous, Half Duplex, Block Transmission Protocol Mode.


        /////////////////////////////////////////////////////////////////////
        // "ReadData" Method: "Action" Parameter Constants
        /////////////////////////////////////////////////////////////////////

        const long SC_READ_DATA = 11;
        const long SC_READ_PROGRAM = 12;
        const long SC_EXECUTE_AND_READ_DATA = 13;
        const long SC_XML_READ_BLOCK_DATA = 14;


        /////////////////////////////////////////////////////////////////////
        // "WriteData" Method: "Action" Parameter Constants
        /////////////////////////////////////////////////////////////////////

        const long SC_STORE_DATA = 21;
        const long SC_STORE_PROGRAM = 22;
        const long SC_EXECUTE_DATA = 23;
        const long SC_XML_BLOCK_DATA = 24;
        const long SC_SECURITY_FUSE = 25;
        const long SC_RESET = 26;


        /////////////////////////////////////////////////////////////////////
        // "StatusUpdateEvent" Event: "Data" Parameter Constant
        /////////////////////////////////////////////////////////////////////

        const long SC_SUE_NO_CARD = 1;  // No card detected in the SCR/W Device.
        const long SC_SUE_CARD_PRESENT = 2;  // There is a card in the device.


        /////////////////////////////////////////////////////////////////////
        // "ErrorEvent" Event: "ResultCodeExtended" Parameter Constants
        /////////////////////////////////////////////////////////////////////

        const long OPOS_ESC_READ = 201;  // There was a read error.
        const long OPOS_ESC_WRITE = 202;  // There was a write error.
        const long OPOS_ESC_TORN = 203;  // The smart card was prematurely removed from the SCR/W unexpectedly. Note: CapCardErrorDetection capability must be true before this can be set.
        const long OPOS_ESC_NO_CARD = 204;  // There is no card detected in the SCR/W but a card was expected.

        #endregion OPOS constants
        
    }
}
