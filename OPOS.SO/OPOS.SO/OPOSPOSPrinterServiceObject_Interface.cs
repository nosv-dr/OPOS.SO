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

namespace OPOSPOSPrinterSO
{
    [Guid("00000001-0001-0001-0001-000000000001"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface OPOSPOSPrinterServiceObject_Interface
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
        long ClearOutput();

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
        long BeginInsertion(long Timeout);

        [DispId(13)]
        long BeginRemoval(long Timeout);

        [DispId(14)]
        long CutPaper(long Percentage);

        [DispId(15)]
        long EndInsertion();

        [DispId(16)]
        long EndRemoval();
        
        [DispId(17)]
        long PrintBarCode( 
            long Station, 
            [MarshalAs(UnmanagedType.BStr)] String Data, 
            long Symbology, 
            long Height, 
            long Width, 
            long Alignment, 
            long TextPosition);
        
        [DispId(18)]
        long PrintBitmap( 
            long Station, 
            [MarshalAs(UnmanagedType.BStr)] String FileName, 
            long Width, 
            long Alignment);

        [DispId(19)]
        long PrintImmediate( 
            long Station, 
            [MarshalAs(UnmanagedType.BStr)] String Data);
  
        [DispId(20)]
        long PrintNormal( 
            long Station, 
            [MarshalAs(UnmanagedType.BStr)] String Data);


        [DispId(21)]
        long PrintTwoNormal( 
            long Stations, 
            [MarshalAs(UnmanagedType.BStr)] String Data1, 
            [MarshalAs(UnmanagedType.BStr)] String Data2);

        [DispId(22)]
        long RotatePrint( 
            long Station, 
            long Rotation);

        [DispId(23)]
        long SetBitmap( 
            long BitmapNumber, 
            long Station, 
            [MarshalAs(UnmanagedType.BStr)] String FileName, 
            long Width, 
            long Alignment);

        [DispId(24)]
        long SetLogo( 
            long Location, 
            [MarshalAs(UnmanagedType.BStr)] String Data);

        [DispId(25)]
        long TransactionPrint( 
            long Station, 
            long Control);

        [DispId(26)]
        long ValidateData( 
            long Station, 
            [MarshalAs(UnmanagedType.BStr)] String Data);

        [DispId(27)]
        long ChangePrintSide( 
            long Side);

        [DispId(28)]
        long MarkFeed( 
            long Type);        
        
        [DispId(29)]
        long ResetStatistics(
            [MarshalAs(UnmanagedType.BStr)]
            String StatisticsBuffer);

        [DispId(30)]
        long RetrieveStatistics(
            [MarshalAs(UnmanagedType.BStr)]
            ref String StatisticsBuffer);

        [DispId(31)]
        long UpdateStatistics(
            [MarshalAs(UnmanagedType.BStr)]
            String StatisticsBuffer);

        [DispId(32)]
        long CompareFirmwareVersion(
            [MarshalAs(UnmanagedType.BStr)]
            String FirmwareFileName,
            out long pResult);

        [DispId(33)]
        long UpdateFirmware(
            [MarshalAs(UnmanagedType.BStr)]
            String FirmwareFileName);

        [DispId(34)]
        long ClearPrintArea();


        [DispId(35)]
        long PageModePrint( 
            long Control);

        [DispId(36)]
        long PrintMemoryBitmap( 
            long Station, 
            [MarshalAs(UnmanagedType.BStr)]
            String Data, 
            long Type, 
            long Width, 
            long Alignment);

        [DispId(37)]
        long DrawRuledLine( 
            long Station, 
            [MarshalAs(UnmanagedType.BStr)]
            String PositionList, 
            long LineDirection, 
            long LineWidth, 
            long LineStyle, 
            long LineColor);
    }
}
