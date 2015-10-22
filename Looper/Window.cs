using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace Looper
{
    class Window : GameWindow
    {
        private const int width = 500;
        private const int height = 700;
        private const double wcoef = 2d / width;
        private const double hcoef = -2d / height;
        private const int size = 50;
        private const int drawSize = 50;
        private static readonly Game game = new Game();
        private static int wpadding;
        private static int hpadding;
        private int[,] textures;
        private int leveltexture;
        private bool prevmdown, prevrdown;
        private int mx, my;

        public Window() : base(width, height, GraphicsMode.Default, "Looper", GameWindowFlags.FixedWindow) { }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            LoadTextures();

            using (var bmp = new Bitmap(100, 100))
            using (var gfx = Graphics.FromImage(bmp))
            {
                gfx.Clear(Color.FromArgb(100, 100, 100, 100));
                leveltexture = LoadTexture(bmp);
            }

            SetNewLevel();

            GL.ClearColor(Color.Navy);
            GL.Ortho(0, Width, Height, 0, -1, 1);
            GL.Viewport(ClientSize);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        }

        private void LoadTextures()
        {
            var bmp = new Bitmap(@"C:\Users\swirly\Desktop\shapes.png");
            int shapes = bmp.Width / size;

            textures = new int[shapes, 4];

            for (int i = 0; i < shapes; i++)
            {
                var part = bmp.Clone(new Rectangle(i * size, 0, size, size), bmp.PixelFormat);

                for (int j = 0; j < 4; j++)
                {
                    textures[i, j] = LoadTexture((Bitmap)part.Clone());
                    part.RotateFlip(RotateFlipType.Rotate90FlipNone);
                }
            }
                
        }

        private int GetTexture(Game.Shape shape, Game.Rotation rotation)
        {
            return textures[(int)shape - 1, (int)rotation];
        }

        private static void SetNewLevel()
        {
            wpadding = (width - drawSize * game.Width) / 2;
            hpadding = (height - drawSize * game.Height) / 2;
            GL.Color4(game.IsSolved() ? Color.Orange : Color.White);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (!Focused)
                return;

            KeyboardState ks = OpenTK.Input.Keyboard.GetState();
            MouseState ms = OpenTK.Input.Mouse.GetCursorState();

            var p = PointToClient(new Point(ms.X, ms.Y));
            mx = p.X;
            my = p.Y;

            var mdown = ms.IsButtonDown(MouseButton.Left);
            var rdown = ms.IsButtonDown(MouseButton.Right);

            if (rdown && !prevrdown)
            {
                game.NewLevel();
                SetNewLevel();
            }

            if (mdown && !prevmdown)
            {
                int x = mx - wpadding;
                int y = my - hpadding;

                if (x >= 0 && y >= 0)
                {
                    x /= drawSize;
                    y /= drawSize;

                    if (x < game.Width && y < game.Height)
                        game.Rotate(x, y);

                    GL.Color4(game.IsSolved() ? Color.Orange : Color.White);
                }
            }

            prevmdown = mdown;
            prevrdown = rdown;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Translate(wpadding * wcoef, hpadding * hcoef, 0);

            Draw(0, 0, game.Width * drawSize, game.Height * drawSize, leveltexture);

            for (int x = 0; x < game.Width; x++)
                for (int y = 0; y < game.Height; y++)
                    DrawItem(x, y);

            SwapBuffers();
        }

        private void DrawItem(int x, int y)
        {
            var shape = game.GetShape(x, y);

            if (shape != Game.Shape.None)
                Draw(x * drawSize, y * drawSize, drawSize, drawSize, GetTexture(shape, game.GetRotation(x, y)));
        }

        private static void Draw(int x, int y, int w, int h, int texture)
        {
            GL.PushMatrix();
            GL.Translate(x * wcoef, y * hcoef, 0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.Begin(PrimitiveType.Quads);

            GL.TexCoord2(0, 0);
            GL.Vertex2(0, 0);

            GL.TexCoord2(1, 0);
            GL.Vertex2(w, 0);

            GL.TexCoord2(1, 1);
            GL.Vertex2(w, h);

            GL.TexCoord2(0, 1);
            GL.Vertex2(0, h);

            GL.End();
            GL.PopMatrix();
        }

        private static int LoadTexture(Bitmap bitmap)
        {
            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.Repeat);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            System.Drawing.Imaging.BitmapData bitmap_data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, bitmap.Width, bitmap.Height, PixelFormat.Bgra, PixelType.UnsignedByte, bitmap_data.Scan0);
            bitmap.UnlockBits(bitmap_data);
            bitmap.Dispose();
            GL.BindTexture(TextureTarget.Texture2D, 0);
            return texture;
        }
    }
}
