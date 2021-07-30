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
    [Guid("00000001-0001-0001-0001-000000000001"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface OPOSSmartCardRWServiceObject_Interface
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
        long ClearOutput();

        [DispId(10)]
        long CloseService();
        
        [DispId(11)]
        long DirectIO(
            long Command, 
            ref long pData, 
            [MarshalAs(UnmanagedType.BStr)]
            out String pString);

        [DispId(12)]
        long ReleaseDevice();

        [DispId(13)]
        long ResetStatistics(
            [MarshalAs(UnmanagedType.BStr)]
            String StatisticsBuffer);        

        [DispId(14)]
        long RetrieveStatistics(
            [MarshalAs(UnmanagedType.BStr)]
            ref String StatisticsBuffer);

        [DispId(15)]
        long UpdateStatistics(
            [MarshalAs(UnmanagedType.BStr)]
            String StatisticsBuffer);
        
        [DispId(16)]
        long BeginInsertion(long Timeout);

        [DispId(17)]
        long BeginRemoval(long Timeout);

        [DispId(18)]
        long EndInsertion();

        [DispId(19)]
        long EndRemoval();

        [DispId(20)]
        long ReadData(
            long Action,
            ref long pCount,
            [MarshalAs(UnmanagedType.BStr)]
            ref String pData);      
        
        [DispId(21)]
        long WriteData(
            long Action,
            long Count,
            [MarshalAs(UnmanagedType.BStr)]
            String Data);   

        [DispId(22)]
        long CompareFirmwareVersion(
            [MarshalAs(UnmanagedType.BStr)]
            String FirmwareFileName,
            out long pResult); 
  
        [DispId(23)]
        long UpdateFirmware(
            [MarshalAs(UnmanagedType.BStr)]
            String FirmwareFileName);  
 
        [DispId(24)]
        long ClearInputProperties();   
    }
}
