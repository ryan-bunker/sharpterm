using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace sharpterm
{
    internal class Program
    {
        private const int TCSANOW = 1;
        
        [DllImport("libutil")]
        private static extern int forkpty(out int amaster, string name, IntPtr termp, IntPtr winp);

        [DllImport("libc", SetLastError = true)]
        private static extern int read(int fd, byte[] buffer, int size);

        [DllImport("libc", SetLastError = true)]
        private static extern int write(int fd, byte[] buffer, int size);

        [DllImport("libc")]
        private static extern int tcgetattr(int fd, IntPtr termios_p);

        [DllImport("libc")]
        private static extern int tcsetattr(int fd, int optional_actions, IntPtr termios_p);
        
        [DllImport("libc")]
        private static extern void cfmakeraw(IntPtr termios_p);

        [DllImport("libc")]
        private static extern int execv(string pathname, string[] argv);

        static void Main(string[] args)
        {
            var termSettings = Marshal.AllocHGlobal(4 * 4 + 4 * 20);
            cfmakeraw(termSettings);
            
            var pid = forkpty(out int masterFd, null, termSettings, IntPtr.Zero);
            if (0 == pid)
            {
                // this is the child
                var stdinReader = new StreamReader(Console.OpenStandardInput());

                while (true)
                {
                    var line = stdinReader.ReadLine();
                    if (line == "quit") return;
                    Console.WriteLine(line.Reverse().ToArray());
                }
            }
            else
            {
                var masterFs = new FileStream(new SafeFileHandle(new IntPtr(masterFd), true), FileAccess.Read);
                var masterReader = new StreamReader(masterFs);
                var masterFsW = new FileStream(new SafeFileHandle(new IntPtr(masterFd), true), FileAccess.Write);
                var masterWriter = new StreamWriter(masterFsW);
                
                var cts = new CancellationTokenSource();
                var readTask = ReadStreamAsync(pid, masterReader, cts.Token);

                while (true)
                {
                    var msg = Console.ReadLine();
                    masterWriter.WriteLine(msg);
                    masterWriter.Flush();
                    if (msg == "quit") return;
                }
            }
        }

        static async Task ReadStreamAsync(int childPid, TextReader reader, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) return;
                Console.WriteLine($"[{childPid}]: {line}");
            }
        }
    }
}