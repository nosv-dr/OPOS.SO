// code portions from http://monroecs.com/opos.htm
// also thanks to MSDN & RSDN

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using OposScanner_CCO;

namespace OPOSScannerSO
{
    [Guid("00000001-0001-0001-0001-000000000002")]
    [ComVisible(true), ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class Service : OPOSScannerServiceObject_Interface, IDisposable 
    {
        private IOPOSScanner FCO;
        private long deviceEnabled;
        private long dataEventEnabled;
        private long freezeEvents;
        private ManualResetEvent readEnabled;

        private ScannerReader scannerReader;

        public Service()
        {
            Thread.CurrentThread.TrySetApartmentState(ApartmentState.STA);

            State = OPOS_S_CLOSED;
            ResultCode = OPOS_E_CLOSED;
            PowerState = OPOS_PS_UNKNOWN;
            PowerNotify = OPOS_PN_DISABLED;

            readEnabled = new ManualResetEvent(false);
        }

        private void SetReadEnabled()
        {
            if (DeviceEnabled == 0 || FreezeEvents > 0 || DataEventEnabled == 0)
            {
                readEnabled.Reset();

                if (null != scannerReader)
                {
                    scannerReader.EnableRead = false;
                }
            }
            else
            {
                readEnabled.Set();

                if (null != scannerReader)
                {
                    scannerReader.EnableRead = true;
                }
            }
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
        
        #region Scanner methods

        internal void OnDataScanned(byte[] data)
        {
            Service_DataScanned(data);
        }

        internal void OnLogWrite(string message, EventLogEntryType type, int eventID, short category, Exception e)
        {
         /*   if (e != null)
            {
                this.EventLog.WriteEntry(message, type, eventID, category, Encoding.Default.GetBytes(e.ToString()));
            }
            else
            {
                this.EventLog.WriteEntry(message, type, eventID, category);
            }*/
        }

        internal void scannerReader_ThreadExceptionEvent(object sender, Exception e)
        {
            if (FreezeEvents == 0)
                FCO.SOError((int)OPOS_E_FAILURE, 0, 0, 0);
        }

        private void Service_DataScanned(byte[] data)
		{
            // Ignore input if we're in the Error state
            if (State == OPOS_S_ERROR)
            {
                return;
            }

			if (data.Length < 2)
			{
				FailedScan();
			}
			else
			{
                GoodScan(data);
			}
		}

        private string BinaryConvert(byte[] data, int index, int count)
        {
            byte[] datapart = new byte[count];
            int idx = 0;

            for (int i = index; i < index + count; i++)
            {
                datapart[idx] = data[i];
                idx++;
            }

            return BinaryConvert(datapart);
        }

        private string BinaryConvert(byte[] data)
        {
            string pData;
            int pCount = data.Length;

            switch (BinaryConversion)
            {
                case OPOS_BC_NONE:
                    pData = Encoding.ASCII.GetString(data);
                    break;
                case OPOS_BC_NIBBLE:
                    pData = "";
                    foreach (byte dataByte in data)
                    {
                        pData += (char)(((dataByte & 0xf0) >> 4) | 0x30) + "" + (char)((dataByte & 0x0f) | 0x30);
                    }
                    break;
                case OPOS_BC_DECIMAL:
                    pData = "";
                    foreach (byte dataByte in data)
                    {
                        pData += (((int)dataByte)).ToString().PadLeft(3);
                    }
                    break;
                default:
                    pData = Convert.ToBase64String(data);
                    break;
            }

            return pData;
        }

		private long GetSymbology(byte b)
		{			
			switch (b)
			{
				case 0x7A:
                    return SCAN_SDT_AZTEC;
				case 0x61:
                    return SCAN_SDT_Codabar;
				case 0x62:
                    return SCAN_SDT_Code39;
                case 0x6A:
                    return SCAN_SDT_Code128;
                case 0x69:
                    return SCAN_SDT_Code93;
                case 0x77:
                    return SCAN_SDT_DATAMATRIX;
                case 0x64:
                    return SCAN_SDT_EAN13;
                case 0x44:
                    return SCAN_SDT_EAN8;
                case 0x65:
                    return SCAN_SDT_ITF;
                case 0x78:
                    return SCAN_SDT_MAXICODE;
                case 0x52:
                    return SCAN_SDT_UPDF417;
                case 0x72:
                    return SCAN_SDT_PDF417;
                case 0x73:
                    return SCAN_SDT_QRCODE;
                case 0x63:
                    return SCAN_SDT_UPCA;
                case 0x45:
                    return SCAN_SDT_UPCE;
				default:
					break;
			}

            return SCAN_SDT_OTHER;
		}

        private long GetSymbology(byte[] b)
        {
            if (b.Length == 3 && b[0] == 0x5d)
            {
                switch (b[1])
                {
                    case 0x46:
                        return SCAN_SDT_Codabar;
                    case 0x41:
                        return SCAN_SDT_Code39;
                    case 0x43:
                        return SCAN_SDT_Code128;
                    case 0x47:
                        return SCAN_SDT_Code93;
                    case 0x49:
                        return SCAN_SDT_ITF;
                    case 0x4c:
                        return SCAN_SDT_PDF417;
                    case 0x45:
                        return SCAN_SDT_EAN13;
                    default:
                        break;
                }
            }

            return SCAN_SDT_OTHER;
        } 

		protected string DecodeScanDataLabel(byte [] scanData)
		{
            if (scanData.Length > 1)
            {
                if (scanData.Length > 3 && scanData[0] == 0x5d)
                {
                    return BinaryConvert(scanData, 3, scanData.Length - 3);
                }
                else
                {
                    return BinaryConvert(scanData, 1, scanData.Length - 1);
                }
            }

            return "";
		}

		protected long DecodeScanDataType(byte [] scanData)
		{
            if (scanData.Length > 1)
            {
                if (scanData[0] == 0x5d)
                {
                    return GetSymbology( new byte[] { scanData[0], scanData[1], scanData[2] });
                }
                else
                {
                    return GetSymbology(scanData[0]);
                }
            }
            else
                return SCAN_SDT_UNKNOWN;
		}
        
        private void GoodScan(byte[] scanData)
        {
            if (AutoDisable > 0)
            {
                DeviceEnabled = 0;
            }

            ScannedData data = new ScannedData();

            data.ScanData = BinaryConvert(scanData);

            if (DecodeData > 0)
            {
                data.ScanDataType = DecodeScanDataType(scanData);
                data.ScanDataLabel = DecodeScanDataLabel(scanData);
            }

            if (data.ScanData != "")
            {
                ScanData = data.ScanData;
                ScanDataLabel = data.ScanDataLabel;
                ScanDataType = data.ScanDataType;

                DataEventEnabled = 0;

                FCO.SOData(0);
            }
        }

        private void FailedScan()
        {
        }

        #endregion Scanner methods

        #region OPOSScannerServiceObject_Interface members

        public long COFreezeEvents(bool Freeze)
        {
            if (Freeze)
            {
                FreezeEvents = 1;
            }
            else
            {
                FreezeEvents = 0;
            }

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

                    case PIDXScan_DecodeData:
                        return DecodeData;

                    case PIDXScan_ScanDataType:
                        return ScanDataType;

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

                    case PIDXScan_DecodeData:
                        DecodeData = Number;
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

                    case PIDXScan_ScanData:
                        return ScanData;

                    case PIDXScan_ScanDataLabel:
                        return ScanDataLabel;

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
            if (DeviceClass != OPOS_CLASSKEY_SCAN)
                return OPOS_E_ILLEGAL;

            FCO = (IOPOSScanner)pDispatch;

            RegistryKey soKey = Registry.LocalMachine.OpenSubKey(OPOS_ROOTKEY + "\\" + OPOS_CLASSKEY_SCAN + "\\" + DeviceName);

            String DevicePath = "NULL";

            if (soKey.ValueCount > 0)
            {
                if (Array.Exists(soKey.GetValueNames(), element => element == "DevicePath"))
                {
                    DevicePath = (String)soKey.GetValue("DevicePath");
                }
                else if (Array.Exists(soKey.GetValueNames(), element => element == "PORT"))
                {
                    DevicePath = (String)soKey.GetValue("PORT");
                }
                else
                {
                    State = OPOS_S_ERROR;
                    return OPOS_E_FAILURE;
                }
            }

            if (!DevicePath.StartsWith("COM"))
            {
                State = OPOS_S_ERROR;
                return OPOS_E_ILLEGAL;
            }

            // init barcode reader
            try
            {
                scannerReader = new ScannerReader(DeviceName, DevicePath, OnDataScanned, OnLogWrite);

                scannerReader.ThreadExceptionEvent += new ScannerReader.ThreadExceptionEventHandler(scannerReader_ThreadExceptionEvent);

                scannerReader.Encoding = Encoding.ASCII;

                scannerReader.OpenDevice();
            }
            catch (Exception e)
            {
                throw e;
            }

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
                        CheckHealthText = "Device enabled";
                    }
                    else
                        return OPOS_E_OFFLINE;

                    break;

                case OPOS_CH_EXTERNAL:
                    return OPOS_E_ILLEGAL;

                case OPOS_CH_INTERACTIVE:
                    return OPOS_E_ILLEGAL;
            }

            return OPOS_SUCCESS;
        }

        public long ClaimDevice(long Timeout)
        {
            if (State != OPOS_S_IDLE)
            {
                return OPOS_E_ILLEGAL;
            }

            Claimed = 1;

            return OPOS_SUCCESS;
        }

        public long ClearInput()
        {
            scannerReader.ClearInput();

            return OPOS_SUCCESS;
        }

        public long CloseService()
        {
            scannerReader.CloseDevice();

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
            scannerReader.CloseDevice();

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

        #endregion OPOSScannerServiceObject_Interface members

        #region POS properties

        public long AutoDisable { get; set; }

        public long BinaryConversion { get; set; }

        public long CapPowerReporting { get { return OPOS_PR_NONE; } }

        public string CheckHealthText { get; private set; }

        public long Claimed { get; private set; }

        public long DataCount { get; private set; }

        public long DataEventEnabled { get { return dataEventEnabled; } set { dataEventEnabled = value; SetReadEnabled(); } }
        
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
                        scannerReader.CloseDevice();

                        ResultCode = OPOS_SUCCESS;
                    }
                    else
                    {
                        // Try to open the serial port
                        try
                        {
                            scannerReader.OpenDevice();
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
                            ResultCode = OPOS_SUCCESS;
                        }
                    }
                }
            }
        }

        public long FreezeEvents { get { return freezeEvents; } set { freezeEvents = value; SetReadEnabled(); } }

        public long OutputID { get; private set; }

        public long PowerNotify { get; set; }

        public long PowerState { get; private set; }

        public long ResultCode { get; private set; }

        public long ResultCodeExtended { get; private set; }

        public long State { get; private set; }

        public string ServiceObjectDescription { get { return "OPOS_Scanner SO"; } }

        public long ServiceObjectVersion { get { return 1012000; } }

        public string DeviceDescription { get { return "Service object for barcode scanner"; } }

        public string DeviceName { get { return "OPOS_Scanner"; } }

        public long CapStatisticsReporting { get { return 0; } }

        public long CapUpdateStatistics { get { return 0; } }

        public long CapCompareFirmwareVersion { get { return 0; } }

        public long CapUpdateFirmware { get { return 0; } }

        public long DecodeData { get; set; }

        public string ScanData { get; private set; }
        
        public string ScanDataLabel { get; private set; }

        public long ScanDataType { get; private set; }

        #endregion POS properties

        public struct ScannedData
        {
            public string ScanData;
            public string ScanDataLabel;
            public long ScanDataType;

            public ScannedData(string ScanData)
            {
                this.ScanData = ScanData;
                this.ScanDataLabel = "";
                this.ScanDataType = 0;
            }

            public ScannedData(string ScanData, string ScanDataLabel, long ScanDataType)
            {
                this.ScanData = ScanData;
                this.ScanDataLabel = ScanDataLabel;
                this.ScanDataType = ScanDataType;
            }
        }

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
        const string OPOS_CLASSKEY_CASH = "CashDrawer";
        const string OPOS_CLASSKEY_COIN = "CoinDispenser";
        const string OPOS_CLASSKEY_TOT = "HardTotals";
        const string OPOS_CLASSKEY_LOCK = "Keylock";
        const string OPOS_CLASSKEY_DISP = "LineDisplay";
        const string OPOS_CLASSKEY_MICR = "MICR";
        const string OPOS_CLASSKEY_MSR = "MSR";
        const string OPOS_CLASSKEY_PTR = "POSPrinter";
        const string OPOS_CLASSKEY_SCAL = "Scale";
        const string OPOS_CLASSKEY_SCAN = "Scanner";
        const string OPOS_CLASSKEY_SIG = "SignatureCapture";
        //   Release 1.1
        const string OPOS_CLASSKEY_KBD = "POSKeyboard";
        //   Release 1.2
        const string OPOS_CLASSKEY_CHAN = "CashChanger";
        const string OPOS_CLASSKEY_TONE = "ToneIndicator";
        //   Release 1.3
        const string OPOS_CLASSKEY_BB = "BumpBar";
        const string OPOS_CLASSKEY_FPTR = "FiscalPrinter";
        const string OPOS_CLASSKEY_PPAD = "PINPad";
        const string OPOS_CLASSKEY_ROD = "RemoteOrderDisplay";
        //   Release 1.4
        const string OPOS_CLASSKEY_CAT = "CAT";
        //   Release 1.5
        const string OPOS_CLASSKEY_PCRW = "PointCardRW";
        const string OPOS_CLASSKEY_PWR = "POSPower";
        //   Release 1.7
        const string OPOS_CLASSKEY_CHK = "CheckScanner";
        const string OPOS_CLASSKEY_MOTION = "MotionSensor";
        //   Release 1.8
        const string OPOS_CLASSKEY_SCRW = "SmartCardRW";
        //   Release 1.10
        const string OPOS_CLASSKEY_BIO = "Biometrics";
        const string OPOS_CLASSKEY_EJ = "ElectronicJournal";
        //   Release 1.11
        const string OPOS_CLASSKEY_BACC = "BillAcceptor";
        const string OPOS_CLASSKEY_BDSP = "BillDispenser";
        const string OPOS_CLASSKEY_CACC = "CoinAcceptor";
        const string OPOS_CLASSKEY_IMG = "ImageScanner";
        //   Release 1.12
        const string OPOS_CLASSKEY_BELT = "Belt";
        const string OPOS_CLASSKEY_EVRW = "ElectronicValueRW";
        const string OPOS_CLASSKEY_GATE = "Gate";
        const string OPOS_CLASSKEY_ITEM = "ItemDispenser";
        const string OPOS_CLASSKEY_LGT = "Lights";
        const string OPOS_CLASSKEY_RFID = "RFIDScanner";

        // OPOS_ROOTKEY_PROVIDER is the key under which a Service Object
        //   provider may place provider-specific information.
        //   (The base key is HKEY_LOCAL_MACHINE.)
        const string OPOS_ROOTKEY_PROVIDER = "SOFTWARE\\OLEforRetail\\ServiceInfo";

        #endregion Registry Keys

        //////////////////////////////////////////////////////////////////////
        // Common Property Base Index Values.
        //////////////////////////////////////////////////////////////////////

        #region Common Property Base Index Values

        // * Base Indices *

        const long PIDX_NUMBER = 0;
        const long PIDX_STRING = 1000000; // 1,000,000

        // * Range Test Functions *

        bool IsNumericPidx(long Pidx)
        {
            return (Pidx < PIDX_STRING) ? true : false;
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

        const long PIDX_Claimed = 1 + PIDX_NUMBER;
        const long PIDX_DataEventEnabled = 2 + PIDX_NUMBER;
        const long PIDX_DeviceEnabled = 3 + PIDX_NUMBER;
        const long PIDX_FreezeEvents = 4 + PIDX_NUMBER;
        const long PIDX_OutputID = 5 + PIDX_NUMBER;
        const long PIDX_ResultCode = 6 + PIDX_NUMBER;
        const long PIDX_ResultCodeExtended = 7 + PIDX_NUMBER;
        const long PIDX_ServiceObjectVersion = 8 + PIDX_NUMBER;
        const long PIDX_State = 9 + PIDX_NUMBER;

        //      Added for Release 1.2:
        const long PIDX_AutoDisable = 10 + PIDX_NUMBER;
        const long PIDX_BinaryConversion = 11 + PIDX_NUMBER;
        const long PIDX_DataCount = 12 + PIDX_NUMBER;

        //      Added for Release 1.3:
        const long PIDX_PowerNotify = 13 + PIDX_NUMBER;
        const long PIDX_PowerState = 14 + PIDX_NUMBER;


        // * Capabilities *

        //      Added for Release 1.3:
        const long PIDX_CapPowerReporting = 501 + PIDX_NUMBER;

        //      Added for Release 1.8:
        const long PIDX_CapStatisticsReporting = 502 + PIDX_NUMBER;
        const long PIDX_CapUpdateStatistics = 503 + PIDX_NUMBER;

        //      Added for Release 1.9:
        const long PIDX_CapCompareFirmwareVersion = 504 + PIDX_NUMBER;
        const long PIDX_CapUpdateFirmware = 505 + PIDX_NUMBER;


        //////////////////////////////////////////////////////////////////////
        // Common String Property Index Values.
        //////////////////////////////////////////////////////////////////////

        // * Properties *

        const long PIDX_CheckHealthText = 1 + PIDX_STRING;
        const long PIDX_DeviceDescription = 2 + PIDX_STRING;
        const long PIDX_DeviceName = 3 + PIDX_STRING;
        const long PIDX_ServiceObjectDescription = 4 + PIDX_STRING;

        //////////////////////////////////////////////////////////////////////
        // Numeric Property Index Values.
        //////////////////////////////////////////////////////////////////////

        // * Properties *

        const long PIDXScan_DecodeData = 1 + PIDX_SCAN + PIDX_NUMBER;
        const long PIDXScan_ScanDataType = 2 + PIDX_SCAN + PIDX_NUMBER;

        // * Capabilities *

        const long PIDXScan_ScanData = 1 + PIDX_SCAN + PIDX_STRING;
        const long PIDXScan_ScanDataLabel = 2 + PIDX_SCAN + PIDX_STRING;


        #endregion Common Property Base Index Values

        //////////////////////////////////////////////////////////////////////
        // Class Property Base Index Values.
        //////////////////////////////////////////////////////////////////////

        #region Class Property Base Index Values

        //   Release 1.0
        const long PIDX_CASH = 1000;  // Cash Drawer.
        const long PIDX_COIN = 2000;  // Coin Dispenser.
        const long PIDX_TOT = 3000;  // Hard Totals.
        const long PIDX_LOCK = 4000;  // Keylock.
        const long PIDX_DISP = 5000;  // Line Display.
        const long PIDX_MICR = 6000;  // Magnetic Ink Character Recognition.
        const long PIDX_MSR = 7000;  // Magnetic Stripe Reader.
        const long PIDX_PTR = 8000;  // POS Printer.
        const long PIDX_SCAL = 9000;  // Scale.
        const long PIDX_SCAN = 10000;  // Scanner - Bar Code Reader.
        const long PIDX_SIG = 11000;  // Signature Capture.
        //   Release 1.1
        const long PIDX_KBD = 12000;  // POS Keyboard.
        //   Release 1.2
        const long PIDX_CHAN = 13000;  // Cash Changer.
        const long PIDX_TONE = 14000;  // Tone Indicator.
        //   Release 1.3
        const long PIDX_BB = 15000;  // Bump Bar.
        const long PIDX_FPTR = 16000;  // Fiscal Printer.
        const long PIDX_PPAD = 17000;  // PIN Pad.
        const long PIDX_ROD = 18000;  // Remote Order Display.
        //   Release 1.4
        const long PIDX_CAT = 19000;  // CAT.
        //   Release 1.5
        const long PIDX_PCRW = 20000;  // Point Card Reader Writer.
        const long PIDX_PWR = 21000;  // POS Power.
        //   Release 1.7
        const long PIDX_CHK = 22000;  // Check Scanner.
        const long PIDX_MOTION = 23000;  // Motion Sensor.
        //   Release 1.8
        const long PIDX_SCRW = 24000;  // Smart Card Reader Writer.
        //   Release 1.10
        const long PIDX_BIO = 25000;  // Biometrics.
        const long PIDX_EJ = 26000;  // Electronic Journal.
        //   Release 1.11
        const long PIDX_BACC = 27000;  // Bill Acceptor.
        const long PIDX_BDSP = 28000;  // Bill Dispenser.
        const long PIDX_CACC = 29000;  // Coin Acceptor.
        const long PIDX_IMG = 30000;  // Image Scanner.
        //   Release 1.12
        const long PIDX_BELT = 31000;  // Belt.
        const long PIDX_EVRW = 32000;  // Electronic Value Reader Writer.
        const long PIDX_GATE = 33000;  // Gate.
        const long PIDX_ITEM = 34000;  // Item Dispenser.
        const long PIDX_LGT = 35000;  // Lights.
        const long PIDX_RFID = 36000;  // RFID Scanner.

        #endregion Class Property Base Index Values

        // Scanner constants -->

        /////////////////////////////////////////////////////////////////////
        // "ScanDataType" Property Constants (added in 1.2)
        /////////////////////////////////////////////////////////////////////

        // - One dimensional symbologies
        const long SCAN_SDT_UPCA = 101;  // Digits
        const long SCAN_SDT_UPCE = 102;  // Digits
        const long SCAN_SDT_JAN8 = 103;  // = EAN 8
        const long SCAN_SDT_EAN8 = 103;  // = JAN 8 (added in 1.2)
        const long SCAN_SDT_JAN13 = 104;  // = EAN 13
        const long SCAN_SDT_EAN13 = 104;  // = JAN 13 (added in 1.2)
        const long SCAN_SDT_TF = 105;  // (Discrete 2 of 5) Digits
        const long SCAN_SDT_ITF = 106;  // (Interleaved 2 of 5) Digits
        const long SCAN_SDT_Codabar = 107;  // Digits, -, $, :, /, ., +;
        //   4 start/stop characters
        //   (a, b, c, d)
        const long SCAN_SDT_Code39 = 108;  // Alpha, Digits, Space, -, .,
        //   $, /, +, %; start/stop (*)
        // Also has Full ASCII feature
        const long SCAN_SDT_Code93 = 109;  // Same characters as Code 39
        const long SCAN_SDT_Code128 = 110;  // 128 data characters

        const long SCAN_SDT_UPCA_S = 111;  // UPC-A with supplemental
        //   barcode
        const long SCAN_SDT_UPCE_S = 112;  // UPC-E with supplemental
        //   barcode
        const long SCAN_SDT_UPCD1 = 113;  // UPC-D1
        const long SCAN_SDT_UPCD2 = 114;  // UPC-D2
        const long SCAN_SDT_UPCD3 = 115;  // UPC-D3
        const long SCAN_SDT_UPCD4 = 116;  // UPC-D4
        const long SCAN_SDT_UPCD5 = 117;  // UPC-D5
        const long SCAN_SDT_EAN8_S = 118;  // EAN 8 with supplemental
        //   barcode
        const long SCAN_SDT_EAN13_S = 119;  // EAN 13 with supplemental
        //   barcode
        const long SCAN_SDT_EAN128 = 120;  // EAN 128
        const long SCAN_SDT_OCRA = 121;  // OCR "A"
        const long SCAN_SDT_OCRB = 122;  // OCR "B"

        // - One dimensional symbologies (added in 1.8)
        //        The following RSS constants deprecated in 1.12.
        //        Instead use the GS1DATABAR constants below.
        const long SCAN_SDT_RSS14 = 131;  // Reduced Space Symbology - 14 digit GTIN
        const long SCAN_SDT_RSS_EXPANDED = 132;  // RSS - 14 digit GTIN plus additional fields

        // - One dimensional symbologies (added in 1.12)
        const long SCAN_SDT_GS1DATABAR = 131;  // GS1 DataBar Omnidirectional (normal or stacked)
        const long SCAN_SDT_GS1DATABAR_E = 132;  // GS1 DataBar Expanded (normal or stacked)

        // - Composite Symbologies (added in 1.8)
        const long SCAN_SDT_CCA = 151;  // Composite Component A.
        const long SCAN_SDT_CCB = 152;  // Composite Component B.
        const long SCAN_SDT_CCC = 153;  // Composite Component C.

        // - Two dimensional symbologies
        const long SCAN_SDT_PDF417 = 201;
        const long SCAN_SDT_MAXICODE = 202;

        // - Two dimensional symbologies (added in 1.11)
        const long SCAN_SDT_DATAMATRIX = 203;  // Data Matrix
        const long SCAN_SDT_QRCODE = 204;  // QR Code
        const long SCAN_SDT_UQRCODE = 205;  // Micro QR Code
        const long SCAN_SDT_AZTEC = 206;  // Aztec
        const long SCAN_SDT_UPDF417 = 207;  // Micro PDF 417

        // - Special cases
        const long SCAN_SDT_OTHER = 501;  // Start of Scanner-Specific bar
        //   code symbologies
        const long SCAN_SDT_UNKNOWN = 0;  // Cannot determine the barcode
        //   symbology.

        #endregion OPOS constants
    }
}
