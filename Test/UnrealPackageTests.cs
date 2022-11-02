using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UELib;
using UELib.Core;

namespace Eliot.UELib.Test
{
    [TestClass]
    public class UnrealPackageTests
    {
        internal static void AssertTestClass(UnrealPackage linker)
        {
            var testClass = linker.FindObject<UClass>("Test");
            Assert.IsNotNull(testClass);

            // Validate that Public/Protected/Private are correct and distinguishable.
            var publicProperty = linker.FindObject<UIntProperty>("Public");
            Assert.IsNotNull(publicProperty);
            Assert.IsTrue(publicProperty.IsPublic());
            Assert.IsFalse(publicProperty.IsProtected());
            Assert.IsFalse(publicProperty.IsPrivate());

            var protectedProperty = linker.FindObject<UIntProperty>("Protected");
            Assert.IsNotNull(protectedProperty);
            Assert.IsTrue(protectedProperty.IsPublic());
            Assert.IsTrue(protectedProperty.IsProtected());
            Assert.IsFalse(protectedProperty.IsPrivate());

            var privateProperty = linker.FindObject<UIntProperty>("Private");
            Assert.IsNotNull(privateProperty);
            Assert.IsFalse(privateProperty.IsPublic());
            Assert.IsFalse(privateProperty.IsProtected());
            Assert.IsTrue(privateProperty.IsPrivate());
        }

        internal static UObject AssertDefaultPropertiesClass(UnrealPackage linker)
        {
            var testClass = linker.FindObject<UClass>("DefaultProperties");
            Assert.IsNotNull(testClass);

            var defaults = testClass.Default ?? testClass;
            defaults.BeginDeserializing();
            Assert.IsNotNull(defaults.Properties);

            return defaults;
        }

        internal static void AssertPropertyTagFormat(UObject obj, string tagName, string expectedFormat)
        {
            Assert.IsNotNull(obj.Properties);
            var tag = obj.Properties.Find(tagName);
            Assert.IsNotNull(tag, $"Couldn't find property tag of '{tagName}'");
            string colorValue = tag.DeserializeValue();
            Assert.AreEqual(expectedFormat, colorValue, $"tag '{tagName}'");
        }

        internal static void AssertExportsOfType<T>(IEnumerable<UObject> exports)
            where T : UObject
        {
            var textures = exports.OfType<T>()
                .ToList();
            Assert.IsTrue(textures.Any());
            textures.ForEach(AssertObjectDeserialization);
        }

        internal static void AssertObjectDeserialization(UObject obj)
        {
            obj.BeginDeserializing();
            Assert.IsTrue(obj.DeserializationState == UObject.ObjectState.Deserialied, obj.GetReferencePath());
        }
    }
}