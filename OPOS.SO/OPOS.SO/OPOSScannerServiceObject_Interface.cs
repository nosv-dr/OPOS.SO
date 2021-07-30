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
    [Guid("00000001-0001-0001-0001-000000000001"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface OPOSScannerServiceObject_Interface
    {
        [DispId(0)]
        long COFreezeEvents(
            [MarshalAs(UnmanagedType.Bool)]
            bool Freeze);

        [DispId(1)]
        [return: MarshalAs(UnmanagedType.I8)]
        long GetPropertyNumber(long PropIndex);

        [DispId(2)]
        void SetPropertyNumber(
            long PropIndex,
            [MarshalAs(UnmanagedType.I8)] 
            long Number);

        [DispId(3)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetPropertyString(long PropIndex);

        [DispId(4)]
        void SetPropertyString(
            long PropIndex,
            [MarshalAs(UnmanagedType.BStr)] 
            String String);

        [DispId(5)]
        long OpenService(
            [MarshalAs(UnmanagedType.BStr)] String DeviceClass,
            [MarshalAs(UnmanagedType.BStr)] String DeviceName,
            [MarshalAs(UnmanagedType.IDispatch)] object pDispatch);


        [DispId(6)]
        long CheckHealth(long Level);

        [DispId(7)]
        long ClaimDevice(long Timeout);

        [DispId(8)]
        long ClearInput();

        [DispId(9)]
        long CloseService();

        [DispId(10)]
        long DirectIO(
            long Command,
            ref long pData,
            [MarshalAs(UnmanagedType.BStr)]
            out String pString);

        [DispId(11)]
        long ReleaseDevice();

        [DispId(12)]
        long ResetStatistics(
            [MarshalAs(UnmanagedType.BStr)]
            String StatisticsBuffer);

        [DispId(13)]
        long RetrieveStatistics(
            [MarshalAs(UnmanagedType.BStr)]
            ref String StatisticsBuffer);

        [DispId(14)]
        long UpdateStatistics(
            [MarshalAs(UnmanagedType.BStr)]
            String StatisticsBuffer);

        [DispId(15)]
        long CompareFirmwareVersion(
            [MarshalAs(UnmanagedType.BStr)]
            String FirmwareFileName,
            out long pResult);

        [DispId(16)]
        long UpdateFirmware(
            [MarshalAs(UnmanagedType.BStr)]
            String FirmwareFileName);

        [DispId(17)]
        long ClearInputProperties();
    }
}
