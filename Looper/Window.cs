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
        private const double rotationDuration = 0.2;
        private static int rotationFrames;
        private static readonly Game game = new Game();
        private static int wpadding;
        private static int hpadding;
        private int[] textures;
        private int leveltexture;
        private bool prevmdown, prevrdown;
        private int mx, my;
        private int rotation;
        private int rotatingx = -1, rotatingy = -1;

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

            rotationFrames = (int)Math.Round(rotationDuration * TargetUpdateFrequency);

            GL.ClearColor(Color.FromArgb(30, 30, 50));
            GL.Ortho(0, Width, Height, 0, -1, 1);
            GL.Viewport(ClientSize);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.Map2Vertex4);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        }

        private void LoadTextures()
        {
            var bmp = new Bitmap(@"C:\Users\swirly\Desktop\shapes.png");
            int shapes = bmp.Width / size;

            textures = new int[shapes];

            for (int i = 0; i < shapes; i++)
                textures[i] = LoadTexture(bmp.Clone(new Rectangle(i * size, 0, size, size), bmp.PixelFormat));
        }

        private int GetTexture(Game.Shape shape)
        {
            return textures[(int)shape - 1];
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

            //if (!Focused)
            //    return;

            if (rotation > 0)
                rotation++;

            if (rotation >= rotationFrames)
            {
                rotation = 0;
                rotatingx = -1;
                rotatingy = -1;
                GL.Color4(game.IsSolved() ? Color.Orange : Color.White);
            }

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

                    if (x < game.Width && y < game.Height && game.GetShape(x, y) != Game.Shape.None)
                    {
                        game.Rotate(x, y);
                        rotation++;
                        rotatingx = x;
                        rotatingy = y;
                        GL.Color4(Color.White);
                    }
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

            //Draw(0, 0, game.Width * drawSize, game.Height * drawSize, leveltexture);

            for (int x = 0; x < game.Width; x++)
                for (int y = 0; y < game.Height; y++)
                    DrawItem(x, y);

            SwapBuffers();
        }

        private static double Smooth(double x)
        {
            return (Math.Cos(Math.PI * (x - 1)) + 1) / 2;
        }

        private void DrawItem(int x, int y)
        {
            var shape = game.GetShape(x, y);

            if (shape != Game.Shape.None)
            {
                double angle = ((int)game.GetRotation(x, y) - 1) * Math.PI / 2;

                if (rotatingx == x && rotatingy == y)
                    angle += (Smooth((double)rotation / rotationFrames) - 1) * Math.PI / 2;

                DrawAngle((x + 0.5) * drawSize, (y + 0.5) * drawSize, drawSize, drawSize, angle, GetTexture(shape));
            }
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

        private static void DrawAngle(double x, double y, int w, int h, double angle, int texture)
        {
            GL.PushMatrix();
            GL.Translate(x * wcoef, y * hcoef, 0);

            double w1 = Math.Cos(angle) * h / 2;
            double h1 = Math.Sin(angle) * h / 2;

            double w2 = Math.Cos(Math.PI / 2 + angle) * w / 2;
            double h2 = Math.Sin(Math.PI / 2 + angle) * w / 2;

            double x1 = w1 - w2;
            double y1 = h1 - h2;

            double x2 = w1 + w2;
            double y2 = h1 + h2;

            double x3 = -w1 + w2;
            double y3 = -h1 + h2;

            double x4 = -w1 - w2;
            double y4 = -h1 - h2;

            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.Begin(PrimitiveType.Quads);

            GL.TexCoord2(0, 0);
            GL.Vertex2(x1, y1);

            GL.TexCoord2(1, 0);
            GL.Vertex2(x2, y2);

            GL.TexCoord2(1, 1);
            GL.Vertex2(x3, y3);

            GL.TexCoord2(0, 1);
            GL.Vertex2(x4, y4);

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
