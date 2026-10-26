using System;
using System.Runtime.InteropServices;
using NppPluginNET;
using NppPlugin.DllExport;

namespace XPatherizerNPP
{
    class UnmanagedExports
    {
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static bool isUnicode()
        {
            return true;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void setInfo(NppData notepadPlusData)
        {
            PluginBase.nppData = notepadPlusData;
            Main.CommandMenuInit();
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getFuncsArray(ref int nbF)
        {
            nbF = PluginBase._funcItems.Items.Count;
            return PluginBase._funcItems.NativePointer;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static uint messageProc(uint Message, IntPtr wParam, IntPtr lParam)
        {
            return 1;
        }

        static IntPtr _ptrPluginName = IntPtr.Zero;
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getName()
        {
            if (_ptrPluginName == IntPtr.Zero)
                _ptrPluginName = Marshal.StringToHGlobalUni(Main.PluginName);
            return _ptrPluginName;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void beNotified(IntPtr notifyCode)
        {
            if (notifyCode == IntPtr.Zero) return;
            try
            {
            Main.Notifications(notifyCode);
            Sci_NotifyHeader header = (Sci_NotifyHeader)Marshal.PtrToStructure(notifyCode, typeof(Sci_NotifyHeader));
            if (header.code == (uint)NppMsg.NPPN_TBMODIFICATION)
            {
                PluginBase._funcItems.RefreshItems();
            }
            else if (header.code == (uint)NppMsg.NPPN_SHUTDOWN)
            {
                Main.PluginCleanUp();
                Marshal.FreeHGlobal(_ptrPluginName);
                _ptrPluginName = IntPtr.Zero;
            }
            }
            catch (Exception ex)
            {
                // Managed exceptions must never escape a native callback.
                try
                {
                    System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                        "XPatherizerNPP-errors.log"), DateTime.Now.ToString("o") + " " + ex + Environment.NewLine);
                }
                catch { }
            }
        }
    }
}
