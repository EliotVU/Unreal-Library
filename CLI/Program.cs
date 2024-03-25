using System;
using System.Linq;

namespace UELib.CLI
{
    public static class Program
    {
        /// <summary>
        /// Basic command line app for UELib.dll.
        /// 
        /// The CLI parses the commandline as following:
        /// <code>["path to file"] ["command"]</code>
        /// For example: <code>"Core.u" "obj decompile Object"</code>
        /// Command format: <code>[command] [action] [object path]</code>
        /// Supported commands:
        /// </summary>
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Missing file path!");
                return;
            }

            string filePath = args[0];
            var pkg = UnrealLoader.LoadPackage(filePath);
            pkg.InitializePackage(UnrealPackage.InitFlags.Construct | UnrealPackage.InitFlags.RegisterClasses);

            string[] cmdArgs;
            if (args.Length > 1)
            {
                cmdArgs = args[1].Split(' ');
            }
            else
            {
                string input = Console.ReadLine();
                if (input == null) return;

                cmdArgs = input.Split(' ');
            }

            performCommand:
            Console.WriteLine("Performing command: '{0}'", string.Join(" ", cmdArgs));
            string commandName = cmdArgs.Length > 0 ? cmdArgs[0] : "unspecified";
            switch (commandName.ToLowerInvariant())
            {
                case "obj":
                    string actionName = cmdArgs.Length > 1 ? cmdArgs[1] : "unspecified";
                    switch (actionName.ToLowerInvariant())
                    {
                        case "index":
                        {
                            string parm = cmdArgs.Length > 2 ? cmdArgs[2] : null;
                            int index = int.Parse(parm);
                            var obj = pkg.GetIndexObject(index);
                            if (obj == null)
                            {
                                Console.Error.WriteLine("Invalid index");
                                break;
                            }

                            Console.WriteLine($"Object:{obj.GetPath()}");
                            break;
                        }
                        
                        case "list":
                        {
                            string classLimitor = cmdArgs.Length > 2 ? cmdArgs[2] : null;
                            if (classLimitor == null)
                            {
                                foreach (var obj in pkg.Objects)
                                    Console.WriteLine(obj.GetReferencePath());
                                break;
                            }

                            foreach (var obj in pkg.Objects.Where(e => e.Class?.Name == classLimitor))
                                Console.WriteLine(obj.GetReferencePath());
                            break;
                        }

                        case "get":
                        {
                            string objectPath = cmdArgs.Length > 2 ? cmdArgs[2] : null;
                            if (objectPath == null)
                            {
                                Console.Error.WriteLine("Missing object path");
                                break;
                            }

                            var obj = pkg.FindObjectByGroup(objectPath);
                            if (obj == null)
                            {
                                Console.Error.WriteLine("Couldn't find object by path '{0}'", objectPath);
                                break;
                            }

                            string propertyName = cmdArgs.Length > 3 ? cmdArgs[3] : null;
                            if (propertyName == null)
                            {
                                Console.Error.WriteLine("Missing property name");
                                break;
                            }


                            // Hack: Temporary workaround the limitations of UELib.
                            obj.BeginDeserializing();
                            var defaultProperty = obj.Properties?.Find(propertyName);
                            if (defaultProperty == null)
                            {
                                Console.Error.WriteLine("Object '{0}' has no property of the name '{1}'",
                                    obj.GetPath(), propertyName);
                                break;
                            }

                            string output = defaultProperty.Decompile();
                            int letIndex = output.IndexOf('=');
                            if (letIndex != -1) output = output.Substring(output.IndexOf('=') + 1);
                            Console.WriteLine(output);
                            break;
                        }

                        case "decompile":
                        {
                            string objectPath = cmdArgs.Length > 2 ? cmdArgs[2] : "unspecified";
                            var obj = pkg.FindObjectByGroup(objectPath);
                            if (obj == null)
                            {
                                Console.Error.WriteLine("Couldn't find object by path '{0}'", objectPath);
                                break;
                            }

                            obj.BeginDeserializing();
                            string output = obj.Decompile();
                            Console.WriteLine(output);
                            break;
                        }

                        default:
                            Console.Error.WriteLine("Unrecognized action '{0}'", actionName);
                            break;
                    }

                    break;

                default:
                    Console.Error.WriteLine("Unrecognized command '{0}'", commandName);
                    break;
            }

            string nextInput = Console.ReadLine();
            if (nextInput != null)
            {
                cmdArgs = nextInput.Split(' ');
                goto performCommand;
            }
        }
    }
}