using MSCLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TommoJProductions.TurboMod
{
    public class TurboMod : Mod
    {
        // Project start, 26.10.2020

        #region CONSTRAINTS


        /// <summary>
        /// Represents if the mod has been complied for x64
        /// </summary>
#if x64
        internal const bool IS_64_BIT = true;
#else
        internal const bool IS_64_BIT = false;
#endif

        #endregion

        #region Mod Properties

        public override string ID => "turboMod";
        public override string Name => "Turbo Mod";
        public override string Version => "0.1";

        public override string Author => "tommjphillips";

        #endregion

        #region Fields

        /// <summary>
        /// Represents the log.
        /// </summary>
        private StringBuilder log;
        /// <summary>
        /// Represents the turbo mod assets.
        /// </summary>
        private TurboModAssets modAssets;

        #endregion

        #region Properties

        internal bool assetsLoaded { get; private set; }
        internal static TurboMod instance
        {
            get;
            private set;
        }

        #endregion

        #region Constructors

        public TurboMod()
        {
            // Written, 26.10.2020

            if (instance == null)
                instance = this;
            else
                throw new Exception("THERES ALREADY AN ACTIVE INSTANCE");
            this.log = new StringBuilder();
            this.log.Append("---START_LOG---");

            print("{0} v{1}: Ready", this.Name, this.Version);
        }

        #endregion

        #region Methods

        // INTERNAL METHODS
        /// <summary>
        /// Returns the log.
        /// </summary>
        internal string getLog()
        {
            // Written, 26.10.2020

            return this.log.ToString();
        }
        /// <summary>
        /// Prints a message to the console and amends it to the log.
        /// </summary>
        /// <param name="inMessage">The message to log</param>
        internal static void print(string inMessage, params object[] _objects)
        {
            // Written, 26.10.2020


            ModConsole.Print(instance.log.AppendFormat(string.Format("{0} {1}", instance.Name, inMessage), _objects).ToString(inMessage.Length, inMessage.Length));
        }

        // PRIVATE METHODS
        /// <summary>
        /// Loads all turbo assets.
        /// </summary>
        private bool loadAssets() 
        {
            // Written, 26.10.2020

            try
            {
                AssetBundle ab = LoadAssets.LoadBundle(this, "dayzturbo.unity3d");
                this.modAssets = new TurboModAssets(ab.LoadAsset("TURBOPARTS.prefab") as GameObject);
                assetsLoaded = true;
                ab.Unload(false);
                print("Asset bundle loaded and unloaded successfully.");
            }
            catch (Exception ex)
            {
                print("Error when loading asset bundle: M:{0}\nSTACKTRACE:{1}", ex.Message, ex.StackTrace);
                assetsLoaded = false;
            }
            return assetsLoaded;
        }
        private void initParts() 
        {
            // Written, 26.10.2020

            
        }

        #endregion

        #region Mod Methods

        public override void OnLoad()
        {
            // Written, 26.10.2020

            if (this.loadAssets())
            {
                this.initParts();
            }
            print("{0} v{1}: Loaded", this.Name, this.Version);
        }

        #endregion
    }
}
