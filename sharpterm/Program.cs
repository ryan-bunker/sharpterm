using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

                var masterFs = new FileStream(new SafeFileHandle(new IntPtr(masterFd), true), FileAccess.Read);
                var masterFsW = new FileStream(new SafeFileHandle(new IntPtr(masterFd), true), FileAccess.Write);
                
                var cts = new CancellationTokenSource();
                var readTask = ReadStreamAsync(masterFs, cts.Token);

                var buffer = new MemoryStream(4096);
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Backspace)
                    {
                        Console.CursorLeft--;
                        Console.Write(' ');
                        Console.CursorLeft--;
                        buffer.Position--;
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        buffer.WriteByte((byte)'\n');
                        
                        masterFsW.Write(buffer.GetBuffer(), 0, (int)buffer.Length);
                        masterFsW.Flush();
                        buffer.Position = 0;
                        buffer.SetLength(0);
                    }
                    else if (key.KeyChar >= ' ' && key.KeyChar <= (char) 127)
                    {
                        Console.Write(key.KeyChar);
                        buffer.WriteByte((byte)key.KeyChar);
                    }
                }
            }
        }

        static async Task ReadStreamAsync(Stream stream, CancellationToken ct)
        {
            var buffer = new byte[128];
            while (!ct.IsCancellationRequested)
            {
                int read = await stream.ReadAsync(buffer, ct);
                var line = Encoding.ASCII.GetString(buffer, 0, read);
                await Console.Out.WriteAsync(line);
            }
        }
    }
}