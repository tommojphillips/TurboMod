using System.Reflection;
using System.Resources;

// General Information
[assembly: AssemblyTitle("Turbo Mod")]
[assembly: AssemblyDescription("A turbo mod for My Summer Car")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("TommoJProductions")]
[assembly: AssemblyProduct("TurboMod")]
[assembly: AssemblyCopyright("Copyright © Tommo J. Productions 2022")]
[assembly: AssemblyTrademark("Azine")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en-AU")]
// Version information
[assembly: AssemblyVersion("0.1.457.3")]
//[assembly: AssemblyFileVersion("0.1.457.3")]

public class VersionInfo
{
	public const string lastestRelease = "03.04.2023 07:17 PM";
	public const string version = "0.1.457.3";

    /// <summary>
    /// Represents if the mod has been complied for x64
    /// </summary>
    #if x64
        internal const bool IS_64_BIT = true;
    #else
        internal const bool IS_64_BIT = false;
    #endif
    #if DEBUG
        internal const bool IS_DEBUG_CONFIG = true;
    #else
        internal const bool IS_DEBUG_CONFIG = false;
    #endif
}

