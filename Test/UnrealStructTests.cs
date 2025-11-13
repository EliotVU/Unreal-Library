using System.Numerics;
using System.Runtime.CompilerServices;
using UELib.Core;

namespace Eliot.UELib.Test
{
    [TestClass]
    public class UnrealStructTests
    {
        [TestMethod]
        public void TestUnrealStructs()
        {
            // Validate non padded sizes.
            Assert.AreEqual(25, Unsafe.SizeOf<UBox>());
            Assert.AreEqual(17, Unsafe.SizeOf<UScale>());
            Assert.AreEqual(64, Unsafe.SizeOf<UMatrix>());

            // Validate casting
            var identity = (UMatrix)Matrix4x4.Identity;
            var intrinsicMatrix = (Matrix4x4)identity;
            Assert.AreEqual(Matrix4x4.Identity, intrinsicMatrix);
        }
    }
}
