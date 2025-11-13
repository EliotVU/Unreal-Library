using Eliot.UELib.Test.Builds;
using UELib.Decompiler;
using UELib.Decompiler.Common;
using UELib.Decompiler.Nodes;
using UELib.Decompiler.T3D;
using UELib.Engine;

namespace Eliot.UELib.Decompiler.T3D.Test
{
    [TestClass]
    public sealed class T3DOutputDecompilerTests
    {
        // We only test for exceptions here, no output is being tested.
        [TestMethod]
        public void DecompileObjectToT3DTest()
        {
            using var linker = PackageTestsUT2004.GetMapPackage("DM-Rankin.ut2");
            linker.InitializePackage();

            var polysObjects = linker.Objects.OfType<UPolys>();

            var outputStream = new TextOutputStream(Console.Out);
            var decompiler = new T3DOutputDecompiler(outputStream);

            foreach (var obj in polysObjects)
            {
                obj.Load();

                decompiler.Decompile(obj, CancellationToken.None);
                outputStream.WriteLine();
            }
        }

        [TestMethod]
        public void ObjectTo3DTreeTest()
        {
            using var linker = PackageTestsUT2004.GetMapPackage("DM-Rankin.ut2");
            linker.InitializePackage();

            var polysObjects = linker.Objects.OfType<UPolys>();

            var outputStream = new TextOutputStream(Console.Out);
            var treeBuilder = new ArchetypeNodeTreeBuilder();

            foreach (var obj in polysObjects)
            {
                obj.Load();

                var node = obj.Accept(treeBuilder);

                RecurseNodes(node);
            }

            return;

            void RecurseNodes(Node node)
            {
                outputStream.Write(node);
                outputStream.WriteLine();

                outputStream.WriteIndented(() =>
                {
                    foreach (var child in node.EnumerateChildren())
                    {
                        if (child.Child != null)
                        {
                            RecurseNodes(child);
                        }
                        else
                        {
                            outputStream.Write(child);
                            outputStream.WriteLine();
                        }
                    }
                });

                outputStream.WriteLine();
            }
        }
    }
}
