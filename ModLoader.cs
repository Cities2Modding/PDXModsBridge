using BepInEx.Bootstrap;
using Colossal.Logging;
using Game;
using Game.Modding;
using Mono.Cecil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;

namespace PDXModsBridge
{
    internal static class ModLoader
    {
        static char _S = Path.DirectorySeparatorChar;
        static string GAME_PATH = Path.GetDirectoryName( UnityEngine.Application.dataPath );
        static string BEPINEX_PATH = Path.Combine( GAME_PATH, $"BepInEx{_S}plugins" );
        static string THUNDERSTORE_PATH = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), $"AppData{_S}Roaming{_S}Thunderstore Mod Manager{_S}DataFolder{_S}CitiesSkylines2{_S}profiles" );
        static string RMODMAN_PATH = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), $"AppData{_S}Roaming{_S}r2modmanPlus-local{_S}CitiesSkylines2{_S}profiles" );
        static ILog _logger = LogManager.GetLogger( "Cities2Modding" );
        static Dictionary<string, List<IMod>> _loadedMods = [];

        /// <summary>
        /// Scan all plugin folders for mods directories
        /// </summary>
        public static void ScanDirectory( )
        {
            try
            {
                if ( Directory.Exists( BEPINEX_PATH ) )
                {
                    _logger.Info( "[PDXModsBridge] Scanning BepInEx folder..." );
                    ProcessSource( BEPINEX_PATH );
                }

                var thunderStorePath = GetActiveThunderstoreProfile( );

                if ( !string.IsNullOrEmpty( thunderStorePath ) )
                {
                    _logger.Info( $"[PDXModsBridge] Scanning Thunderstore folder '{thunderStorePath}'..." );
                    ProcessSource( thunderStorePath );
                }

                var rModManPath = GetActiveRModManProfile( );

                if ( !string.IsNullOrEmpty( rModManPath ) )
                {
                    _logger.Info( $"[PDXModsBridge] Scanning rModMan folder '{rModManPath}'..." );
                    ProcessSource( rModManPath );
                }
            }
            catch ( Exception ex )
            {
                HandleException( ex );
            }
        }

        /// <summary>
        /// Process a source directory
        /// </summary>
        /// <param name="sourceDirectory"></param>
        private static void ProcessSource( string sourceDirectory )
        {
            var files = Directory.GetFiles( sourceDirectory, "*.dll", SearchOption.AllDirectories );

            foreach ( string file in files )
            {
                try
                {
                    if ( _loadedMods?.ContainsKey( file ) == true )
                        continue;

                    var assemblyDefinition = AssemblyDefinition.ReadAssembly( file, TypeLoader.ReaderParameters );

                    var assemblyLoadedMods = new List<IMod>( );
                    var modsToLoad = assemblyDefinition.MainModule.Types
                                          .Where( t => t != null && t.HasInterfaces && t.Interfaces?.Count( i => i.InterfaceType?.FullName == typeof( IMod ).FullName ) > 0 )
                                          .Select( t => t.ResolveReflection() )
                                          .Distinct( )
                                          .ToList( );

                    if ( modsToLoad?.Count > 0 )
                    {
                        foreach ( var modType in modsToLoad )
                        {
                            if ( modType == null )
                                continue;

                            var descriptionAttribute = modType.GetCustomAttribute<DescriptionAttribute>( );

                            if ( descriptionAttribute == null || string.IsNullOrEmpty( descriptionAttribute.Description ) || descriptionAttribute.Description.ToLowerInvariant( ) != "bridge" )
                                continue;

                            var instance = ( IMod ) Activator.CreateInstance( modType );

                            if ( instance == null )
                                continue;

                            assemblyLoadedMods.Add( instance );

                            _logger.Info( $"[PDXModsBridge] Initialised IMod from third-party mod sources: '{modType.FullName}'." );
                            UnityEngine.Debug.Log( $"[PDXModsBridge] Initialised IMod from third-party mod sources: '{modType.FullName}'." );
                        }

                        if ( assemblyLoadedMods.Count == 0 )
                        {
                            _logger.Debug( $"[PDXModsBridge] No mods to load in '{file}'." );
                        }
                    }

                    _loadedMods[file] = assemblyLoadedMods;
                    assemblyDefinition.Dispose( );
                }
                catch ( BadImageFormatException ex )
                {
                    _logger.Debug( $"Invalid .NET assembly, skipping {file}: {ex.Message}" );
                }
                catch ( Exception ex2 )
                {
                    _logger.Error( ex2.ToString( ) );
                }
            }
        }

        /// <summary>
        /// Check for mod manager active path
        /// </summary>
        /// <returns></returns>
        private static string GetActiveProfile( string path )
        {
            if ( !Directory.Exists( path ) )
                return null;

            DateTime mostRecent = DateTime.MinValue;
            var mostRecentProfile = string.Empty;

            foreach ( var profileDirectory in Directory.GetDirectories( path ) )
            {
                var bepInExPath = Path.Combine( profileDirectory, $"BepInEx{_S}plugins" );

                if ( Directory.Exists( bepInExPath ) )
                {
                    var mostRecentModified = GetMostRecentModifiedDate( bepInExPath );

                    if ( mostRecentModified > mostRecent )
                    {
                        mostRecent = mostRecentModified;
                        mostRecentProfile = bepInExPath;
                    }
                }
            }

            return mostRecentProfile;
        }


        /// <summary>
        /// Gets the most recent date modified date
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public static DateTime GetMostRecentModifiedDate( string directory )
        {
            return Directory.GetFiles( directory, "*", SearchOption.AllDirectories )
                                          .Select( file => new FileInfo( file ).LastWriteTime )
                                          .OrderByDescending( date => date )
                                          .FirstOrDefault( );
        }

        /// <summary>
        /// Get the active Thunderstore profile
        /// </summary>
        /// <returns></returns>
        private static string GetActiveThunderstoreProfile( )
        {
            return GetActiveProfile( THUNDERSTORE_PATH );
        }

        /// <summary>
        /// Get the active RModMan profile
        /// </summary>
        /// <returns></returns>
        private static string GetActiveRModManProfile( )
        {
            if ( Directory.Exists( RMODMAN_PATH ) )
            {
                return GetActiveProfile( RMODMAN_PATH );
            }
            else
            {
                String envVar = Environment.GetEnvironmentVariable( "DOORSTOP_INVOKE_DLL_PATH" );
                String dllPath = Path.GetDirectoryName( envVar );
                String profilesPath = Path.GetFullPath( Path.Combine( dllPath, "..", "..", ".." ) );

                return GetActiveProfile( profilesPath );
            }
        }

        /// <summary>
        /// Handle exceptions, use best practice of only handling
        /// exceptions we expect within our context.
        /// </summary>
        /// <param name="ex"></param>
        private static void HandleException( Exception ex )
        {
            if ( ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException ||
                ex is InvalidDataException /* For handling invalid or corrupt zip files */ ||
                ex is FileNotFoundException /* If a zip file is not found */)
            {
                _logger.Error( ex );
            }
            else
                throw ex; // Rethrow if it's not an expected exception
        }

        public static void Load( UpdateSystem updateSystem )
        {
            if ( _loadedMods == null || _loadedMods.Count == 0 )
                return;

            foreach ( var kvp in _loadedMods )
            {
                var fileName = Path.GetFileNameWithoutExtension( kvp.Key );
                var mods = kvp.Value;

                if ( mods.Any( ) )
                {
                    foreach ( var mod in mods )
                    {
                        mod.OnLoad( updateSystem );
                        _logger.Info( $"[PDXModsBridge] On load '{fileName}' ." );
                    }
                }
            }
        }

        public static void Unload( )
        {
            if ( _loadedMods == null || _loadedMods.Count == 0 )
                return;

            foreach ( var kvp in _loadedMods )
            {
                var fileName = Path.GetFileNameWithoutExtension( kvp.Key );
                var mods = kvp.Value;

                if ( mods.Any( ) )
                {
                    foreach ( var mod in mods )
                    {
                        mod.OnDispose( );
                        _logger.Info( $"[PDXModsBridge] Unloaded mod '{fileName}'." );
                    }
                }
            }
        }
    }
}
