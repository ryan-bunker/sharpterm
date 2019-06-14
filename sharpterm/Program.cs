using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.SPIRV;
using SharpFont;
using SharpTerm;
using Encoding = System.Text.Encoding;

namespace GettingStarted
{
    public class Program
    {
        private static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;
        private static DeviceBuffer _vertexBuffer;
        private static DeviceBuffer _indexBuffer;
        private static Shader[] _shaders;
        private static Pipeline _pipeline;
        private static ResourceSet _textureSet;

        private static DeviceBuffer _projectionBuffer;
        private static ResourceSet _projectionSet;

        private static TextLayout _textArray;
        private static BufferWindow _textWindow;
        private static TextArrayRenderer _textRenderer;

        static void Main(string[] args)
        {
            WindowCreateInfo windowCI = new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 1920,
                WindowHeight = 1920,
                WindowTitle = "Veldrid Tutorial"
            };
            Sdl2Window window = VeldridStartup.CreateWindow(ref windowCI);

            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(window);

            CreateResources(window);

            while (window.Exists)
            {
                window.PumpEvents();

                if (window.Exists)
                {
                    Draw();
                }
            }

            DisposeResources();
        }

        public class CharCell
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float W { get; set; }
            public float H { get; set; }
            public float R => X + W;
            public float B => Y + H;
            public float Aspect => W / H;
        }

        private static CharCell[] Cells = new CharCell[256];

        private static void CreateResources(Sdl2Window window)
        {
            // load font and build texture atlas
            var face = new Face(new Library(), @"./Inconsolata-Regular.ttf");
            face.SetPixelSizes(36, 0);
            var charAtlas = new CharTextureAtlas(_graphicsDevice, face);
            
            _projectionBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            // Update our projection
            var charsWidth = window.Width / face.Size.Metrics.MaxAdvance.ToSingle();
            var charsHeight = window.Height / face.Size.Metrics.Height.ToSingle();
            var scale = Matrix4x4.CreateScale(2f / charsWidth, -2f / charsHeight, 1);
            var translate = Matrix4x4.CreateTranslation(-1f, 1f, 0);
            _graphicsDevice.UpdateBuffer(_projectionBuffer, 0, scale * translate);
            
            var buffer = new TextBuffer((uint)charsWidth, 1000);
            _textWindow = new BufferWindow(buffer, buffer.Width, (uint)charsHeight);
            _textRenderer = new TextArrayRenderer(_graphicsDevice, _textWindow, charAtlas, _projectionBuffer);
            _textArray = new TextLayout(buffer);
            
            _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();
        }

        private static long _lastFrame = Stopwatch.GetTimestamp();
        private static double[] _rates = new double[100];
        private static int _rateIdx = 0;

        private static DateTime _nextScroll = DateTime.UtcNow.AddSeconds(1);

        private static void Draw()
        {
            var now = Stopwatch.GetTimestamp();
            var elapsed = (now - _lastFrame) / (double)Stopwatch.Frequency;
            _rates[_rateIdx++ % _rates.Length] = 1.0 / elapsed;
            _lastFrame = now;

            var fps = _rates.Sum() / _rates.Length;
            _textArray.CursorLeft = 0;
            _textArray.CursorTop = 0;
            _textArray.Write($@"Hello world!
The time is {DateTime.Now}
{fps,6:F1} FPS
Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec non libero id sem elementum pretium ut ac justo. Donec a tellus nulla. Ut pretium justo nec justo feugiat, quis molestie lorem iaculis. Ut ut pellentesque mi, nec bibendum nulla. Duis elementum tortor ut eros laoreet, rhoncus mollis massa pretium. Phasellus vitae purus arcu. Duis fringilla diam luctus est facilisis dapibus. Cras faucibus fermentum quam ac varius. Aliquam erat volutpat. Etiam a sapien nec odio aliquet lacinia a non libero.

Sed malesuada risus purus, vulputate fringilla urna congue eu. Sed varius nibh vitae sagittis fermentum. Etiam sed augue ac lacus dignissim euismod. Maecenas auctor sem nibh, in ultricies enim eleifend quis. Etiam maximus, lacus ut viverra blandit, mauris libero tempor nisi, vel venenatis libero mauris a sapien. Cras ornare accumsan arcu, ut pulvinar lacus porttitor sit amet. Curabitur vehicula id diam cursus eleifend. Nam laoreet orci nisi, ut tincidunt elit tristique sit amet. Nulla gravida porta augue a auctor. Duis elit lectus, euismod dignissim risus non, mollis ultricies elit. Pellentesque dui dolor, ultrices in egestas vel, viverra ac nisi. Phasellus nulla mi, viverra sit amet tellus et, tristique tempor tellus.

In placerat viverra nunc vel viverra. Nulla consequat lorem et ipsum porttitor pulvinar. Fusce fermentum, risus non sollicitudin viverra, metus velit mollis urna, sit amet sollicitudin massa urna ut justo. Phasellus ac leo magna. Aliquam feugiat maximus purus, ut condimentum nunc ultricies et. Vivamus nec dolor quis leo faucibus tempor. Vivamus sollicitudin rhoncus libero, eget egestas mauris dictum euismod. Morbi a purus ut purus commodo ullamcorper sit amet blandit dolor. Ut congue ultrices dignissim.

Vestibulum non accumsan mauris, eu accumsan eros. Nulla vulputate massa at ligula condimentum mollis. Sed ac varius turpis. Nullam tempus felis quis mauris varius condimentum. Ut quis justo ut nisl vehicula gravida. Fusce quis ante eu felis gravida gravida. Ut eu arcu lectus. Cras eu felis efficitur, dapibus lectus eget, ultricies est. Curabitur cursus varius pulvinar. Vivamus accumsan sem non nunc feugiat, nec euismod metus mollis. Vivamus pretium mauris vitae cursus scelerisque.

Cras facilisis quam lacus, vitae convallis velit commodo sed. Praesent suscipit ipsum arcu, ut tempor leo sodales sit amet. Praesent a sem mi. Nullam sed fermentum dui. In imperdiet rutrum orci, et ullamcorper velit congue vitae. Pellentesque urna enim, finibus ut lectus a, pellentesque vulputate orci. Aenean bibendum orci et magna dignissim aliquet nec et turpis. Fusce risus erat, laoreet at odio a, aliquet finibus est. Integer dictum rhoncus rhoncus. Ut tempus ut enim vel sodales. Morbi nec felis sed lorem condimentum porttitor ut id arcu. Pellentesque placerat in massa id iaculis. Nulla a laoreet nibh. Etiam finibus lorem neque, aliquet iaculis mauris porta at. Sed sed erat id mi euismod pretium. Quisque vehicula rutrum odio in commodo.");

            if (DateTime.UtcNow >= _nextScroll)
            {
                _textWindow.OffsetY++;
                _nextScroll = _nextScroll.AddSeconds(1);
            }
            
            // Begin() must be called before commands can be issued.
            _commandList.Begin();
            
            // We want to render directly to the output window.
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
            _commandList.ClearColorTarget(0, RgbaFloat.Clear);

            _textRenderer.Render(_commandList);

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