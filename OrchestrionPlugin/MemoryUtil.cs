using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OrchestrionPlugin
{

    public class MemoryUtil
    {
        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess,
IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
     uint processAccess,
     bool bInheritHandle,
     int processId);

    public static long GetMusicWriteAddress()
        {
            var process = Process.GetProcessesByName("ffxiv_dx11")[0];

            var hProc = OpenProcess(0x001F0FFF, false, process.Id);

            var modBase = GetModuleBaseAddress(process, "ffxiv_dx11.exe");

            var address = FindDMAAddy(hProc, (modBase + 0x01F31B68), new int[] { 0x98, 0x10, 0x58, 0x48, 0x38, 0x8, 0x80 });

            return Marshal.ReadInt64(address);
        }

        private static IntPtr FindDMAAddy(IntPtr hProc, IntPtr ptr, int[] offsets)
        {
            var buffer = new byte[IntPtr.Size];
            foreach (int i in offsets)
            {
                ReadProcessMemory(hProc, ptr, buffer, buffer.Length, out var read);

                ptr = (IntPtr.Size == 4)
                ? IntPtr.Add(new IntPtr(BitConverter.ToInt32(buffer, 0)), i)
                : ptr = IntPtr.Add(new IntPtr(BitConverter.ToInt64(buffer, 0)), i);
            }
            return ptr;
        }

        private static IntPtr GetModuleBaseAddress(Process proc, string modName)
        {
            IntPtr addr = IntPtr.Zero;

            foreach (ProcessModule m in proc.Modules)
            {
                if (m.ModuleName == modName)
                {
                    addr = m.BaseAddress;
                    break;
                }
            }
            return addr;
        }
    }
}
