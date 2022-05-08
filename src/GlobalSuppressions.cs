// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the
// Error List, point to "Suppress Message(s)", and click
// "In Project Suppression File".
// You do not need to add suppressions to this file manually.

[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors", Scope = "member",
        Target = "UELib.UObjectStream.#.ctor(UELib.UPackageStream,System.Byte[]&)")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
        "CA2214:DoNotCallOverridableMethodsInConstructors", Scope = "member",
        Target = "UELib.UPackageStream.#.ctor(System.String,System.IO.FileMode,System.IO.FileAccess)")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly",
        Scope = "member", Target = "UELib.UnrealExtensions.#TextureExt")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags",
        Scope = "member", Target = "UELib.UnrealMethods.#FlagsToList(System.Type,System.UInt32)")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "ZLX", Scope = "member", Target = "UELib.Flags.CompressionFlags.#ZLX")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "ZLO", Scope = "member", Target = "UELib.Flags.CompressionFlags.#ZLO")]
[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly",
        MessageId = "ZLIB", Scope = "member", Target = "UELib.Flags.CompressionFlags.#ZLIB")]