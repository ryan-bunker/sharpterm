using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImGuiNET;
using Microsoft.Win32.SafeHandles;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace sharpterm
{
    internal class Program2
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

        private static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;
        private static DeviceBuffer _vertexBuffer;
        private static DeviceBuffer _indexBuffer;
        private static Shader[] _shaders;
        private static Pipeline _pipeline;

        private const string VertexCode = @"
#version 450
layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;
layout(location = 0) out vec4 fsin_Color;
void main()
{
    gl_Position = vec4(Position, 0, 1);
    fsin_Color = Color;
}";

        private const string FragmentCode = @"
#version 450
layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;
void main()
{
    fsout_Color = fsin_Color;
}";
        
        static void MainImGui(string[] args)
        {
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1920, 1080, WindowState.Normal, "ImGui Test"),
                out var window,
                out var gd);

            var cl = gd.ResourceFactory.CreateCommandList();

            // [1]
            var imguiRenderer = new ImGuiRenderer(
                gd,
                gd.MainSwapchain.Framebuffer.OutputDescription,
                window.Width,
                window.Height);

            var io = ImGui.GetIO();
//
//            var data = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ushort)) * 3);
//            var ranges = new ImVector<ushort>(3, 3, data);
//            ranges[0] = 0xf000;
//            ranges[1] = 0xf3ff;
//            ranges[2] = 0;
//            
            var font = io.Fonts.AddFontFromFileTTF("Inconsolata-Regular.ttf", 16);
//            io.Fonts.Build();
            imguiRenderer.RecreateFontDeviceTexture();
            if (!font.IsLoaded())
                throw new Exception("Could not load font");

            var field = typeof(ImGuiRenderer).GetField("_scaleFactor", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(imguiRenderer, new Vector2(2.5f, 2.5f));

            window.Resized += () =>
            {
                imguiRenderer.WindowResized(window.Width, window.Height);
                gd.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
            };
            
            var buffer = new StringBuilder("[ryan@manjaro-xfce-vm-laptop ~]$ ");
//            for (int i = 127; i < 256 + 128; i++)
//                buffer.AppendLine($"{i}: {(char)i}");

            var nextToggle = DateTime.UtcNow.AddMilliseconds(750);
            bool visible = true;
            while (window.Exists)
            {
                var snapshot = new InputSnapshotWrapper(window.PumpEvents(), new Vector2(2.5f, 2.5f));
                imguiRenderer.Update(1f / 60f, snapshot); // [2]

                bool scrollToBottom = false;
                foreach (var c in snapshot.KeyCharPresses)
                {
                    buffer.Append(c);
                    scrollToBottom = true;
                }

                foreach (var k in snapshot.KeyEvents)
                    if (k.Down && k.Key == Key.Enter)
                    {
                        buffer.AppendLine();
                        scrollToBottom = true;
                    }

                if (DateTime.UtcNow >= nextToggle)
                {
                    visible = !visible;
                    nextToggle = nextToggle.AddMilliseconds(750);
                }

                // Draw whatever you want here.
                ImGui.Begin("Test Window", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoNav);
                ImGui.SetWindowPos(new Vector2(0, 0));
                ImGui.SetWindowSize(new Vector2(window.Width / 2.5f, window.Height / 2.5f));
                {
                    ImGui.PushFont(font);
                    
                    ImGui.TextWrapped(buffer.ToString() + (visible ? '_' : ' '));
                    if (scrollToBottom)
                        ImGui.SetScrollHereY(1);
                }

                cl.Begin();
                cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
                cl.ClearColorTarget(0, new RgbaFloat(0, 0, 0.2f, 1f));
                imguiRenderer.Render(gd, cl); // [3]
                cl.End();
                gd.SubmitCommands(cl);
                gd.SwapBuffers(gd.MainSwapchain);
            }       
        }

        private class InputSnapshotWrapper : InputSnapshot
        {
            private InputSnapshot _internal;
            private Vector2 _scale;

            public InputSnapshotWrapper(InputSnapshot @internal, Vector2 scale)
            {
                _internal = @internal;
                _scale = scale;
            }

            public bool IsMouseDown(MouseButton button) => _internal.IsMouseDown(button);

            public IReadOnlyList<KeyEvent> KeyEvents => _internal.KeyEvents;

            public IReadOnlyList<MouseEvent> MouseEvents => _internal.MouseEvents;

            public IReadOnlyList<char> KeyCharPresses => _internal.KeyCharPresses;

            public Vector2 MousePosition => _internal.MousePosition / _scale;

            public float WheelDelta => _internal.WheelDelta;
        }

        private static void CreateResources()
        {
            ResourceFactory factory = _graphicsDevice.ResourceFactory;

            VertexPositionColor[] quadVertices =
            {
                new VertexPositionColor(new Vector2(-.75f, .75f), RgbaFloat.Red),
                new VertexPositionColor(new Vector2(.75f, .75f), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(-.75f, -.75f), RgbaFloat.Blue),
                new VertexPositionColor(new Vector2(.75f, -.75f), RgbaFloat.Yellow)
            };
            BufferDescription vbDescription = new BufferDescription(
                4 * VertexPositionColor.SizeInBytes,
                BufferUsage.VertexBuffer);
            _vertexBuffer = factory.CreateBuffer(vbDescription);
            _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, quadVertices);

            ushort[] quadIndices = { 0, 1, 2, 3 };
            BufferDescription ibDescription = new BufferDescription(
                4 * sizeof(ushort),
                BufferUsage.IndexBuffer);
            _indexBuffer = factory.CreateBuffer(ibDescription);
            _graphicsDevice.UpdateBuffer(_indexBuffer, 0, quadIndices);

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

            ShaderDescription vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(VertexCode),
                "main");
            ShaderDescription fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(FragmentCode),
                "main");

            _shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);

            // Create pipeline
            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual);
            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);
            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: _shaders);
            pipelineDescription.Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription;

            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

            _commandList = factory.CreateCommandList();
        }

        private static void Draw()
        {
            // Begin() must be called before commands can be issued.
            _commandList.Begin();

            // We want to render directly to the output window.
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
            _commandList.ClearColorTarget(0, RgbaFloat.Black);

            // Set all relevant state to draw our quad.
            _commandList.SetVertexBuffer(0, _vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _commandList.SetPipeline(_pipeline);
            // Issue a Draw command for a single instance with 4 indices.
            _commandList.DrawIndexed(
                indexCount: 4,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);

            // End() must be called before commands can be submitted for execution.
            _commandList.End();
            _graphicsDevice.SubmitCommands(_commandList);

            // Once commands have been submitted, the rendered image can be presented to the application window.
            _graphicsDevice.SwapBuffers();
        }

        private static void DisposeResources()
        {
            _pipeline.Dispose();
            foreach (Shader shader in _shaders)
            {
                shader.Dispose();
            }
            _commandList.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _graphicsDevice.Dispose();
        }

        static void MainPty(string[] args)
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

    struct VertexPositionColor
    {
        public const uint SizeInBytes = 24;
        public Vector2 Position;
        public RgbaFloat Color;
        public VertexPositionColor(Vector2 position, RgbaFloat color)
        {
            Position = position;
            Color = color;
        }
    }

}