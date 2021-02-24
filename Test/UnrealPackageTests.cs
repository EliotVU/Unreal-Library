// using System.IO;
// using System.Reflection;
// using Microsoft.VisualStudio.TestTools.UnitTesting;
// using UELib.Core;
//
// namespace UELib.Test
// {
//     [TestClass]
//     public class UnrealPackageTests
//     {
//         [TestMethod]
//         public void LoadPackageTest()
//         {
//             string testPackagePath = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "upk", "TestUC2", "TestUC2.u");
//             var package = UnrealLoader.LoadPackage(testPackagePath);
//             Assert.IsNotNull(package);
//
//             // FIXME: UELib is dependent on WinForms
//             //package.InitializePackage();
//
//             //var testClass = package.FindObject("Test", typeof(UClass));
//             //Assert.IsNotNull(testClass);
//
//             //string testClassContent = testClass.Decompile();
//             //Assert.AreNotSame("", testClassContent);
//         }
//     }
// }
