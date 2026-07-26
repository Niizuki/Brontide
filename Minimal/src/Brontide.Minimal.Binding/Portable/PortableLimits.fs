namespace Brontide.Minimal.Binding.Portable

open System

/// The declared delivery and hardening bounds for one binding scope.
///
/// Every bound is declared in the contract and frozen into the Binding Plan: an undeclared limit is
/// not enforceable and an unbounded limit is not permitted, so the record has no "no limit" state.
/// The declared values take the tighter of the two baseline experiments wherever they disagreed.
[<StructuralEquality; NoComparison>]
type PortableLimits =
    { MaxFrameBytes: int
      MaxNestingDepth: int
      MaxRecordFields: int
      MaxFragmentsPerRecord: int
      MaxSequenceItems: int
      MaxTextBytes: int
      MaxByteStringBytes: int
      MaxResourceBytes: int
      IoTimeoutMilliseconds: int
      MaxConcurrentRequests: int }

[<RequireQualifiedAccess>]
module PortableLimits =

    let declared =
        { MaxFrameBytes = 65536
          MaxNestingDepth = 32
          MaxRecordFields = 256
          MaxFragmentsPerRecord = 16
          MaxSequenceItems = 4096
          MaxTextBytes = 16384
          MaxByteStringBytes = 32768
          MaxResourceBytes = 32768
          IoTimeoutMilliseconds = 10000
          MaxConcurrentRequests = 1 }

    let ioTimeout limits =
        TimeSpan.FromMilliseconds(float limits.IoTimeoutMilliseconds)

    /// Refuses a declaration that is internally inconsistent or effectively unbounded.
    let validate limits : PortableResult<unit> =
        let positive =
            [ limits.MaxFrameBytes
              limits.MaxNestingDepth
              limits.MaxRecordFields
              limits.MaxFragmentsPerRecord
              limits.MaxSequenceItems
              limits.MaxTextBytes
              limits.MaxByteStringBytes
              limits.MaxResourceBytes
              limits.IoTimeoutMilliseconds ]
            |> List.forall (fun bound -> bound > 0)

        portable {
            do! ensure positive (fun () -> malformed "limit-unbounded" "Every declared limit must be positive.")

            do!
                ensure (limits.MaxConcurrentRequests = 1) (fun () ->
                    unsupportedContract
                        "concurrency-unsupported"
                        "Version 0.1 supports exactly one concurrent request per binding.")

            // Every value travels inside one frame, so no value bound may exceed the frame bound.
            do!
                ensure
                    (limits.MaxResourceBytes <= limits.MaxByteStringBytes
                     && limits.MaxTextBytes <= limits.MaxFrameBytes
                     && limits.MaxByteStringBytes <= limits.MaxFrameBytes)
                    (fun () ->
                        malformed "limit-inconsistent" "A declared value bound exceeds the frame bound it must fit inside.")
        }
