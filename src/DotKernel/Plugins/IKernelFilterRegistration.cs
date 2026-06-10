namespace DotKernel;

/// <summary>Implemented by source-generated filter partial classes.</summary>
public interface IKernelFilterRegistration
{
    static abstract void Register(IKernelBuilder builder, IKernelFilter instance);
}
