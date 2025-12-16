using System.Text;
using UELib;
using UELib.Core;
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
        private UnrealPackage? _TestPackage;
        private UnrealPackage TestPackage => _TestPackage!;

        [TestInitialize]
        public void CreateTestPackage()
        {
            _TestPackage = new UnrealPackage("TestT3D");
            _TestPackage.Environment.AddUnrealClasses(); // Register all UELib classes.

            var linker = _TestPackage.Linker;
            var polys = linker.CreateObject<UPolys>("TestPolys");
            polys.Element =
            [
                new Poly
                {
                    Vertex =
                    [
                        new UVector { X = 0, Y = 0, Z = 0 },
                        new UVector { X = 100, Y = 0, Z = 0 },
                        new UVector { X = 100, Y = 100, Z = 0 }
                    ],
                    TextureU = new UVector(1, 0, 0),
                    TextureV = new UVector(0, 1, 0),
                    PanU = 0,
                    PanV = 0,
                },
                new Poly
                {
                    Vertex =
                    [
                        new UVector { X = 0, Y = 0, Z = 0 },
                        new UVector { X = 200, Y = 0, Z = 0 },
                        new UVector { X = 200, Y = 200, Z = 0 }
                    ],
                    TextureU = new UVector(0, 1, 0),
                    TextureV = new UVector(1, 0, 0),
                    PanU = 0,
                    PanV = 0,
                },
            ];

            _TestPackage.InitializePackage(null);
        }

        [TestCleanup]
        public void DestroyTestPackage()
        {
            _TestPackage?.Dispose();
        }

        [TestMethod]
        public void DecompileObjectToT3DTest()
        {
            var output = new StringWriter(new StringBuilder());
            var outputStream = new TextOutputStream(output);
            var decompiler = new T3DOutputDecompiler(outputStream);

            var polyObject = TestPackage.Environment.FindObject<UPolys>("TestPolys");
            decompiler.Decompile(polyObject, CancellationToken.None);

            string text = output.ToString();
            Assert.AreEqual(
            """
                Begin PolyList
                    Begin Polygon Item=None Flags=3584 Link=-1 ShadowMapScale=32.000000
                        U=0
                        V=0
                    End Polygon
                    Begin Polygon Item=None Flags=3584 Link=-1 ShadowMapScale=32.000000
                        U=0
                        V=0
                    End Polygon
                End PolyList
                """,
                text
            );
        }

        [TestMethod]
        public void ObjectToT3DTreeTest()
        {
            var outputStream = new TextOutputStream(Console.Out);
            var treeBuilder = new ArchetypeNodeTreeBuilder();

            var polysObjects = TestPackage.EnumerateObjects<UPolys>();
            foreach (var obj in polysObjects)
            {
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
