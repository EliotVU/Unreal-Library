using System.Diagnostics.Contracts;
using UELib.ObjectModel.Annotations;
using UELib.Services;

namespace UELib.Core;

[UnrealRegisterClass(InternalClassFlags.Preload)]
public sealed class UObjectRedirector : UObject
{
    /// <summary>
    ///     The other object that this object should redirect to.
    /// </summary>
    public UObject Other { get; set; }

    public override void Deserialize(IUnrealStream stream)
    {
        base.Deserialize(stream);

        Other = stream.ReadObject();
        stream.Record(nameof(Other), Other);

        // Debug because we want to fail safely here
        LibServices.LogService.SilentAssert(Other != null, "'Other' should never be null when deserialized.");
    }

    public override void Serialize(IUnrealStream stream)
    {
        base.Serialize(stream);

        Contract.Assert(Other != null, "Should not serialize an object redirector if the 'other' is null.");
        stream.WriteObject(Other);
    }
}
