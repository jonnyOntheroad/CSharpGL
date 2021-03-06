﻿using System;
using System.Drawing;
using System.IO;

namespace CSharpGL
{
    // Y
    // ^
    // |
    // |
    // 1--------------------0
    // |      .             |
    // |      |             |
    // |                    |
    // |    .               |
    // |   .                |
    // |  .                 |
    // | .                  |
    // 2--------------------3 --> X
    //
    /// <summary>
    /// A billboard that always faces camera in 3D world. Its size is described by Width\Height(in pixels).
    /// </summary>
    public class TextureBillboardRenderer : Renderer
    {
        #region shaders

        private const string projectionMatrix = "projectionMatrix";
        private const string viewMatrix = "viewMatrix";
        private const string modelMatrix = "modelMatrix";
        private const string width = "width";
        private const string height = "height";
        private const string screenSize = "screenSize";
        private const string tex = "tex";
        private const string transparentBackground = "transparentBackground";
        private const string delta = "delta";

        private const string vertexCode =
            @"#version 330 core

uniform mat4 " + projectionMatrix + @";
uniform mat4 " + viewMatrix + @";
uniform mat4 " + modelMatrix + @";
uniform float " + width + @";
uniform float " + height + @";
uniform vec2 " + screenSize + @";

out vec2 passUV;

const float value = 0.5;

void main(void) {
	vec2 vertexes[4] = vec2[4](vec2(value, value), vec2(-value, value), vec2(-value, -value), vec2(value, -value));
	vec2 texCoord[4] = vec2[4](vec2(1.0, 1.0), vec2(0.0, 1.0), vec2(0.0, 0.0), vec2(1.0, 0.0));

	vec4 position = projectionMatrix * viewMatrix * modelMatrix * vec4(0, 0, 0, 1);
    position = position / position.w;
    vec2 diffPos = vertexes[gl_VertexID];
    position.xy += diffPos * vec2(width, height) / screenSize ;
	gl_Position = position;

	passUV = texCoord[gl_VertexID];
}
";
        private const string fragmentCode =
            @"#version 330 core

uniform sampler2D " + tex + @";
uniform bool " + transparentBackground + @" = false;
uniform float " + delta + @" = 0.01;

in vec2 passUV;

out vec4 out_Color;

void main(void) {
    vec4 color = texture(tex, passUV);
    if (transparentBackground)
    {
        if (color.a == 0)
        {
            vec4 color0 = texture(tex, vec2(passUV.x -  delta, passUV.y - delta));
            vec4 color1 = texture(tex, vec2(passUV.x -  delta, passUV.y - 0));
            vec4 color2 = texture(tex, vec2(passUV.x -  delta, passUV.y + delta));
            vec4 color3 = texture(tex, vec2(passUV.x -  0, passUV.y - delta));
            vec4 color4 = texture(tex, vec2(passUV.x -  0, passUV.y + delta));
            vec4 color5 = texture(tex, vec2(passUV.x +  delta, passUV.y - delta));
            vec4 color6 = texture(tex, vec2(passUV.x +  delta, passUV.y - 0));
            vec4 color7 = texture(tex, vec2(passUV.x +  delta, passUV.y + delta));
            if (color0.a != 0 || color1.a != 0 || color2.a != 0 || color3.a != 0
                || color4.a != 0 || color5.a != 0 || color6.a != 0 || color7.a != 0)
            {
                out_Color = (color0 + color1 + color2 + color3 + color + color4 + color5 + color6 + color7) / 9;
            }
            else
            {
                discard;
            }
        }
        else 
        {
            out_Color = color; 
        }
    }
    else
    {
        out_Color = color; 
    }

}
";

        #endregion shaders

        /// <summary>
        /// Creates a billboard in 3D world. Its size is described by Width\Height(in pixels).
        /// </summary>
        /// <param name="textureSource"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static TextureBillboardRenderer Create(ITextureSource textureSource, int width, int height)
        {
            var vertexShader = new VertexShader(vertexCode);// this vertex shader has no vertex attributes.
            var fragmentShader = new FragmentShader(fragmentCode);
            var provider = new ShaderArray(vertexShader, fragmentShader);
            var map = new AttributeMap();
            var renderer = new TextureBillboardRenderer(textureSource, width, height, new Billboard(), provider, map);
            renderer.Initialize();

            return renderer;
        }

        private ITextureSource textureSource;
        /// <summary>
        /// Source of texture object.
        /// </summary>
        public ITextureSource TextureSource
        {
            get { return textureSource; }
            set { textureSource = value; }
        }

        private float _width;
        /// <summary>
        /// Billboard's width(in pixels).
        /// </summary>
        public int Width
        {
            get { return (int)_width; }
            set { _width = (int)value; }
        }

        private float _height;
        /// <summary>
        /// Billboard's height(in pixels).
        /// </summary>
        public int Height
        {
            get { return (int)_height; }
            set { _height = (int)value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool TransparentBackground { get; set; }

        public float Delta { get; set; }

        private TextureBillboardRenderer(ITextureSource textureSource, int width, int height, IBufferable model, IShaderProgramProvider shaderProgramProvider,
            AttributeMap attributeMap, params GLState[] switches)
            : base(model, shaderProgramProvider, attributeMap, switches)
        {
            this.TextureSource = textureSource;
            this.Width = width;
            this.Height = height;
        }

        protected override void DoRender(RenderEventArgs arg)
        {
            ICamera camera = arg.CameraStack.Peek();
            mat4 projection = camera.GetProjectionMatrix();
            mat4 view = camera.GetViewMatrix();
            mat4 model = this.GetModelMatrix();
            var viewport = new int[4];
            GL.Instance.GetIntegerv((uint)GetTarget.Viewport, viewport);
            this.SetUniform(projectionMatrix, projection);
            this.SetUniform(viewMatrix, view);
            this.SetUniform(modelMatrix, model);
            this.SetUniform(width, this._width);
            this.SetUniform(height, this._height);
            this.SetUniform(screenSize, new vec2(viewport[2], viewport[3]));
            this.SetUniform(tex, this.textureSource.BindingTexture);
            this.SetUniform(transparentBackground, this.TransparentBackground);
            this.SetUniform(delta, this.Delta);

            base.DoRender(arg);
        }

    }

    class Billboard : IBufferable
    {
        private IndexBuffer indexBuffer;

        #region IBufferable 成员

        public VertexBuffer GetVertexAttributeBuffer(string bufferName, string varNameInShader)
        {
            return null;// not need any vertex buffer.
        }

        public IndexBuffer GetIndexBuffer()
        {
            if (this.indexBuffer == null)
            {
                this.indexBuffer = ZeroIndexBuffer.Create(DrawMode.Quads, 0, 4);
            }

            return this.indexBuffer;
        }

        #endregion
    }
}