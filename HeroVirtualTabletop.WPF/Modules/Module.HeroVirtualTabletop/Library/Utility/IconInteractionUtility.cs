﻿using Module.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Module.HeroVirtualTabletop.Library.Utility
{
    public static class IconInteractionUtility
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool InitGame(IntPtr hWnd);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool CloseGame(IntPtr hWnd);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool SetUserHWND(IntPtr hWnd);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ExecuteCommand([MarshalAs(UnmanagedType.LPStr)]string commandline);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SetCOHPath([MarshalAs(UnmanagedType.LPStr)]string path);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate string GetHoveredNPCInfo();

        private static IntPtr dllHandle;
        private static InitGame initGame;
        private static CloseGame closeGame;
        private static SetUserHWND setUserHWnd;
        private static ExecuteCommand executeCmd;
        private static GetHoveredNPCInfo getHoveredNPCInfo;

        static IconInteractionUtility()
        {
            //string outPutDirectory = Application.ExecutablePath;
            //outPutDirectory = outPutDirectory.Substring(0, outPutDirectory.IndexOf("Shell"));
            //string iconPath = Path.Combine(outPutDirectory, @"Modules\Module.HeroVirtualTabletop\Resources\");
            dllHandle = WindowsUtilities.LoadLibrary(Path.Combine(Settings.Default.CityOfHeroesGameDirectory, "HookCostume.dll"));
            if (dllHandle != null)
            {
                IntPtr initGameAddress = WindowsUtilities.GetProcAddress(dllHandle, "InitGame");
                if (initGameAddress != IntPtr.Zero)
                {
                    initGame = (InitGame)(Marshal.GetDelegateForFunctionPointer(initGameAddress, typeof(InitGame)));
                }

                IntPtr closeGameAddress = WindowsUtilities.GetProcAddress(dllHandle, "CloseGame");
                if (closeGameAddress != IntPtr.Zero)
                {
                    closeGame = (CloseGame)(Marshal.GetDelegateForFunctionPointer(closeGameAddress, typeof(CloseGame)));
                }

                IntPtr setUserHWndAddress = WindowsUtilities.GetProcAddress(dllHandle, "SetUserHWND");
                if (setUserHWndAddress != IntPtr.Zero)
                {
                    setUserHWnd = (SetUserHWND)(Marshal.GetDelegateForFunctionPointer(setUserHWndAddress, typeof(SetUserHWND)));
                }

                IntPtr executeCmdAddress = WindowsUtilities.GetProcAddress(dllHandle, "ExecuteCommand");
                if (executeCmdAddress != IntPtr.Zero)
                {
                    executeCmd = (ExecuteCommand)(Marshal.GetDelegateForFunctionPointer(executeCmdAddress, typeof(ExecuteCommand)));
                }

                IntPtr getHoveredNPCInfoAddress = WindowsUtilities.GetProcAddress(dllHandle, "GetHoveredNPCInfo");
                if (getHoveredNPCInfoAddress != IntPtr.Zero)
                {
                    getHoveredNPCInfo = (GetHoveredNPCInfo)(Marshal.GetDelegateForFunctionPointer(getHoveredNPCInfoAddress, typeof(GetHoveredNPCInfo)));
                }


            }
        }

        public static void RunCOHAndLoadDLL()
        {
            initGame(IntPtr.Zero);
            MessageBox.Show("Please wait for COH to initialize and close this message");
            setUserHWnd(IntPtr.Zero);
        }

        public static void CloseCOH()
        {
            closeGame(IntPtr.Zero);
        }

        public static void ExecuteCmd(string command)
        {
            executeCmd(command);
        }
        
    }
}
