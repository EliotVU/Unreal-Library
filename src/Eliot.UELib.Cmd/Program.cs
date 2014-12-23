using System;
using UELib;

namespace Eliot.UELib.Cmd
{
    class Program
    {
        /// <summary>
        /// Basic command line app for UELib.dll.
        /// 
        /// Arguments in order:
        ///     "Path to package"
        ///     "Object group path"
        /// </summary>
        /// <param name="args">(PackagePath: Core.u) (ObjectPath: Object.Min)</param>
        static void Main( string[] args )
        {
            if( args.Length == 0 )
            {
                Console.WriteLine( "No arguments passed!" );
                return;
            }

            var filePath = args[0];
            var pkg = UnrealLoader.LoadPackage( filePath );
            pkg.InitializePackage();

            if( args.Length < 2 )
            {
                Console.WriteLine( "No object name argument found!" );
                return;
            }

            var objectGroup = args[1];
            var obj = pkg.FindObjectByGroup( objectGroup );
            if( obj != null )
            {
                var contents = obj.Decompile();
                Console.Write( contents );
            }
            else
            {
                Console.Write( "Couldn't find object by group {0}", objectGroup );
            }
            Console.ReadKey();
        }
    }
}
