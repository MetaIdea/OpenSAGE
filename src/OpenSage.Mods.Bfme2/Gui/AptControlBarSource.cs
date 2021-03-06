﻿using System;
using System.Collections.Generic;
using System.IO;
using OpenSage.Data;
using OpenSage.Data.Apt.Characters;
using OpenSage.Gui.Apt;
using OpenSage.Gui.Apt.ActionScript;
using OpenSage.Gui.Apt.ActionScript.Opcodes;
using OpenSage.Gui.ControlBar;
using OpenSage.Logic;
using OpenSage.Mods.Bfme2.Gui;
using Veldrid.ImageSharp;
using ValueType = OpenSage.Gui.Apt.ActionScript.ValueType;

namespace OpenSage.Mods.Bfme2
{
    class AptControlBar : IControlBar
    {
        Game _game;
        AptWindow _window;
        SpriteItem _root;

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public AptControlBar(Game game)
        {
            _game = game;
        }

        public void AddToScene(Scene2D scene2D)
        {
            AptPalantir.Reset();

            _window = _game.LoadAptWindow("Palantir.apt");
            _root = _window.Root;

            _game.Scene2D.AptWindowManager.PushWindow(_window);
        }

        private bool _commandbarVisible = false;
        private bool _palantirInitialized = false;
        private bool _minimapInitialized = false;

        private void SetMinimap()
        {
            if (!_minimapInitialized)
            {
                var radar = _root.ScriptObject.GetMember("Radar").ToObject();
                var radarClipValue = radar.GetMember("RadarClip");

                if(radarClipValue.Type == ValueType.Undefined)
                {
                    return;
                }

                var mapCache = _game.CurrentMap;
                var mapPath = FileSystem.NormalizeFilePath(mapCache.Name);
                var basePath = Path.GetDirectoryName(mapPath) + "/" + Path.GetFileNameWithoutExtension(mapPath);

                // Minimap images drawn by an artist
                var mapArtPath = basePath + "_art.tga";
                var minimapTexture = _game.AssetStore.GuiTextures.GetByName(mapArtPath).Texture;

                if(minimapTexture == null)
                {
                    // Fallback minimap images generated by the worldbuilder
                    mapArtPath = basePath + ".tga";
                    minimapTexture = _game.AssetStore.GuiTextures.GetByName(mapArtPath).Texture;
                }

                var radarClip = radarClipValue.ToObject().Item as SpriteItem;
                var shape = radarClip.Content.Items[1] as RenderItem;
                shape.Visible = false;

                var radarChild = ((SpriteItem)radar.Item).Content.Items[1] as SpriteItem;
                shape = radarChild.Content.Items[1] as RenderItem;
                shape.Texture = minimapTexture;

                _minimapInitialized = true;
            }
        }

        private void InitializePalantir(Player player)
        {
            if (!_palantirInitialized)
            {
                logger.Info("Initialize Palantir!");

                var showCommandInterface = _root.ScriptObject.GetMember("SetPalantirFrameState");
                if (showCommandInterface.Type != ValueType.Undefined)
                {
                    bool good = Array.Exists(player.Template.IntrinsicSciences, s => s == "SCIENCE_GOOD");
                    List<Value> emptyArgs = new List<Value>();
                    emptyArgs.Add(Value.FromString(good ? "_good" : "_evil"));
                    FunctionCommon.ExecuteFunction(showCommandInterface, emptyArgs.ToArray(), _root.ScriptObject, _window.Context.Avm);
                    _palantirInitialized = true;
                }
            }
        }

        private void UpdateSideCommandbar(Player player)
        {
            var sideCommandBar = _root.ScriptObject.GetMember("SideCommandBar").ToObject();

            if (player.SelectedUnits.Count > 0 && !_commandbarVisible)
            {
                var fadeIn = sideCommandBar.Item.ScriptObject.GetMember("FadeIn");

                if (fadeIn.Type != ValueType.Undefined)
                {
                    List<Value> emptyArgs = new List<Value>();
                    FunctionCommon.ExecuteFunction(fadeIn, emptyArgs.ToArray(), sideCommandBar.Item.ScriptObject, _window.Context.Avm);
                    _commandbarVisible = true;
                }
            }
            else if (player.SelectedUnits.Count == 0 && _commandbarVisible)
            {
                var fadeOut = sideCommandBar.Item.ScriptObject.GetMember("FadeOut");

                if (fadeOut.Type != ValueType.Undefined)
                {
                    List<Value> emptyArgs = new List<Value>();
                    FunctionCommon.ExecuteFunction(fadeOut, emptyArgs.ToArray(), sideCommandBar.Item.ScriptObject, _window.Context.Avm);
                    _commandbarVisible = true;
                }

                _commandbarVisible = false;
            }
        }

        public void Update(Player player)
        {
            if (AptPalantir.Initialized)
            {
                InitializePalantir(player);
                SetMinimap();
                if (AptPalantir.SideButtonsInitialized == 12)
                {
                    UpdateSideCommandbar(player);
                }
            }
        }
    }

    class AptControlBarSource : IControlBarSource
    {
        public IControlBar Create(string side, Game game)
        {
            return new AptControlBar(game);
        }
    }
}
