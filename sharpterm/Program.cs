using System;
using System.IO;
using System.Net;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.SPIRV;
using SharpFont;
using Encoding = System.Text.Encoding;

namespace GettingStarted
{
    class Program
    {
        private static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;
        private static DeviceBuffer _vertexBuffer;
        private static DeviceBuffer _indexBuffer;
        private static Shader[] _shaders;
        private static Pipeline _pipeline;
        private static ResourceSet _textureSet;

        private const string VertexCode = @"
#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 Texture;
layout(location = 2) in vec4 Color;

layout(location = 0) out vec2 fsin_TexCoords;

void main()
{
    gl_Position = vec4(Position, 0, 1);
    fsin_TexCoords = Texture;
}";

        private const string FragmentCode = @"
#version 450

layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 0) out vec4 fsout_Color;

layout(set = 1, binding = 1) uniform texture2D SurfaceTexture;
layout(set = 1, binding = 2) uniform sampler SurfaceSampler;

void main()
{
    float c = texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_TexCoords).r;
    fsout_Color = vec4(c, c, c, 1);
}";

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

            CreateResources();

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

        private class CharCell
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

        private static void CreateResources()
        {
            ResourceFactory factory = _graphicsDevice.ResourceFactory;
            
            var lib = new Library();
            var face = new Face(lib, @"./Inconsolata-Regular.ttf");
            face.SetPixelSizes(96, 0);
            uint font_width = face.Size.Metrics.NominalWidth;
            uint font_height = (uint)face.Size.Metrics.Height.ToInt32();

            var surfaceTexture = factory.CreateTexture(new TextureDescription(
                16 * font_width, 16 * font_height, 1, 1, 1,
                PixelFormat.R8_UNorm, TextureUsage.Sampled, TextureType.Texture2D));

            uint x = 0, y = 0;
            for (uint ci = 0; ci < 256; ci++)
            {
                var c = (char)ci;
                if (ci % 16 == 0) {
                    x = 0;
                    y = font_height * (ci / 16);
                }
                face.LoadChar((uint)Char.ConvertToUtf32(c.ToString(), 0), LoadFlags.Render, LoadTarget.Normal);
                var bmpIn = face.Glyph.Bitmap;
	
                uint xpos = x + (uint)face.Glyph.BitmapLeft;
                uint ypos = y - (uint)face.Glyph.BitmapTop + (uint)face.Size.Metrics.Ascender.ToInt32();
                
                //g.DrawRectangle(Pens.Red, x, y, face.Glyph.Advance.X.ToInt32(), font_height);
                Cells[ci] = new CharCell
                {
                    X = x / (16f * font_width),
                    Y = y / (16f * font_height),
                    W = face.Glyph.Advance.X.ToSingle() / (16f * font_width),
                    H = 1f / 16f
                };

                _graphicsDevice.UpdateTexture(surfaceTexture, bmpIn.Buffer, (uint)(bmpIn.Rows * bmpIn.Width),
                    xpos, ypos, 0, (uint)bmpIn.Width, (uint)bmpIn.Rows, 1, 0, 0);

                x += font_width;
            }
            
            var surfaceTextureView = factory.CreateTextureView(surfaceTexture);

            var cell = Cells['&'];
            var height = 96f / 1920f;
            var width = cell.Aspect * height;
            VertexPositionColor[] quadVertices =
            {
                new VertexPositionColor(new Vector2(-width / 2f, height / 2f), new Vector2(cell.X, cell.Y),  RgbaFloat.Red),
                new VertexPositionColor(new Vector2(width / 2f, height / 2f), new Vector2(cell.R, cell.Y), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(-width / 2f, -height / 2f), new Vector2(cell.X, cell.B), RgbaFloat.Blue),
                new VertexPositionColor(new Vector2(width / 2f, -height / 2f), new Vector2(cell.R, cell.B), RgbaFloat.Yellow)
            };
            BufferDescription vbDescription = new BufferDescription(
                (uint)quadVertices.Length * VertexPositionColor.SizeInBytes,
                BufferUsage.VertexBuffer);
            _vertexBuffer = factory.CreateBuffer(vbDescription);
            _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, quadVertices);

            ushort[] quadIndices = { 0, 1, 2, 3 };
            BufferDescription ibDescription = new BufferDescription(
                (uint)quadIndices.Length * sizeof(ushort),
                BufferUsage.IndexBuffer);
            _indexBuffer = factory.CreateBuffer(ibDescription);
            _graphicsDevice.UpdateBuffer(_indexBuffer, 0, quadIndices);
            
            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Texture", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
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

            var textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

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
            pipelineDescription.ResourceLayouts = new[] {textureLayout};
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new[] { vertexLayout },
                shaders: _shaders);
            pipelineDescription.Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription;
            

            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

            _textureSet = factory.CreateResourceSet(new ResourceSetDescription(
                textureLayout,
                surfaceTextureView,
                factory.CreateSampler(new SamplerDescription
                {
                    AddressModeU = SamplerAddressMode.Clamp,
                    AddressModeV = SamplerAddressMode.Clamp,
                    AddressModeW = SamplerAddressMode.Clamp,
                    Filter = SamplerFilter.Anisotropic,
                    LodBias = 0,
                    MinimumLod = 0,
                    MaximumLod = uint.MaxValue,
                    MaximumAnisotropy = 4
                })));

            _commandList = factory.CreateCommandList();
        }

        private static void Draw()
        {
            // Begin() must be called before commands can be issued.
            _commandList.Begin();

            // We want to render directly to the output window.
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
            _commandList.ClearColorTarget(0, RgbaFloat.Clear);

            // Set all relevant state to draw our quad.
            _commandList.SetVertexBuffer(0, _vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _commandList.SetPipeline(_pipeline);
            _commandList.SetGraphicsResourceSet(0, _textureSet);
            // Issue a Draw command for a single instance with 4 indices.
            _commandList.DrawIndexed(
                indexCount: _indexBuffer.SizeInBytes / sizeof(ushort),
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
    }

    struct VertexPositionColor
    {
        public const uint SizeInBytes = 32;
        public Vector2 Position;
        public Vector2 Texture;
        public RgbaFloat Color;
        public VertexPositionColor(Vector2 position, Vector2 texture, RgbaFloat color)
        {
            Texture = texture;
            Position = position;
            Color = color;
        }
    }
}