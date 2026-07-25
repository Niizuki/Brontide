namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>
/// The declared delivery and hardening bounds for one binding scope.
/// </summary>
/// <remarks>
/// Every bound is declared in the contract and frozen into the Binding Plan: an undeclared limit is
/// not enforceable and an unbounded limit is not permitted, so the type has no "no limit" state. The
/// defaults are the neutral contract's values, which take the tighter of the two experiments
/// wherever Cooling and Catalog disagreed.
/// </remarks>
public sealed record PortableLimits(
    int MaxFrameBytes,
    int MaxNestingDepth,
    int MaxRecordFields,
    int MaxFragmentsPerRecord,
    int MaxSequenceItems,
    int MaxTextBytes,
    int MaxByteStringBytes,
    int MaxResourceBytes,
    int IoTimeoutMilliseconds,
    int MaxConcurrentRequests)
{
    public static PortableLimits Declared { get; } = new(
        MaxFrameBytes: 65_536,
        MaxNestingDepth: 32,
        MaxRecordFields: 256,
        MaxFragmentsPerRecord: 16,
        MaxSequenceItems: 4_096,
        MaxTextBytes: 16_384,
        MaxByteStringBytes: 32_768,
        MaxResourceBytes: 32_768,
        IoTimeoutMilliseconds: 10_000,
        MaxConcurrentRequests: 1);

    public TimeSpan IoTimeout => TimeSpan.FromMilliseconds(IoTimeoutMilliseconds);

    /// <summary>Rejects a declaration that is internally inconsistent or unbounded.</summary>
    public void Validate()
    {
        var positive = MaxFrameBytes > 0 && MaxNestingDepth > 0 && MaxRecordFields > 0 &&
            MaxFragmentsPerRecord > 0 && MaxSequenceItems > 0 && MaxTextBytes > 0 &&
            MaxByteStringBytes > 0 && MaxResourceBytes > 0 && IoTimeoutMilliseconds > 0;
        if (!positive)
        {
            throw PortableFaultException.Malformed("limit-unbounded", "Every declared limit must be positive.");
        }

        if (MaxConcurrentRequests != 1)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "concurrency-unsupported",
                "Version 0.1 supports exactly one concurrent request per binding.");
        }

        // Every value travels inside one frame, so no value bound may exceed the frame bound.
        if (MaxResourceBytes > MaxByteStringBytes ||
            MaxTextBytes > MaxFrameBytes ||
            MaxByteStringBytes > MaxFrameBytes)
        {
            throw PortableFaultException.Malformed(
                "limit-inconsistent",
                "A declared value bound exceeds the frame bound it must fit inside.");
        }
    }
}
