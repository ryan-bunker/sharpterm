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
        private static extern int forkpty(out int amaster, string name, IntPtr termp, IntPtr winp);

        [DllImport("libc")]
        private static extern int tcgetattr(int fd, IntPtr termios_p);

        [DllImport("libc")]
        private static extern int tcsetattr(int fd, int optional_actions, IntPtr termios_p);
        
        [DllImport("libc")]
        private static extern void cfmakeraw(IntPtr termios_p);

        [DllImport("libc")]
        private static extern int execv(string pathname, string[] argv);

        public LinuxPty()
        {
            var pid = forkpty(out int masterFd, null, IntPtr.Zero, IntPtr.Zero);
            if (0 == pid)
            {
                // this is the child
                execv("/bin/sh", new[] {"/bin/sh", null});
            }
            else
            {
                var termSettings = Marshal.AllocHGlobal(4 * 4 + 4 * 20);
                tcgetattr(masterFd, termSettings);
                cfmakeraw(termSettings);
                tcsetattr(masterFd, TCSANOW, termSettings);

                ReadStream = new FileStream(new SafeFileHandle(new IntPtr(masterFd), true), FileAccess.Read);
                WriteStream = new FileStream(new SafeFileHandle(new IntPtr(masterFd), true), FileAccess.Write);
            }
        }
        
        public Stream ReadStream { get; }
        
        public Stream WriteStream { get; }
    }
}