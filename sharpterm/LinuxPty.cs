#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SharpTerm
{
    public class LinuxPty
    {
        private const int TCSANOW = 1;
        
        [DllImport("libutil")]
        private static extern int forkpty(out int amaster, string? name, IntPtr termp, IntPtr winp);

        [DllImport("libc")]
        private static extern int execv(string pathname, string?[] argv);

        [DllImport("libc")]
        private static extern int ioctl(int fd, uint cmd, ref winsize ws);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct winsize
        {
            public ushort ws_row;
            public ushort ws_col;
            public ushort ws_xpixel;   /* unused */
            public ushort ws_ypixel;   /* unused */
        }

        private readonly int _masterPtyFd;

        public LinuxPty()
        {
            var pid = forkpty(out _masterPtyFd, null, IntPtr.Zero, IntPtr.Zero);
            if (0 == pid)
            {
                // this is the child
                execv("/bin/sh", new[] {"/bin/sh", null});

                // we'll never actually hit this because execv on linux never returns
                // since it switches the process execution to the listed program, however
                // the compiler can't know that so we include a throw here so it doesn't
                // expect this code path to fully initialize everything
                throw new Exception();
            }
            else
            {
                ReadStream = new FileStream(new SafeFileHandle(new IntPtr(_masterPtyFd), false), FileAccess.Read);
                WriteStream = new FileStream(new SafeFileHandle(new IntPtr(_masterPtyFd), false), FileAccess.Write);
            }
        }
        
        public Stream ReadStream { get; }
        
        public Stream WriteStream { get; }

        public void SetSize(int width, int height)
        {
            var ws = new winsize {ws_col = (ushort)width, ws_row = (ushort)height};
            ioctl(_masterPtyFd, 0x5414, ref ws);
        }
    }
}