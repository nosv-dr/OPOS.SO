using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using OposPOSPrinter_CCO;

namespace OPOSPOSPrinterSO
{
    [Guid("12345678-1234-1234-1234-123456789ABC"), ProgId("VirtualOPOS.POSPrinter")]
    [ComVisible(true), ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class Service : OPOSPOSPrinterServiceObject_Interface, IDisposable
    {
        private string OutputFileName;

        private IOPOSPOSPrinter FCO;
        private long deviceEnabled;
        private long freezeEvents;

        public Service()
        {
            Thread.CurrentThread.TrySetApartmentState(ApartmentState.STA);

            State = OPOS_S_CLOSED;
            ResultCode = OPOS_E_CLOSED;
            PowerState = OPOS_PS_UNKNOWN;
            PowerNotify = OPOS_PN_DISABLED;
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

        private void LoadParameters(RegistryKey soKey)
        {
            CapCharacterSet = (long)soKey.GetValue("CapCharacterSet", 0L);
            CapConcurrentJrnRec = (long)soKey.GetValue("CapConcurrentJrnRec", 0L);
            CapConcurrentJrnSlp = (long)soKey.GetValue("CapConcurrentJrnSlp", 0L);
            CapConcurrentPageMode = (long)soKey.GetValue("CapConcurrentPageMode", 0L);
            CapConcurrentRecSlp = (long)soKey.GetValue("CapConcurrentRecSlp", 0L);
            CapCoverSensor = (long)soKey.GetValue("CapCoverSensor", 0L);
            CapJrn2Color = (long)soKey.GetValue("CapJrn2Color", 0L);
            CapJrnBold = (long)soKey.GetValue("CapJrnBold", 0L);
            CapJrnCartridgeSensor = (long)soKey.GetValue("CapJrnCartridgeSensor", 0L);
            CapJrnColor = (long)soKey.GetValue("CapJrnColor", 0L);
            CapJrnDhigh = (long)soKey.GetValue("CapJrnDhigh", 0L);
            CapJrnDwide = (long)soKey.GetValue("CapJrnDwide", 0L);
            CapJrnDwideDhigh = (long)soKey.GetValue("CapJrnDwideDhigh", 0L);
            CapJrnEmptySensor = (long)soKey.GetValue("CapJrnEmptySensor", 0L);
            CapJrnItalic = (long)soKey.GetValue("CapJrnItalic", 0L);
            CapJrnNearEndSensor = (long)soKey.GetValue("CapJrnNearEndSensor", 0L);
            CapJrnPresent = (long)soKey.GetValue("CapJrnPresent", 0L);
            CapJrnUnderline = (long)soKey.GetValue("CapJrnUnderline", 0L);
            CapMapCharacterSet = (long)soKey.GetValue("CapMapCharacterSet", 0L);
            CapRec2Color = (long)soKey.GetValue("CapRec2Color", 0L);
            CapRecBarCode = (long)soKey.GetValue("CapRecBarCode", 0L);
            CapRecBitmap = (long)soKey.GetValue("CapRecBitmap", 0L);
            CapRecBold = (long)soKey.GetValue("CapRecBold", 0L);
            CapRecCartridgeSensor = (long)soKey.GetValue("CapRecCartridgeSensor", 0L);
            CapRecColor = (long)soKey.GetValue("CapRecColor", 0L);
            CapRecDhigh = (long)soKey.GetValue("CapRecDhigh", 0L);
            CapRecDwide = (long)soKey.GetValue("CapRecDwide", 0L);
            CapRecDwideDhigh = (long)soKey.GetValue("CapRecDwideDhigh", 0L);
            CapRecEmptySensor = (long)soKey.GetValue("CapRecEmptySensor", 0L);
            CapRecItalic = (long)soKey.GetValue("CapRecItalic", 0L);
            CapRecLeft90 = (long)soKey.GetValue("CapRecLeft90", 0L);
            CapRecMarkFeed = (long)soKey.GetValue("CapRecMarkFeed", 0L);
            CapRecNearEndSensor = (long)soKey.GetValue("CapRecNearEndSensor", 0L);
            CapRecPageMode = (long)soKey.GetValue("CapRecPageMode", 0L);
            CapRecPapercut = (long)soKey.GetValue("CapRecPapercut", 0L);
            CapRecPresent = (long)soKey.GetValue("CapRecPresent", 0L);
            CapRecRight90 = (long)soKey.GetValue("CapRecRight90", 0L);
            CapRecRotate180 = (long)soKey.GetValue("CapRecRotate180", 0L);
            CapRecRuledLine = (long)soKey.GetValue("CapRecRuledLine", 0L);
            CapSlp2Color = (long)soKey.GetValue("CapSlp2Color", 0L);
            CapSlpBarCode = (long)soKey.GetValue("CapSlpBarCode", 0L);
            CapSlpBitmap = (long)soKey.GetValue("CapSlpBitmap", 0L);
            CapSlpBold = (long)soKey.GetValue("CapSlpBold", 0L);
            CapSlpBothSidesPrint = (long)soKey.GetValue("CapSlpBothSidesPrint", 0L);
            CapSlpCartridgeSensor = (long)soKey.GetValue("CapSlpCartridgeSensor", 0L);
            CapSlpColor = (long)soKey.GetValue("CapSlpColor", 0L);
            CapSlpDhigh = (long)soKey.GetValue("CapSlpDhigh", 0L);
            CapSlpDwide = (long)soKey.GetValue("CapSlpDwide", 0L);
            CapSlpDwideDhigh = (long)soKey.GetValue("CapSlpDwideDhigh", 0L);
            CapSlpEmptySensor = (long)soKey.GetValue("CapSlpEmptySensor", 0L);
            CapSlpFullslip = (long)soKey.GetValue("CapSlpFullslip", 0L);
            CapSlpItalic = (long)soKey.GetValue("CapSlpItalic", 0L);
            CapSlpLeft90 = (long)soKey.GetValue("CapSlpLeft90", 0L);
            CapSlpNearEndSensor = (long)soKey.GetValue("CapSlpNearEndSensor", 0L);
            CapSlpPageMode = (long)soKey.GetValue("CapSlpPageMode", 0L);
            CapSlpPresent = (long)soKey.GetValue("CapSlpPresent", 0L);
            CapSlpRight90 = (long)soKey.GetValue("CapSlpRight90", 0L);
            CapSlpRotate180 = (long)soKey.GetValue("CapSlpRotate180", 0L);
            CapSlpRuledLine = (long)soKey.GetValue("CapSlpRuledLine", 0L);
            CapSlpUnderline = (long)soKey.GetValue("CapSlpUnderline", 0L);
            CapTransaction = (long)soKey.GetValue("CapTransaction", 0L);
        }

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
                    case PIDX_BinaryConversion:
                        return BinaryConversion;

                    case PIDX_CapCompareFirmwareVersion:
                        return CapCompareFirmwareVersion;

                    case PIDX_CapPowerReporting:
                        return CapPowerReporting;

                    case PIDX_CapStatisticsReporting:
                        return CapStatisticsReporting;

                    case PIDX_CapUpdateStatistics:
                        return CapUpdateStatistics;

                    case PIDX_CapUpdateFirmware:
                        return CapUpdateFirmware;

                    case PIDX_Claimed:
                        return Claimed;

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

                    // Printer specific

                    case PIDXPtr_AsyncMode:
                        return AsyncMode;

                    case PIDXPtr_CapCharacterSet:
                        return CapCharacterSet;

                    case PIDXPtr_CapConcurrentJrnRec:
                        return CapConcurrentJrnRec;

                    case PIDXPtr_CapConcurrentJrnSlp:
                        return CapConcurrentJrnSlp;

                    case PIDXPtr_CapConcurrentPageMode:
                        return CapConcurrentPageMode;

                    case PIDXPtr_CapConcurrentRecSlp:
                        return CapConcurrentRecSlp;

                    case PIDXPtr_CapCoverSensor:
                        return CapCoverSensor;

                    case PIDXPtr_CapJrn2Color:
                        return CapJrn2Color;

                    case PIDXPtr_CapJrnBold:
                        return CapJrnBold;

                    case PIDXPtr_CapJrnCartridgeSensor:
                        return CapJrnCartridgeSensor;

                    case PIDXPtr_CapJrnColor:
                        return CapJrnColor;

                    case PIDXPtr_CapJrnDhigh:
                        return CapJrnDhigh;

                    case PIDXPtr_CapJrnDwide:
                        return CapJrnDwide;

                    case PIDXPtr_CapJrnDwideDhigh:
                        return CapJrnDwideDhigh;

                    case PIDXPtr_CapJrnEmptySensor:
                        return CapJrnEmptySensor;

                    case PIDXPtr_CapJrnItalic:
                        return CapJrnItalic;

                    case PIDXPtr_CapJrnNearEndSensor:
                        return CapJrnNearEndSensor;

                    case PIDXPtr_CapJrnPresent:
                        return CapJrnPresent;

                    case PIDXPtr_CapJrnUnderline:
                        return CapJrnUnderline;

                    case PIDXPtr_CapMapCharacterSet:
                        return CapMapCharacterSet;

                    case PIDXPtr_CapRec2Color:
                        return CapRec2Color;

                    case PIDXPtr_CapRecBarCode:
                        return CapRecBarCode;

                    case PIDXPtr_CapRecBitmap:
                        return CapRecBitmap;

                    case PIDXPtr_CapRecBold:
                        return CapRecBold;

                    case PIDXPtr_CapRecCartridgeSensor:
                        return CapRecCartridgeSensor;

                    case PIDXPtr_CapRecColor:
                        return CapRecColor;

                    case PIDXPtr_CapRecDhigh:
                        return CapRecDhigh;

                    case PIDXPtr_CapRecDwide:
                        return CapRecDwide;

                    case PIDXPtr_CapRecDwideDhigh:
                        return CapRecDwideDhigh;

                    case PIDXPtr_CapRecEmptySensor:
                        return CapRecEmptySensor;

                    case PIDXPtr_CapRecItalic:
                        return CapRecItalic;

                    case PIDXPtr_CapRecLeft90:
                        return CapRecLeft90;

                    case PIDXPtr_CapRecMarkFeed:
                        return CapRecMarkFeed;

                    case PIDXPtr_CapRecNearEndSensor:
                        return CapRecNearEndSensor;

                    case PIDXPtr_CapRecPageMode:
                        return CapRecPageMode;

                    case PIDXPtr_CapRecPapercut:
                        return CapRecPapercut;

                    case PIDXPtr_CapRecPresent:
                        return CapRecPresent;

                    case PIDXPtr_CapRecRight90:
                        return CapRecRight90;

                    case PIDXPtr_CapRecRotate180:
                        return CapRecRotate180;

                    case PIDXPtr_CapRecRuledLine:
                        return CapRecRuledLine;

                    case PIDXPtr_CapSlp2Color:
                        return CapSlp2Color;

                    case PIDXPtr_CapSlpBarCode:
                        return CapSlpBarCode;

                    case PIDXPtr_CapSlpBitmap:
                        return CapSlpBitmap;

                    case PIDXPtr_CapSlpBold:
                        return CapSlpBold;

                    case PIDXPtr_CapSlpBothSidesPrint:
                        return CapSlpBothSidesPrint;

                    case PIDXPtr_CapSlpCartridgeSensor:
                        return CapSlpCartridgeSensor;

                    case PIDXPtr_CapSlpColor:
                        return CapSlpColor;

                    case PIDXPtr_CapSlpDhigh:
                        return CapSlpDhigh;

                    case PIDXPtr_CapSlpDwide:
                        return CapSlpDwide;

                    case PIDXPtr_CapSlpDwideDhigh:
                        return CapSlpDwideDhigh;

                    case PIDXPtr_CapSlpEmptySensor:
                        return CapSlpEmptySensor;

                    case PIDXPtr_CapSlpFullslip:
                        return CapSlpFullslip;

                    case PIDXPtr_CapSlpItalic:
                        return CapSlpItalic;

                    case PIDXPtr_CapSlpLeft90:
                        return CapSlpLeft90;

                    case PIDXPtr_CapSlpNearEndSensor:
                        return CapSlpNearEndSensor;

                    case PIDXPtr_CapSlpPageMode:
                        return CapSlpPageMode;

                    case PIDXPtr_CapSlpPresent:
                        return CapSlpPresent;

                    case PIDXPtr_CapSlpRight90:
                        return CapSlpRight90;

                    case PIDXPtr_CapSlpRotate180:
                        return CapSlpRotate180;

                    case PIDXPtr_CapSlpRuledLine:
                        return CapSlpRuledLine;

                    case PIDXPtr_CapSlpUnderline:
                        return CapSlpUnderline;

                    case PIDXPtr_CapTransaction:
                        return CapTransaction;

                    case PIDXPtr_CartridgeNotify:
                        return CartridgeNotify;

                    case PIDXPtr_CharacterSet:
                        return CharacterSet;

                    case PIDXPtr_CharacterSetList:
                        return CharacterSetList;

                    case PIDXPtr_CoverOpen:
                        return CoverOpen;

                    case PIDXPtr_ErrorLevel:
                        return ErrorLevel;

                    case PIDXPtr_ErrorStation:
                        return ErrorStation;

                    case PIDXPtr_FlagWhenIdle:
                        return FlagWhenIdle;

                    case PIDXPtr_JrnCartridgeState:
                        return JrnCartridgeState;

                    case PIDXPtr_JrnCurrentCartridge:
                        return JrnCurrentCartridge;

                    case PIDXPtr_JrnEmpty:
                        return JrnEmpty;

                    case PIDXPtr_JrnLetterQuality:
                        return JrnLetterQuality;

                    case PIDXPtr_JrnLineChars:
                        return JrnLineChars;

                    case PIDXPtr_JrnLineHeight:
                        return JrnLineHeight;

                    case PIDXPtr_JrnLineSpacing:
                        return JrnLineSpacing;

                    case PIDXPtr_JrnLineWidth:
                        return JrnLineWidth;

                    case PIDXPtr_JrnNearEnd:
                        return JrnNearEnd;

                    case PIDXPtr_MapCharacterSet:
                        return MapCharacterSet;

                    case PIDXPtr_MapMode:
                        return MapMode;

                    case PIDXPtr_PageModeDescriptor:
                        return PageModeDescriptor;

                    case PIDXPtr_PageModeHorizontalPosition:
                        return PageModeHorizontalPosition;

                    case PIDXPtr_PageModePrintDirection:
                        return PageModePrintDirection;

                    case PIDXPtr_PageModeStation:
                        return PageModeStation;

                    case PIDXPtr_PageModeVerticalPosition:
                        return PageModeVerticalPosition;

                    case PIDXPtr_RecCartridgeState:
                        return RecCartridgeState;

                    case PIDXPtr_RecCurrentCartridge:
                        return RecCurrentCartridge;

                    case PIDXPtr_RecEmpty:
                        return RecEmpty;

                    case PIDXPtr_RecLetterQuality:
                        return RecLetterQuality;

                    case PIDXPtr_RecLineChars:
                        return RecLineChars;

                    case PIDXPtr_RecLineHeight:
                        return RecLineHeight;

                    case PIDXPtr_RecLineSpacing:
                        return RecLineSpacing;

                    case PIDXPtr_RecLineWidth:
                        return RecLineWidth;

                    case PIDXPtr_RecLinesToPaperCut:
                        return RecLinesToPaperCut;

                    case PIDXPtr_RecNearEnd:
                        return RecNearEnd;

                    case PIDXPtr_RecSidewaysMaxChars:
                        return RecSidewaysMaxChars;

                    case PIDXPtr_RecSidewaysMaxLines:
                        return RecSidewaysMaxLines;

                    case PIDXPtr_RotateSpecial:
                        return RotateSpecial;

                    case PIDXPtr_SlpCartridgeState:
                        return SlpCartridgeState;

                    case PIDXPtr_SlpCurrentCartridge:
                        return SlpCurrentCartridge;

                    case PIDXPtr_SlpEmpty:
                        return SlpEmpty;

                    case PIDXPtr_SlpLetterQuality:
                        return SlpLetterQuality;

                    case PIDXPtr_SlpLineChars:
                        return SlpLineChars;

                    case PIDXPtr_SlpLineHeight:
                        return SlpLineHeight;

                    case PIDXPtr_SlpLineSpacing:
                        return SlpLineSpacing;

                    case PIDXPtr_SlpLineWidth:
                        return SlpLineWidth;

                    case PIDXPtr_SlpLinesNearEndToEnd:
                        return SlpLinesNearEndToEnd;

                    case PIDXPtr_SlpMaxLines:
                        return SlpMaxLines;

                    case PIDXPtr_SlpNearEnd:
                        return SlpNearEnd;

                    case PIDXPtr_SlpPrintSide:
                        return SlpPrintSide;

                    case PIDXPtr_SlpSidewaysMaxChars:
                        return SlpSidewaysMaxChars;

                    case PIDXPtr_SlpSidewaysMaxLines:
                        return SlpSidewaysMaxLines;

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
                    case PIDX_BinaryConversion:
                        BinaryConversion = Number;
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

                    // Printer specific

                    case PIDXPtr_AsyncMode:
                        AsyncMode = Number;
                        break;

                    case PIDXPtr_CartridgeNotify:
                        CartridgeNotify = Number;
                        break;

                    case PIDXPtr_CharacterSet:
                        CharacterSet = Number;
                        break;

                    case PIDXPtr_FlagWhenIdle:
                        FlagWhenIdle = Number;
                        break;

                    case PIDXPtr_JrnCurrentCartridge:
                        JrnCurrentCartridge = Number;
                        break;

                    case PIDXPtr_JrnLetterQuality:
                        JrnLetterQuality = Number;
                        break;

                    case PIDXPtr_JrnLineChars:
                        JrnLineChars = Number;
                        break;

                    case PIDXPtr_JrnLineHeight:
                        JrnLineHeight = Number;
                        break;

                    case PIDXPtr_JrnLineSpacing:
                        JrnLineSpacing = Number;
                        break;

                    case PIDXPtr_MapCharacterSet:
                        MapCharacterSet = Number;
                        break;

                    case PIDXPtr_MapMode:
                        MapMode = Number;
                        break;

                    case PIDXPtr_PageModeHorizontalPosition:
                        PageModeHorizontalPosition = Number;
                        break;

                    case PIDXPtr_PageModePrintDirection:
                        PageModePrintDirection = Number;
                        break;

                    case PIDXPtr_PageModeStation:
                        PageModeStation = Number;
                        break;

                    case PIDXPtr_PageModeVerticalPosition:
                        PageModeVerticalPosition = Number;
                        break;

                    case PIDXPtr_RecCurrentCartridge:
                        RecCurrentCartridge = Number;
                        break;

                    case PIDXPtr_RecLetterQuality:
                        RecLetterQuality = Number;
                        break;

                    case PIDXPtr_RecLineChars:
                        RecLineChars = Number;
                        break;

                    case PIDXPtr_RecLineHeight:
                        RecLineHeight = Number;
                        break;

                    case PIDXPtr_RecLineSpacing:
                        RecLineSpacing = Number;
                        break;

                    case PIDXPtr_RotateSpecial:
                        RotateSpecial = Number;
                        break;

                    case PIDXPtr_SlpCurrentCartridge:
                        SlpCurrentCartridge = Number;
                        break;

                    case PIDXPtr_SlpLetterQuality:
                        SlpLetterQuality = Number;
                        break;

                    case PIDXPtr_SlpLineChars:
                        SlpLineChars = Number;
                        break;

                    case PIDXPtr_SlpLineHeight:
                        SlpLineHeight = Number;
                        break;

                    case PIDXPtr_SlpLineSpacing:
                        SlpLineSpacing = Number;
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

                    // Printer specific

                    case PIDXPtr_ErrorString:
                        return ErrorString;

                    case PIDXPtr_FontTypefaceList:
                        return FontTypefaceList;

                    case PIDXPtr_JrnLineCharsList:
                        return JrnLineCharsList;

                    case PIDXPtr_PageModeArea:
                        return PageModeArea;

                    case PIDXPtr_PageModePrintArea:
                        return PageModePrintArea;

                    case PIDXPtr_RecBarCodeRotationList:
                        return RecBarCodeRotationList;

                    case PIDXPtr_RecBitmapRotationList:
                        return RecBitmapRotationList;

                    case PIDXPtr_RecLineCharsList:
                        return RecLineCharsList;

                    case PIDXPtr_SlpBarCodeRotationList:
                        return SlpBarCodeRotationList;

                    case PIDXPtr_SlpBitmapRotationList:
                        return SlpBitmapRotationList;

                    case PIDXPtr_SlpLineCharsList:
                        return SlpLineCharsList;

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

                    // Printer specific

                    case PIDXPtr_PageModePrintArea:
                        PageModePrintArea = String;
                        break;

                    default:
                        break;
                }
            }
        }

