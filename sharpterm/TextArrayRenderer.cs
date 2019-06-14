using System;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace SharpTerm
{
    
    public sealed class TextArrayRenderer : IDisposable
    {
        private readonly DeviceBuffer _vertexBuffer;
        private readonly DeviceBuffer _indexBuffer;
        private readonly Pipeline _pipeline;
        private readonly ResourceSet _projectionTextureResourceSet;
        private readonly ITextArray _textArray;
        private readonly CharTextureAtlas _charAtlas;
        private readonly VertexPositionColor[] _charVertices;

        public TextArrayRenderer(GraphicsDevice gd, ITextArray textArray, CharTextureAtlas charAtlas, BindableResource projectionBuffer)
        {
            var factory = gd.ResourceFactory;
            
            var surfaceTextureView = factory.CreateTextureView(charAtlas.Texture);

            // create the vertex buffer
            _vertexBuffer = factory.CreateBuffer(new BufferDescription(
                textArray.Width * textArray.Height * 4 * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));

            // setup the index buffer
            ushort[] quadIndices = { 0, 1, 2, 3 };
            _indexBuffer = factory.CreateBuffer(new BufferDescription((uint) quadIndices.Length * sizeof(ushort),
                BufferUsage.IndexBuffer));
            gd.UpdateBuffer(_indexBuffer, 0, quadIndices);
            
            // create vertex layout
            var vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2),
                new VertexElementDescription("Texture", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float4));

            // create vertex and pixel shaders
            var vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                Encoding.UTF8.GetBytes(VertexCode),
                "main");
            var fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(FragmentCode),
                "main");
            var shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
            
            // create resource layout and resource set for projection buffer and texture
            var projectionTextureResourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            _projectionTextureResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                projectionTextureResourceLayout,
                projectionBuffer,
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

            // create pipeline
            _pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription(
                    cullMode: FaceCullMode.Back,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false),
                PrimitiveTopology = PrimitiveTopology.TriangleStrip,
                ResourceLayouts = new[] {projectionTextureResourceLayout},
                ShaderSet = new ShaderSetDescription(
                    vertexLayouts: new[] {vertexLayout},
                    shaders: shaders),
                Outputs = gd.SwapchainFramebuffer.OutputDescription
            });

            _textArray = textArray;
            _charAtlas = charAtlas;
            _charVertices = new VertexPositionColor[4];
        }

        public void Render(CommandList cl)
        {
            for (uint row = 0; row < _textArray.Height; ++row)
            {
                for (uint col = 0; col < _textArray.Width; ++col)
                {
                    var c = _textArray[col, row];
                    if (c == null || c <= ' ') continue;
                    
                    var cell = _charAtlas[(char)c];
                    _charVertices[0].Position.X = col + 0;
                    _charVertices[1].Position.X = col + 1;
                    _charVertices[2].Position.X = col + 0;
                    _charVertices[3].Position.X = col + 1;

                    _charVertices[0].Position.Y = row + 0;
                    _charVertices[1].Position.Y = row + 0;
                    _charVertices[2].Position.Y = row + 1;
                    _charVertices[3].Position.Y = row + 1;

                    _charVertices[0].Texture = cell.TopLeft;
                    _charVertices[1].Texture = cell.TopRight;
                    _charVertices[2].Texture = cell.BottomLeft;
                    _charVertices[3].Texture = cell.BottomRight;

                    _charVertices[0].Color = RgbaFloat.White;
                    _charVertices[1].Color = RgbaFloat.White;
                    _charVertices[2].Color = RgbaFloat.White;
                    _charVertices[3].Color = RgbaFloat.White;

                    uint vi = (row * _textArray.Width + col) * 4;
                    cl.UpdateBuffer(_vertexBuffer, vi * VertexPositionColor.SizeInBytes, _charVertices);
                }
            }

            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, _projectionTextureResourceSet);

            // draw characters
            for (uint row = 0; row < _textArray.Height; ++row)
            {
                for (uint col = 0; col < _textArray.Width; ++col)
                {
                    var c = _textArray[col, row];
                    if (c == null || c <= ' ') continue;
                    
                    cl.DrawIndexed(
                        indexCount: 4,
                        instanceCount: 1,
                        indexStart: 0,
                        vertexOffset: (int)(row * _textArray.Width + col) * 4,
                        instanceStart: 0);
                }
            }
        }

        public void Dispose()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _pipeline.Dispose();
            _projectionTextureResourceSet.Dispose();
        }
        
        private struct VertexPositionColor
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
        
        private const string VertexCode = @"
#version 450

layout(set = 0, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};

layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 Texture;
layout(location = 2) in vec4 Color;

layout(location = 0) out vec2 fsin_TexCoords;
layout(location = 1) out vec4 fsin_Color;

void main()
{
    gl_Position = Projection * vec4(Position, 0, 1);
    fsin_TexCoords = Texture;
    fsin_Color = Color;
}";

        private const string FragmentCode = @"
#version 450

layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 1) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;

layout(set = 0, binding = 1) uniform texture2D SurfaceTexture;
layout(set = 0, binding = 2) uniform sampler SurfaceSampler;

void main()
{
    float c = texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_TexCoords).r;
    fsout_Color = vec4(c, c, c, 1) * fsin_Color;
}";
    }
}