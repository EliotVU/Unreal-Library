using System.Threading;

namespace UELib.Decompiler;

public interface IOutputDecompiler<in TAcceptable>
    where TAcceptable : IAcceptable
{
    bool CanDecompile(TAcceptable? visitable);
    void Decompile(TAcceptable visitable, CancellationToken cancellationToken);
}
