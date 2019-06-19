using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using SharpTerm;

namespace GettingStarted
{
    public class Program
    {
        private static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;

        private static DeviceBuffer _projectionBuffer;

        private static FontAtlas _fontAtlas;
        private static OutputTokenizer _tokenizer;
        private static TextLayout _textLayout;
        private static BufferWindow _textWindow;
        private static TextArrayRenderer _textRenderer;

        static void Main(string[] args)
        {
            WindowCreateInfo windowCI = new WindowCreateInfo()
            {
                X = 1920,
                Y = 100,
                WindowWidth = 1920,
                WindowHeight = 1920,
                WindowTitle = "Veldrid Tutorial"
            };
            Sdl2Window window = VeldridStartup.CreateWindow(ref windowCI);

            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(window);

            CreateResources(window);

            var pty = new LinuxPty();
            var readStream =  new ConsoleLoggingStream(pty.ReadStream, "READ");
            var writeStream = new ConsoleLoggingStream(pty.WriteStream, "WRIT");

            var charsWidth = (int)(window.Width / (float) _fontAtlas.CellWidth);
            var charsHeight = (int)(window.Height / (float) _fontAtlas.CellHeight);
            pty.SetSize(charsWidth, charsHeight);
            
            _tokenizer = new OutputTokenizer(_textLayout);

            var inputProcessor = new InputKeyStreamer {OutStream = writeStream};
#pragma warning disable 4014
            PipePtyToScreen(readStream);
#pragma warning restore 4014

            while (window.Exists)
            {
                var input = window.PumpEvents();
                inputProcessor.ProcessToStream(input);

                if (window.Exists) Draw();
            }

            DisposeResources();
        }

        private static void CreateResources(Sdl2Window window)
        {
            // load font and build texture atlas
            _fontAtlas = new FontAtlas(_graphicsDevice, @"./Inconsolata-Regular.ttf", 36);
            
            _projectionBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            // Update our projection
            var charsWidth = window.Width / (float) _fontAtlas.CellWidth;
            var charsHeight = window.Height / (float) _fontAtlas.CellHeight;
            var scale = Matrix4x4.CreateScale(2f / charsWidth, -2f / charsHeight, 1);
            var translate = Matrix4x4.CreateTranslation(-1f, 1f, 0);
            _graphicsDevice.UpdateBuffer(_projectionBuffer, 0, scale * translate);
            
            var buffer = new TextBuffer((uint)charsWidth, 1000);
            _textWindow = new BufferWindow(buffer, buffer.Width, (uint)charsHeight);
            _textRenderer = new TextArrayRenderer(_graphicsDevice, _fontAtlas, _projectionBuffer);
            _textLayout = new TextLayout(buffer) {ForeColor = RgbaFloat.White};
            
            _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();
        }
        
        private static async Task PipePtyToScreen(Stream readStream)
        {
            var buffer = new byte[128];
            while (true)
            {
                int read = await readStream.ReadAsync(buffer, 0, buffer.Length);
                for (int i = 0; i < read; ++i)
                    _tokenizer.Process((char) buffer[i]);
            }
        }

        private static void Draw()
        {
            // Begin() must be called before commands can be issued.
            _commandList.Begin();
            
            // We want to render directly to the output window.
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
            _commandList.ClearColorTarget(0, RgbaFloat.Clear);
            _commandList.SetViewport(0, new Viewport(10, 10, 1900, 1900, 0, 1));

            _textRenderer.Render(_commandList, _textWindow);
            _textRenderer.Render(_commandList, '_', _textLayout.CursorLeft, _textLayout.CursorTop);

            // End() must be called before commands can be submitted for execution.
            _commandList.End();
            _graphicsDevice.SubmitCommands(_commandList);

            // Once commands have been submitted, the rendered image can be presented to the application window.
            _graphicsDevice.SwapBuffers();
        }

        private static void DisposeResources()
        {
            _textRenderer.Dispose();
            _projectionBuffer.Dispose();
            _commandList.Dispose();
            _graphicsDevice.Dispose();
        }
    }
}