        public long OpenService(string DeviceClass, string DeviceName, object pDispatch)
        {
            if (!string.Equals(DeviceClass, OPOS_CLASSKEY_PTR, System.StringComparison.OrdinalIgnoreCase))
                return OPOS_E_ILLEGAL;

            FCO = (IOPOSPOSPrinter)pDispatch;

            String DevicePath = "NULL";

            try
            {
                using (var soKey = Registry.LocalMachine.OpenSubKey(OPOS_ROOTKEY + "\\" + OPOS_CLASSKEY_PTR + "\\" + DeviceName))
                {
                    if (soKey.ValueCount > 0)
                    {
                        if (Array.Exists(soKey.GetValueNames(), element => string.Equals(element, "DevicePath", System.StringComparison.OrdinalIgnoreCase)))
                        {
                            DevicePath = (String)soKey.GetValue("DevicePath");
                        }
                        else
                        {
                            State = OPOS_S_ERROR;
                            return OPOS_E_FAILURE;
                        }

                        LoadParameters(soKey);
                    }
                }
            }
            catch (Exception ex)
            {
                State = OPOS_S_ERROR;
                ErrorString = ex.ToString();
                return OPOS_E_FAILURE;
            }
            
            if (string.IsNullOrEmpty(DevicePath))
            {
                State = OPOS_S_ERROR;
                return OPOS_E_ILLEGAL;
            }

            // init barcode reader
            try
            {
                OutputFileName = DevicePath;

                File.AppendAllText(OutputFileName, "service initialized" + Environment.NewLine);
            }
            catch (Exception e)
            {
                throw e;
            }

            State = OPOS_S_IDLE;

            ResultCode = OPOS_SUCCESS;

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

        public long ClearOutput()
        {
            // do nothing

            return OPOS_SUCCESS;
        }

        public long ClearPrintArea()
        {
            return OPOS_E_ILLEGAL;
        }

        public long CloseService()
        {
            // do nothing

            Claimed = 0;

            State = OPOS_S_CLOSED;

            ResultCode = OPOS_E_CLOSED;

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
            // do nothing

            Claimed = 0;

            deviceEnabled = 0;

            return OPOS_SUCCESS;
        }

        public long BeginInsertion(long Timeout)
        {
            return OPOS_E_ILLEGAL;
        }

        public long BeginRemoval(long Timeout)
        {
            return OPOS_E_ILLEGAL;
        }

        public long CutPaper(long Percentage)
        {
            return OPOS_SUCCESS;
        }

        public long EndInsertion()
        {
            return OPOS_E_ILLEGAL;
        }

        public long EndRemoval()
        {
            return OPOS_E_ILLEGAL;
        }

        public long PrintBarCode(long Station, string Data, long Symbology, long Height, long Width, long Alignment, long TextPosition)
        {
            return OPOS_E_ILLEGAL;
        }

        public long PrintBitmap(long Station, string FileName, long Width, long Alignment)
        {
            return OPOS_E_ILLEGAL;
        }

        public long PrintImmediate(long Station, string Data)
        {
            return OPOS_E_ILLEGAL;
        }

        public long PrintNormal(long Station, string Data)
        {
            File.AppendAllText(OutputFileName, Data + Environment.NewLine);

            return OPOS_SUCCESS;
        }

        public long PrintTwoNormal(long Stations, string Data1, string Data2)
        {
            return OPOS_E_ILLEGAL;
        }

        public long RotatePrint(long Station, long Rotation)
        {
            return OPOS_E_ILLEGAL;
        }

        public long SetBitmap(long BitmapNumber, long Station, string FileName, long Width, long Alignment)
        {
            return OPOS_E_ILLEGAL;
        }

        public long SetLogo(long Location, string Data)
        {
            return OPOS_E_ILLEGAL;
        }

        public long TransactionPrint(long Station, long Control)
        {
            return OPOS_E_ILLEGAL;
        }

        public long ValidateData(long Station, string Data)
        {
            return OPOS_E_ILLEGAL;
        }

        public long ChangePrintSide(long Side)
        {
            return OPOS_E_ILLEGAL;
        }

        public long MarkFeed(long Type)
        {
            return OPOS_E_ILLEGAL;
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

        public long PageModePrint(long Control)
        {
            return OPOS_E_ILLEGAL;
        }

        public long PrintMemoryBitmap(long Station, string Data, long Type, long Width, long Alignment)
        {
            return OPOS_E_ILLEGAL;
        }

        public long DrawRuledLine(long Station, string PositionList, long LineDirection, long LineWidth, long LineStyle, long LineColor)
        {
            return OPOS_E_ILLEGAL;
        }

        #endregion OPOSScannerServiceObject_Interface members

        #region POS properties

        public long BinaryConversion { get; set; }

        public long CapPowerReporting { get { return OPOS_PR_NONE; } }

        public string CheckHealthText { get; private set; }

        public long Claimed { get; private set; }

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
                        // do nothing

                        ResultCode = OPOS_SUCCESS;
                    }
                    else
                    {
                        // Try to open the serial port
                        try
                        {
                            // do nothing
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

        public long FreezeEvents { get { return freezeEvents; } set { freezeEvents = value; } }

        public long OutputID { get; private set; }

        public long PowerNotify { get; set; }

        public long PowerState { get; private set; }

        public long ResultCode { get; private set; }

        public long ResultCodeExtended { get; private set; }

        public long State { get; private set; }

        public string ServiceObjectDescription { get { return "OPOS_POSPrinter SO"; } }

        public long ServiceObjectVersion { get { return 1014000; } }

        public string DeviceDescription { get { return "Service object for printer"; } }

        public string DeviceName { get { return "OPOS_POSPrinter"; } }

        public long CapStatisticsReporting { get { return 0; } }

        public long CapUpdateStatistics { get { return 0; } }

        public long CapCompareFirmwareVersion { get { return 0; } }

        public long CapUpdateFirmware { get { return 0; } }

        // Printer specific

        public long AsyncMode { get; private set; }

        public long CapCharacterSet { get; private set; }
        public long CapConcurrentJrnRec { get; private set; }
        public long CapConcurrentJrnSlp { get; private set; }
        public long CapConcurrentPageMode { get; private set; }
        public long CapConcurrentRecSlp { get; private set; }
        public long CapCoverSensor { get; private set; }
        public long CapJrn2Color { get; private set; }
        public long CapJrnBold { get; private set; }
        public long CapJrnCartridgeSensor { get; private set; }
        public long CapJrnColor { get; private set; }
        public long CapJrnDhigh { get; private set; }
        public long CapJrnDwide { get; private set; }
        public long CapJrnDwideDhigh { get; private set; }
        public long CapJrnEmptySensor { get; private set; }
        public long CapJrnItalic { get; private set; }
        public long CapJrnNearEndSensor { get; private set; }
        public long CapJrnPresent { get; private set; }
        public long CapJrnUnderline { get; private set; }
        public long CapMapCharacterSet { get; private set; }
        public long CapRec2Color { get; private set; }
        public long CapRecBarCode { get; private set; }
        public long CapRecBitmap { get; private set; }
        public long CapRecBold { get; private set; }
        public long CapRecCartridgeSensor { get; private set; }
        public long CapRecColor { get; private set; }
        public long CapRecDhigh { get; private set; }
        public long CapRecDwide { get; private set; }
        public long CapRecDwideDhigh { get; private set; }
        public long CapRecEmptySensor { get; private set; }
        public long CapRecItalic { get; private set; }
        public long CapRecLeft90 { get; private set; }
        public long CapRecMarkFeed { get; private set; }
        public long CapRecNearEndSensor { get; private set; }
        public long CapRecPageMode { get; private set; }
        public long CapRecPapercut { get; private set; }
        public long CapRecPresent { get; private set; }
        public long CapRecRight90 { get; private set; }
        public long CapRecRotate180 { get; private set; }
        public long CapRecRuledLine { get; private set; }
        public long CapSlp2Color { get; private set; }
        public long CapSlpBarCode { get; private set; }
        public long CapSlpBitmap { get; private set; }
        public long CapSlpBold { get; private set; }
        public long CapSlpBothSidesPrint { get; private set; }
        public long CapSlpCartridgeSensor { get; private set; }
        public long CapSlpColor { get; private set; }
        public long CapSlpDhigh { get; private set; }
        public long CapSlpDwide { get; private set; }
        public long CapSlpDwideDhigh { get; private set; }
        public long CapSlpEmptySensor { get; private set; }
        public long CapSlpFullslip { get; private set; }
        public long CapSlpItalic { get; private set; }
        public long CapSlpLeft90 { get; private set; }
        public long CapSlpNearEndSensor { get; private set; }
        public long CapSlpPageMode { get; private set; }
        public long CapSlpPresent { get; private set; }
        public long CapSlpRight90 { get; private set; }
        public long CapSlpRotate180 { get; private set; }
        public long CapSlpRuledLine { get; private set; }
        public long CapSlpUnderline { get; private set; }
        public long CapTransaction { get; private set; }

        public long CartridgeNotify { get; private set; }

        public long CharacterSet { get; private set; }

        public long CharacterSetList { get; private set; }

        public long CoverOpen { get; private set; }

        public long ErrorLevel { get; private set; }

        public long ErrorStation { get; private set; }

        public string ErrorString { get; private set; }

        public string FontTypefaceList { get; private set; }

        public long FlagWhenIdle { get; private set; }

        public long JrnCartridgeState { get; private set; }

        public long JrnCurrentCartridge { get; private set; }

        public long JrnEmpty { get; private set; }

        public long JrnLetterQuality { get; private set; }

        public long JrnLineChars { get; private set; }

        public string JrnLineCharsList { get; private set; }        

        public long JrnLineHeight { get; private set; }

        public long JrnLineSpacing { get; private set; }

        public long JrnLineWidth { get; private set; }

        public long JrnNearEnd { get; private set; }

        public long MapCharacterSet { get; private set; }

        public long MapMode { get; private set; }

        public string PageModeArea { get; private set; }

        public long PageModeDescriptor { get; private set; }

        public long PageModeHorizontalPosition { get; private set; }

        public string PageModePrintArea { get; private set; }

        public long PageModePrintDirection { get; private set; }

        public long PageModeStation { get; private set; }

        public long PageModeVerticalPosition { get; private set; }

        public string RecBarCodeRotationList { get; private set; }

        public string RecBitmapRotationList { get; private set; }

        public long RecCartridgeState { get; private set; }

        public long RecCurrentCartridge { get; private set; }

        public long RecEmpty { get; private set; }

        public long RecLetterQuality { get; private set; }

        public long RecLineChars { get; private set; }

        public string RecLineCharsList { get; private set; }

        public long RecLineHeight { get; private set; }

        public long RecLineSpacing { get; private set; }

        public long RecLineWidth { get; private set; }

        public long RecLinesToPaperCut { get; private set; }

        public long RecNearEnd { get; private set; }

        public long RecSidewaysMaxChars { get; private set; }

        public long RecSidewaysMaxLines { get; private set; }

        public long RotateSpecial { get; private set; }

        public string  SlpBarCodeRotationList { get; private set; }
            
        public string SlpBitmapRotationList { get; private set; }

        public long SlpCartridgeState { get; private set; }

        public long SlpCurrentCartridge { get; private set; }

        public long SlpEmpty { get; private set; }

        public long SlpLetterQuality { get; private set; }

        public long SlpLineChars { get; private set; }

        public string SlpLineCharsList { get; private set; }

        public long SlpLineHeight { get; private set; }

        public long SlpLineSpacing { get; private set; }

        public long SlpLineWidth { get; private set; }

        public long SlpLinesNearEndToEnd { get; private set; }

        public long SlpMaxLines { get; private set; }

        public long SlpNearEnd { get; private set; }

        public long SlpPrintSide { get; private set; }

        public long SlpSidewaysMaxChars { get; private set; }

        public long SlpSidewaysMaxLines { get; private set; }
        
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
        //const long PIDX_SCAN = 10000;  // Scanner - Bar Code Reader.
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
        
        //////////////////////////////////////////////////////////////////////
        // Numeric Property Index Values.
        //////////////////////////////////////////////////////////////////////

        // * Properties *

        const long PIDXPtr_AsyncMode = 1 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CharacterSet = 2 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CoverOpen = 3 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_ErrorStation = 4 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_FlagWhenIdle = 5 + PIDX_PTR + PIDX_NUMBER;

        const long PIDXPtr_JrnEmpty = 6 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_JrnLetterQuality = 7 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_JrnLineChars = 8 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_JrnLineHeight = 9 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_JrnLineSpacing = 10 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_JrnLineWidth = 11 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_JrnNearEnd = 12 + PIDX_PTR + PIDX_NUMBER;

        const long PIDXPtr_MapMode = 13 + PIDX_PTR + PIDX_NUMBER;

        const long PIDXPtr_RecEmpty = 14 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_RecLetterQuality = 15 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_RecLineChars = 16 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_RecLineHeight = 17 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_RecLineSpacing = 18 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_RecLinesToPaperCut = 19 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_RecLineWidth = 20 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_RecNearEnd = 21 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_RecSidewaysMaxChars = 22 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_RecSidewaysMaxLines = 23 + PIDX_PTR + PIDX_NUMBER;

        const long PIDXPtr_SlpEmpty = 24 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpLetterQuality = 25 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpLineChars = 26 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpLineHeight = 27 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpLinesNearEndToEnd = 28 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpLineSpacing = 29 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpLineWidth = 30 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpMaxLines = 31 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpNearEnd = 32 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpSidewaysMaxChars = 33 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpSidewaysMaxLines = 34 + PIDX_PTR + PIDX_NUMBER;

        //      Added for Release 1.1:
        const long PIDXPtr_ErrorLevel = 35 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_RotateSpecial = 36 + PIDX_PTR + PIDX_NUMBER;

        //      Added for Release 1.5:
        const long PIDXPtr_CartridgeNotify = 37 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_JrnCartridgeState = 38 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_JrnCurrentCartridge = 39 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_RecCartridgeState = 40 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_RecCurrentCartridge = 41 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpPrintSide = 42 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpCartridgeState = 43 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_SlpCurrentCartridge = 44 + PIDX_PTR + PIDX_NUMBER;

        // Added in Release 1.7
        const long PIDXPtr_MapCharacterSet = 45 + PIDX_PTR + PIDX_NUMBER;

        // Added in Release 1.9
        const long PIDXPtr_PageModeDescriptor = 46 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_PageModeHorizontalPosition = 47 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_PageModePrintDirection = 48 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_PageModeStation = 49 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_PageModeVerticalPosition = 50 + PIDX_PTR + PIDX_NUMBER;

        // * Capabilities *

        const long PIDXPtr_CapConcurrentJrnRec = 501 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapConcurrentJrnSlp = 502 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapConcurrentRecSlp = 503 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapCoverSensor = 504 + PIDX_PTR + PIDX_NUMBER;

        const long PIDXPtr_CapJrn2Color = 505 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapJrnBold = 506 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapJrnDhigh = 507 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapJrnDwide = 508 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapJrnDwideDhigh = 509 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapJrnEmptySensor = 510 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapJrnItalic = 511 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapJrnNearEndSensor = 512 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapJrnPresent = 513 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapJrnUnderline = 514 + PIDX_PTR + PIDX_NUMBER;

        const long PIDXPtr_CapRec2Color = 515 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecBarCode = 516 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecBitmap = 517 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecBold = 518 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecDhigh = 519 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecDwide = 520 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecDwideDhigh = 521 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecEmptySensor = 522 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecItalic = 523 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecLeft90 = 524 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecNearEndSensor = 525 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecPapercut = 526 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecPresent = 527 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecRight90 = 528 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecRotate180 = 529 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecStamp = 530 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecUnderline = 531 + PIDX_PTR + PIDX_NUMBER;

        const long PIDXPtr_CapSlp2Color = 532 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpBarCode = 533 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpBitmap = 534 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpBold = 535 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpDhigh = 536 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpDwide = 537 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpDwideDhigh = 538 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpEmptySensor = 539 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpFullslip = 540 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpItalic = 541 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpLeft90 = 542 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpNearEndSensor = 543 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpPresent = 544 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpRight90 = 545 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpRotate180 = 546 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpUnderline = 547 + PIDX_PTR + PIDX_NUMBER;

        //      Added for Release 1.1:
        const long PIDXPtr_CapCharacterSet = 548 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapTransaction = 549 + PIDX_PTR + PIDX_NUMBER;

        //      Added for Release 1.5:
        const long PIDXPtr_CapJrnCartridgeSensor = 550 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapJrnColor = 551 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecCartridgeSensor = 552 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecColor = 553 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecMarkFeed = 554 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpBothSidesPrint = 555 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpCartridgeSensor = 556 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpColor = 557 + PIDX_PTR + PIDX_NUMBER;

        // Added in Release 1.7
        const long PIDXPtr_CapMapCharacterSet = 558 + PIDX_PTR + PIDX_NUMBER;

        // Added in Release 1.9
        const long PIDXPtr_CapConcurrentPageMode = 559 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapRecPageMode = 560 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpPageMode = 561 + PIDX_PTR + PIDX_NUMBER;

        // Added in Release 1.13
        const long PIDXPtr_CapRecRuledLine = 562 + PIDX_PTR + PIDX_NUMBER;
        const long PIDXPtr_CapSlpRuledLine = 563 + PIDX_PTR + PIDX_NUMBER;


        //////////////////////////////////////////////////////////////////////
        // String Property Index Values.
        //////////////////////////////////////////////////////////////////////

        // * Properties *

        const long PIDXPtr_CharacterSetList = 1 + PIDX_PTR + PIDX_STRING;
        const long PIDXPtr_JrnLineCharsList = 2 + PIDX_PTR + PIDX_STRING;
        const long PIDXPtr_RecLineCharsList = 3 + PIDX_PTR + PIDX_STRING;
        const long PIDXPtr_SlpLineCharsList = 4 + PIDX_PTR + PIDX_STRING;

        //      Added for Release 1.1:
        const long PIDXPtr_ErrorString = 5 + PIDX_PTR + PIDX_STRING;
        const long PIDXPtr_FontTypefaceList = 6 + PIDX_PTR + PIDX_STRING;
        const long PIDXPtr_RecBarCodeRotationList = 7 + PIDX_PTR + PIDX_STRING;
        const long PIDXPtr_SlpBarCodeRotationList = 8 + PIDX_PTR + PIDX_STRING;

        // Added in Release 1.7
        const long PIDXPtr_RecBitmapRotationList = 9 + PIDX_PTR + PIDX_STRING;
        const long PIDXPtr_SlpBitmapRotationList = 10 + PIDX_PTR + PIDX_STRING;

        // Added in Release 1.9
        const long PIDXPtr_PageModeArea = 11 + PIDX_PTR + PIDX_STRING;
        const long PIDXPtr_PageModePrintArea = 12 + PIDX_PTR + PIDX_STRING;

        /////////////////////////////////////////////////////////////////////
        // Printer Station Constants
        /////////////////////////////////////////////////////////////////////

        const long PTR_S_JOURNAL = 1;
        const long PTR_S_RECEIPT = 2;
        const long PTR_S_SLIP = 4;

        const long PTR_S_JOURNAL_RECEIPT = 0x0003;
        const long PTR_S_JOURNAL_SLIP = 0x0005;
        const long PTR_S_RECEIPT_SLIP = 0x0006;

        const long PTR_TWO_RECEIPT_JOURNAL = 0x8003; // (added in 1.3)
        const long PTR_TWO_SLIP_JOURNAL = 0x8005; // (added in 1.3)
        const long PTR_TWO_SLIP_RECEIPT = 0x8006; // (added in 1.3)


        /////////////////////////////////////////////////////////////////////
        // "CapCharacterSet" Property Constants (added in 1.1)
        /////////////////////////////////////////////////////////////////////

        const long PTR_CCS_ALPHA = 1;
        const long PTR_CCS_ASCII = 998;
        const long PTR_CCS_KANA = 10;
        const long PTR_CCS_KANJI = 11;
        const long PTR_CCS_UNICODE = 997; // (added in 1.5)


        /////////////////////////////////////////////////////////////////////
        // "CharacterSet" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long PTR_CS_UNICODE = 997; // (added in 1.5)
        const long PTR_CS_ASCII = 998;
        const long PTR_CS_WINDOWS = 999;
        const long PTR_CS_ANSI = 999;


        /////////////////////////////////////////////////////////////////////
        // "ErrorLevel" Property Constants (added in 1.1)
        /////////////////////////////////////////////////////////////////////

        const long PTR_EL_NONE = 1;
        const long PTR_EL_RECOVERABLE = 2;
        const long PTR_EL_FATAL = 3;


        /////////////////////////////////////////////////////////////////////
        // "MapMode" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long PTR_MM_DOTS = 1;
        const long PTR_MM_TWIPS = 2;
        const long PTR_MM_ENGLISH = 3;
        const long PTR_MM_METRIC = 4;


        /////////////////////////////////////////////////////////////////////
        // "CapXxxColor" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long PTR_COLOR_PRIMARY = 0x00000001;
        const long PTR_COLOR_CUSTOM1 = 0x00000002;
        const long PTR_COLOR_CUSTOM2 = 0x00000004;
        const long PTR_COLOR_CUSTOM3 = 0x00000008;
        const long PTR_COLOR_CUSTOM4 = 0x00000010;
        const long PTR_COLOR_CUSTOM5 = 0x00000020;
        const long PTR_COLOR_CUSTOM6 = 0x00000040;
        const long PTR_COLOR_CYAN = 0x00000100;
        const long PTR_COLOR_MAGENTA = 0x00000200;
        const long PTR_COLOR_YELLOW = 0x00000400;
        const long PTR_COLOR_FULL = 0x80000000;


        /////////////////////////////////////////////////////////////////////
        // "CapXxxCartridgeSensor" and  "XxxCartridgeState" Property Constants
        //   (added in 1.5)
        /////////////////////////////////////////////////////////////////////

        const long PTR_CART_UNKNOWN = 0x10000000;
        const long PTR_CART_OK = 0x00000000;
        const long PTR_CART_REMOVED = 0x00000001;
        const long PTR_CART_EMPTY = 0x00000002;
        const long PTR_CART_NEAREND = 0x00000004;
        const long PTR_CART_CLEANING = 0x00000008;


        /////////////////////////////////////////////////////////////////////
        // "CartridgeNotify" Property Constants (added in 1.5)
        /////////////////////////////////////////////////////////////////////

        const long PTR_CN_DISABLED = 0x00000000;
        const long PTR_CN_ENABLED = 0x00000001;


        /////////////////////////////////////////////////////////////////////
        // "PageModeDescriptor" Property Constants (added in 1.9)
        /////////////////////////////////////////////////////////////////////

        const long PTR_PM_BITMAP = 0x00000001;
        const long PTR_PM_BARCODE = 0x00000002;
        const long PTR_PM_BM_ROTATE = 0x00000004;
        const long PTR_PM_BC_ROTATE = 0x00000008;
        const long PTR_PM_OPAQUE = 0x00000010;


        /////////////////////////////////////////////////////////////////////
        // "PageModePrintDirection" Property Constants (added in 1.9)
        /////////////////////////////////////////////////////////////////////

        const long PTR_PD_LEFT_TO_RIGHT = 1;
        const long PTR_PD_BOTTOM_TO_TOP = 2;
        const long PTR_PD_RIGHT_TO_LEFT = 3;
        const long PTR_PD_TOP_TO_BOTTOM = 4;


        /////////////////////////////////////////////////////////////////////
        // "CutPaper" Method Constant
        /////////////////////////////////////////////////////////////////////

        const long PTR_CP_FULLCUT = 100;


        /////////////////////////////////////////////////////////////////////
        // "PageModePrint" Method: "Control" Parameter Constants (added in 1.9)
        /////////////////////////////////////////////////////////////////////

        const long PTR_PM_PAGE_MODE = 1;
        const long PTR_PM_PRINT_SAVE = 2;
        const long PTR_PM_NORMAL = 3;
        const long PTR_PM_CANCEL = 4;


        /////////////////////////////////////////////////////////////////////
        // "PrintBarCode" Method Constants:
        /////////////////////////////////////////////////////////////////////

        //** "Alignment" Parameter
        //     Either the distance from the left-most print column to the start
        //     of the bar code, or one of the following:

        const long PTR_BC_LEFT = -1;
        const long PTR_BC_CENTER = -2;
        const long PTR_BC_RIGHT = -3;

        //** "TextPosition" Parameter

        const long PTR_BC_TEXT_NONE = -11;
        const long PTR_BC_TEXT_ABOVE = -12;
        const long PTR_BC_TEXT_BELOW = -13;

        //** "Symbology" Parameter:

        //    - One dimensional symbologies
        const long PTR_BCS_UPCA = 101;  // Digits
        const long PTR_BCS_UPCE = 102;  // Digits
        const long PTR_BCS_JAN8 = 103;  // = EAN 8
        const long PTR_BCS_EAN8 = 103;  // = JAN 8 (added in 1.2)
        const long PTR_BCS_JAN13 = 104;  // = EAN 13
        const long PTR_BCS_EAN13 = 104;  // = JAN 13 (added in 1.2)
        const long PTR_BCS_TF = 105;  // (Discrete 2 of 5) Digits
        const long PTR_BCS_ITF = 106;  // (Interleaved 2 of 5) Digits
        const long PTR_BCS_Codabar = 107;  // Digits, -, $, :, /, ., +;
        //   4 start/stop characters
        //   (a, b, c, d)
        const long PTR_BCS_Code39 = 108;  // Alpha, Digits, Space, -, .,
        //   $, /, +, %; start/stop (*)
        // Also has Full ASCII feature
        const long PTR_BCS_Code93 = 109;  // Same characters as Code 39
        const long PTR_BCS_Code128 = 110;  // 128 data characters

        //    - One dimensional symbologies (added in 1.2)
        const long PTR_BCS_UPCA_S = 111;  // UPC-A with supplemental
        //   barcode
        const long PTR_BCS_UPCE_S = 112;  // UPC-E with supplemental
        //   barcode
        const long PTR_BCS_UPCD1 = 113;  // UPC-D1
        const long PTR_BCS_UPCD2 = 114;  // UPC-D2
        const long PTR_BCS_UPCD3 = 115;  // UPC-D3
        const long PTR_BCS_UPCD4 = 116;  // UPC-D4
        const long PTR_BCS_UPCD5 = 117;  // UPC-D5
        const long PTR_BCS_EAN8_S = 118;  // EAN 8 with supplemental
        //   barcode
        const long PTR_BCS_EAN13_S = 119;  // EAN 13 with supplemental
        //   barcode
        const long PTR_BCS_EAN128 = 120;  // EAN 128
        const long PTR_BCS_OCRA = 121;  // OCR "A"
        const long PTR_BCS_OCRB = 122;  // OCR "B"

        //    - One dimensional symbologies (added in 1.8)
        const long PTR_BCS_Code128_Parsed = 123;  // Code 128 with parsing
        //        The following RSS constants deprecated in 1.12.
        //        Instead use the GS1DATABAR constants below.
        const long PTR_BCS_RSS14 = 131;  // Reduced Space Symbology - 14 digit GTIN
        const long PTR_BCS_RSS_EXPANDED = 132;  // RSS - 14 digit GTIN plus additional fields

        //    - One dimensional symbologies (added in 1.12)
        const long PTR_BCS_GS1DATABAR = 131;  // GS1 DataBar Omnidirectional
        const long PTR_BCS_GS1DATABAR_E = 132;  // GS1 DataBar Expanded
        const long PTR_BCS_GS1DATABAR_S = 133;  // GS1 DataBar Stacked Omnidirectional
        const long PTR_BCS_GS1DATABAR_E_S = 134;  // GS1 DataBar Expanded Stacked

        //    - Two dimensional symbologies
        const long PTR_BCS_PDF417 = 201;
        const long PTR_BCS_MAXICODE = 202;

        //    - Two dimensional symbologies (added in 1.13)
        const long PTR_BCS_DATAMATRIX = 203;  // Data Matrix
        const long PTR_BCS_QRCODE = 204;  // QR Code
        const long PTR_BCS_UQRCODE = 205;  // Micro QR Code
        const long PTR_BCS_AZTEC = 206;  // Aztec
        const long PTR_BCS_UPDF417 = 207;  // Micro PDF 417

        //    - Start of Printer-Specific bar code symbologies
        const long PTR_BCS_OTHER = 501;


        /////////////////////////////////////////////////////////////////////
        // "PrintBitmap" and "PrintMemoryBitmap" Method Constants:
        /////////////////////////////////////////////////////////////////////

        //** "Width" Parameter
        //     Either bitmap width or:

        const long PTR_BM_ASIS = -11;  // One pixel per printer dot

        //** "Alignment" Parameter
        //     Either the distance from the left-most print column to the start
        //     of the bitmap, or one of the following:

        const long PTR_BM_LEFT = -1;
        const long PTR_BM_CENTER = -2;
        const long PTR_BM_RIGHT = -3;

        //** "Type" Parameter ("PrintMemoryBitmap" only)
        const long PTR_BMT_BMP = 1;
        const long PTR_BMT_JPEG = 2;
        const long PTR_BMT_GIF = 3;


        /////////////////////////////////////////////////////////////////////
        // "RotatePrint" Method: "Rotation" Parameter Constants
        // "RotateSpecial" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long PTR_RP_NORMAL = 0x0001;
        const long PTR_RP_RIGHT90 = 0x0101;
        const long PTR_RP_LEFT90 = 0x0102;
        const long PTR_RP_ROTATE180 = 0x0103;

        // For "RotatePrint", one or both of the following values may be
        // ORed with one of the above values.
        const long PTR_RP_BARCODE = 0x1000; // (added in 1.7)
        const long PTR_RP_BITMAP = 0x2000; // (added in 1.7)


        /////////////////////////////////////////////////////////////////////
        // "SetLogo" Method: "Location" Parameter Constants
        /////////////////////////////////////////////////////////////////////

        const long PTR_L_TOP = 1;
        const long PTR_L_BOTTOM = 2;


        /////////////////////////////////////////////////////////////////////
        // "TransactionPrint" Method: "Control" Parameter Constants (added in 1.1)
        /////////////////////////////////////////////////////////////////////

        const long PTR_TP_TRANSACTION = 11;
        const long PTR_TP_NORMAL = 12;


        /////////////////////////////////////////////////////////////////////
        // "MarkFeed" Method: "Type" Parameter Constants (added in 1.5)
        // "CapRecMarkFeed" Property Constants (added in 1.5)
        /////////////////////////////////////////////////////////////////////

        const long PTR_MF_TO_TAKEUP = 1;
        const long PTR_MF_TO_CUTTER = 2;
        const long PTR_MF_TO_CURRENT_TOF = 4;
        const long PTR_MF_TO_NEXT_TOF = 8;


        /////////////////////////////////////////////////////////////////////
        // "ChangePrintSide" Method: "Side" Parameter Constants (added in 1.5)
        /////////////////////////////////////////////////////////////////////

        const long PTR_PS_UNKNOWN = 0;
        const long PTR_PS_SIDE1 = 1;
        const long PTR_PS_SIDE2 = 2;
        const long PTR_PS_OPPOSITE = 3;


        /////////////////////////////////////////////////////////////////////
        // "CapRecRuledLine" and "CapSlpRuledLine" Property Constants
        // "DrawRuledLine" Method: "LineDirection" Parameter Constants
        // (added in 1.13)
        /////////////////////////////////////////////////////////////////////

        const long PTR_RL_HORIZONTAL = 1;
        const long PTR_RL_VERTICAL = 2;


        /////////////////////////////////////////////////////////////////////
        // "DrawRuledLine" Method: "LineStyle" Parameter Constants
        // (added in 1.13)
        /////////////////////////////////////////////////////////////////////

        const long PTR_LS_SINGLE_SOLID_LINE = 1;
        const long PTR_LS_DOUBLE_SOLID_LINE = 2;
        const long PTR_LS_BROKEN_LINE = 3;
        const long PTR_LS_CHAIN_LINE = 4;


        /////////////////////////////////////////////////////////////////////
        // "StatusUpdateEvent" Event: "Data" Parameter Constants
        /////////////////////////////////////////////////////////////////////

        const long PTR_SUE_COVER_OPEN = 11;
        const long PTR_SUE_COVER_OK = 12;
        const long PTR_SUE_JRN_COVER_OPEN = 60;  // (added in 1.8)
        const long PTR_SUE_JRN_COVER_OK = 61;  // (added in 1.8)
        const long PTR_SUE_REC_COVER_OPEN = 62;  // (added in 1.8)
        const long PTR_SUE_REC_COVER_OK = 63;  // (added in 1.8)
        const long PTR_SUE_SLP_COVER_OPEN = 64;  // (added in 1.8)
        const long PTR_SUE_SLP_COVER_OK = 65;  // (added in 1.8)

        const long PTR_SUE_JRN_EMPTY = 21;
        const long PTR_SUE_JRN_NEAREMPTY = 22;
        const long PTR_SUE_JRN_PAPEROK = 23;

        const long PTR_SUE_REC_EMPTY = 24;
        const long PTR_SUE_REC_NEAREMPTY = 25;
        const long PTR_SUE_REC_PAPEROK = 26;

        const long PTR_SUE_SLP_EMPTY = 27;
        const long PTR_SUE_SLP_NEAREMPTY = 28;
        const long PTR_SUE_SLP_PAPEROK = 29;

        const long PTR_SUE_JRN_CARTRIDGE_EMPTY = 41; // (added in 1.5)
        const long PTR_SUE_JRN_CARTRIDGE_NEAREMPTY = 42; // (added in 1.5)
        const long PTR_SUE_JRN_HEAD_CLEANING = 43; // (added in 1.5)
        const long PTR_SUE_JRN_CARTRIDGE_OK = 44; // (added in 1.5)

        const long PTR_SUE_REC_CARTRIDGE_EMPTY = 45; // (added in 1.5)
        const long PTR_SUE_REC_CARTRIDGE_NEAREMPTY = 46; // (added in 1.5)
        const long PTR_SUE_REC_HEAD_CLEANING = 47; // (added in 1.5)
        const long PTR_SUE_REC_CARTRIDGE_OK = 48; // (added in 1.5)

        const long PTR_SUE_SLP_CARTRIDGE_EMPTY = 49; // (added in 1.5)
        const long PTR_SUE_SLP_CARTRIDGE_NEAREMPTY = 50; // (added in 1.5)
        const long PTR_SUE_SLP_HEAD_CLEANING = 51; // (added in 1.5)
        const long PTR_SUE_SLP_CARTRIDGE_OK = 52; // (added in 1.5)

        const long PTR_SUE_IDLE = 1001;


        /////////////////////////////////////////////////////////////////////
        // "ResultCodeExtended" Property Constants
        /////////////////////////////////////////////////////////////////////

        const long OPOS_EPTR_COVER_OPEN = 201; // (Several)
        const long OPOS_EPTR_JRN_EMPTY = 202; // (Several)
        const long OPOS_EPTR_REC_EMPTY = 203; // (Several)
        const long OPOS_EPTR_SLP_EMPTY = 204; // (Several)
        const long OPOS_EPTR_SLP_FORM = 205; // EndRemoval
        const long OPOS_EPTR_TOOBIG = 206; // PrintBitmap
        const long OPOS_EPTR_BADFORMAT = 207; // PrintBitmap
        const long OPOS_EPTR_JRN_CARTRIDGE_REMOVED = 208; // (Several) (added in 1.5)
        const long OPOS_EPTR_JRN_CARTRIDGE_EMPTY = 209; // (Several) (added in 1.5)
        const long OPOS_EPTR_JRN_HEAD_CLEANING = 210; // (Several) (added in 1.5)
        const long OPOS_EPTR_REC_CARTRIDGE_REMOVED = 211; // (Several) (added in 1.5)
        const long OPOS_EPTR_REC_CARTRIDGE_EMPTY = 212; // (Several) (added in 1.5)
        const long OPOS_EPTR_REC_HEAD_CLEANING = 213; // (Several) (added in 1.5)
        const long OPOS_EPTR_SLP_CARTRIDGE_REMOVED = 214; // (Several) (added in 1.5)
        const long OPOS_EPTR_SLP_CARTRIDGE_EMPTY = 215; // (Several) (added in 1.5)
        const long OPOS_EPTR_SLP_HEAD_CLEANING = 216; // (Several) (added in 1.5)

        #endregion OPOS constants
    }
}
