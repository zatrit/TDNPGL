﻿using SkiaSharp.Views.Desktop;
using SkiaSharp;

using System.Runtime.ExceptionServices;
using System.Reflection;
using System.Linq;
using System;
using System.Windows.Forms;

using TDNPGL.Core.Debug;
using TDNPGL.Core;
using TDNPGL.Core.Gameplay.Assets;
using TDNPGL.Core.Math;
using TDNPGL.Core.Graphics.Renderers;

namespace TDNPGL.Views.WinForms
{
    public partial class GameRendererControl : SKGLControl,IGameRenderer
    {
        public double PixelSize => ScreenCalculations.CalculatePixelSize(width, height); 

        public double width => Width;
        public double height => Height;

        public bool Rendering = true; 
        
        public void InitGame(Assembly assembly,string GameName)
        {
            TDNPGL.Core.Game.Init(this, assembly,GameName,true);
        }
        public void InitGame<EntryType>(string GameName) where EntryType : EntryPoint => InitGame(Assembly.GetAssembly(typeof(EntryType)), GameName);

        public SKBitmap CurrentGameBitmap
        {
            get
            {
                return currentGameBitmap;
            }
            set
            {
                currentGameBitmap.Dispose();
                currentGameBitmap = value;
            }
        }
        private SKBitmap currentGameBitmap = new SKBitmap();

        public GameRendererControl()
        {
            InitializeComponent();
        }

        public void InitializeComponent()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            this.SizeChanged += GameRendererControl_SizeChanged;
            this.VSync = true;
            this.PaintSurface += new System.EventHandler<SkiaSharp.Views.Desktop.SKPaintGLSurfaceEventArgs>(this.This_PaintSurface);
            this.SizeChanged += new System.EventHandler(this.skglControl1_SizeChanged);
            this.MouseClick += GameRendererControl_MouseClick;
        }

        private void GameRendererControl_MouseClick(object sender, MouseEventArgs e)
        {
            SKPoint point = new SKPoint(e.X, e.Y);
            MouseButtons[] buttons = { MouseButtons.Left, MouseButtons.Middle, MouseButtons.Right };
            int b = buttons.ToList().IndexOf(e.Button);
            Game.MouseClick(b,point);
        }

        private void GameRendererControl_SizeChanged(object sender, EventArgs e)
        {
        }

        [HandleProcessCorruptedStateExceptions]
        private void This_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintGLSurfaceEventArgs e)
        {
            try
            {
                e.Surface.Canvas.Clear(SKColors.Black);
                e.Surface.Canvas.DrawBitmap(CurrentGameBitmap, new SKRect(0, 0, (float)width, (float)height));
            }
            catch (Exception ex)
            {
                if (ex is AccessViolationException)
                {
                    Logging.MessageAction("GAMECONTROL", ex.Message + " on rendering game on canvas!", ConsoleColor.Red);
                    return;
                }
                Logging.WriteError(ex);
                CurrentGameBitmap.Dispose();
            }
        }

        public void DrawBitmap(SKBitmap bitmap)
        {
            if (!Rendering)
            {
                Rendering = true;
                return;
            }

            CurrentGameBitmap = bitmap;

            Invalidate();
        }

        private void skglControl1_SizeChanged(object sender, EventArgs e)
        {

        }
    }
}
