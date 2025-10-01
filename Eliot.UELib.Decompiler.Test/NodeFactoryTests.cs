using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib.Core;
using UELib.Decompiler.Common;
using UELib.Decompiler.Common.Nodes;
using UELib.Decompiler.Nodes;
using UELib.ObjectModel.Annotations;

namespace Eliot.UELib.Decompiler.Test
{
    [TestClass]
    public class NodeFactoryTests
    {
        [TestMethod]
        public void TestArchetypeNodeFactory()
        {
            // Static sanity check
            var members = typeof(TestExportClass).FindMembers(
                MemberTypes.Field | MemberTypes.Property,
                BindingFlags.Public | BindingFlags.Instance,
                (info, criteria) => true, null);
            Assert.AreEqual(2, members.Length, members.ToString());
            Assert.AreEqual(2, members
                .Count(m => m.GetCustomAttribute<OutputAttribute>() != null));

            var obj = new TestExportClass { StringParameter = "String", StringProperty = "String" };
            var nodes = ArchetypeNodeFactory
                .Create(obj)
                .ToList();
            Assert.AreEqual(2, nodes.Count, nodes.Count.ToString());
        }

        [TestMethod]
        public void TestNodeFactory()
        {
            var vertex = new[] { new UVector(), new UVector() };
            var node = NodeFactory.Create(vertex);
            Assert.IsTrue(node is ArrayLiteralNode);
            Assert.AreEqual(2, ((ArrayLiteralNode)node).Value.Count(n => n is StructLiteralNode<UVector>));
        }

        private class TestExportClass
        {
            [Output(OutputSlot.Parameter)]
            public required string StringParameter;

            [Output(OutputSlot.Property)]
            public required string StringProperty;
        }
    }
}
