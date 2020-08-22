﻿using TDNPGL.Core.Debug;
using TDNPGL.Core.Gameplay.Assets;
using TDNPGL.Core.Gameplay.LowLevel;
using TDNPGL.Core.Graphics;
using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Linq;
using TDNPGL.Core.Gameplay.Interfaces;
using TDNPGL.Core.Debug.Exceptions;
using System.Threading;

namespace TDNPGL.Core.Gameplay
{
    public class GameObject : IParentable, IEquatable<GameObject>, IComparable, IUpdateable
    {
        #region Private Fields
        [JsonIgnore]
        protected bool firstTick = true;
        #endregion
        #region Delegate
        public GameObjectEventHandler Tick { set; internal get; }
        #endregion
        #region Animation
        private bool Paused = false;
        [JsonProperty("sprite")]
        public string SpriteName
        {
            get
            {
                return spritename;
            }
            set
            {
                spritename = value;
                Sprite = Graphics.Sprite.Sprites[value];
            }
        }
        [JsonIgnore]
        private string spritename;
        [JsonIgnore]
        public Sprite Sprite;
        [JsonIgnore]
        public int SpriteIndex;
        [JsonIgnore]
        public SKBitmap SpriteBitmap => this.Sprite.Frames[SpriteIndex];

        [JsonProperty("anim_offset")]
        public int UpdateOffset;
        [JsonProperty("z_layer")]
        public int ZLayer = 0;

        [JsonIgnore]
        private Task AnimationTask;

        public void BeginAnimation()
        {
            AnimationTask = new Task(() =>
            {
                while (true)
                {
                    while (Paused) ;
                    #region Animate
                    if (SpriteIndex == Sprite.Frames.Length - 1) SpriteIndex = 0;
                    else SpriteIndex++;
                    #endregion
                    Task.Delay(this.UpdateOffset).Wait();
                }
            }
            );
            AnimationTask.Start();
        }
        public void StopAnimation() => AnimationTask.Dispose();
        public void PauseAnimation() => Paused = true;
        public void ResumeAnimation() => Paused = false;
        public void ChangeAnimationState() => Paused = !Paused;
        #endregion
        #region Controllers
        [JsonIgnore]
        public bool Loaded { get; private set; } = false;
        [JsonIgnore]
        internal int LevelID;
        [JsonProperty("aabb")]
        public AABB AABB;
        [JsonIgnore]
        public SKPoint Position => AABB.min;
        [JsonIgnore]
        public SKSize Size => new SKSize(AABB.max.X - AABB.min.X, AABB.max.Y - AABB.min.Y);
        [JsonIgnore]
        public SKRect Rect => AABB.ToRect();

        public IParentable Parent { get; set; }

        [HandleProcessCorruptedStateExceptions]
        private static void OnTick(GameObject @object)
        {
            try
            {
                if (@object.firstTick)
                    @object.OnFirstTick();
                @object.OnTick();
                @object.firstTick = false;

                if (@object.Parent == null)
                    return;

                List<GameObject> objects = (@object?.Parent as Level)?.Objects.ToList();
                foreach (GameObject obj in objects)
                {
                    if (AABB.AABBvsAABB(obj.AABB, @object.AABB))
                    {
                        obj.CollidedWith(@object);
                        @object.CollidedWith(obj);
                    }
                }
            }
            catch
            {
            }
        }
        internal void Render(SKCanvas canvas)
        {
            SKRect rect = Rect;
            double pixelSize = GraphicsOutput.GetMainRenderer().PixelSize;

            IParentable parent = Parent;
            while (!(parent is Level))
                parent = parent.Parent;

            Level level = parent as Level;

            rect.Left = (float)(rect.Left * pixelSize) + (float)(level.CameraPosition.X * pixelSize);
            rect.Right = (float)(rect.Right * pixelSize) + (float)(level.CameraPosition.X * pixelSize);
            rect.Bottom = (float)(rect.Bottom * pixelSize) + (float)(level.CameraPosition.Y * pixelSize);
            rect.Top = (float)(rect.Top * pixelSize) + (float)(level.CameraPosition.Y * pixelSize);

            canvas.DrawBitmap(SpriteBitmap, rect);
        }
        #endregion
        #region Scripting
        [JsonProperty("scripts")]
        public string[] scripts;
        [JsonIgnore]
        public List<GameObjectScript> Scripts = new List<GameObjectScript>();
        internal void CollidedWith(GameObject obj)
        {
            foreach (GameObjectScript script in Scripts)
            {
                ThreadPool.QueueUserWorkItem((object state) =>
                script.OnCollideWith(state as GameObject),obj);
            }
        }
        internal void LoadScripts()
        {
            if (scripts != null)
                foreach (string scriptName in scripts)
                {
                    Type type = Game.CurrentEntry.GetScript(scriptName);
                    if (type == null)
                        throw new AssetsException(Parent as Level, "Script not found!");
                    GameObjectScript script = Activator.CreateInstance(type, this) as GameObjectScript;
                    Scripts.Add(script);
                }
            else
                Console.WriteLine($"Wrong object format!");
        }
        #endregion
        #region Implementations
        public bool Equals(GameObject other)
        {
            return other.ZLayer.Equals(ZLayer);
        }
        public int CompareTo(object obj)
        {
            return obj is GameObject ? (obj as GameObject).ZLayer.CompareTo(ZLayer) : -1;
        }
        public virtual void OnTick()
        {
            foreach (GameObjectScript script in Scripts)
            {
                ThreadPool.QueueUserWorkItem((object state) =>
                script.OnTick());
            }
        }
        public virtual void OnCreate()
        {
            LoadScripts();
            foreach (GameObjectScript script in Scripts)
            {
                ThreadPool.QueueUserWorkItem((object state) =>
                script.OnCreate());
            }
            Loaded = true;
        }
        public virtual void OnFirstTick()
        {
            foreach (GameObjectScript script in Scripts)
            {
                ThreadPool.QueueUserWorkItem((object state) =>
                script.OnFirstTick());
            }
        }
        #endregion
        #region Constructors
        [JsonConstructor]
        internal GameObject(IParentable parent)
        {
            this.Parent = parent;
            SKBitmap bmp = new SKBitmap(1, 1);
            bmp.Erase(SKColors.White);
            Sprite = new Sprite(bmp);
            this.AABB = new AABB() { max = new SKPoint(100, 100) };
            this.BeginAnimation();

            Tick += OnTick;
        }
        public GameObject(IParentable parent, string sprite) : this(parent, Sprite.Sprites[sprite])
        {

        }
        public GameObject(IParentable parent, Sprite sprite) : this(parent)
        {
            this.Sprite = sprite;
        }
        #endregion
    }
}
