namespace Brontide.Minimal.Binding.Portable

open System

/// A canonical name inside the portable profile.
///
/// The portable profile narrows the Brontide canonical-name rule to ASCII letters, digits,
/// underscore, and hyphen. A Unicode-letter name is a valid Brontide canonical name but is not
/// portable in binding 0.1, so the narrowing is enforced at construction rather than at a codec.
[<StructuralEquality; StructuralComparison>]
type PortableName = private PortableName of string

[<RequireQualifiedAccess>]
module PortableName =
    [<Literal>]
    let MaxBytes = 256

    let private isSegmentCharacter (character: char) =
        (character >= 'A' && character <= 'Z')
        || (character >= 'a' && character <= 'z')
        || (character >= '0' && character <= '9')
        || character = '_'
        || character = '-'

    let private isPath (path: string) =
        path.Length > 0
        && path.Split('.')
           |> Array.forall (fun segment -> segment.Length > 0 && Seq.forall isSegmentCharacter segment)

    let private isWellFormed (value: string) =
        if String.IsNullOrEmpty value || value.Length > MaxBytes then
            false
        else
            match value.Split(':') with
            | [| single |] -> isPath single
            | [| authority; concept |] -> isPath authority && isPath concept
            | _ -> false

    let tryCreate (value: string) : PortableResult<PortableName> =
        if isWellFormed value then
            Ok(PortableName value)
        else
            malformed "name-profile" $"'{value}' is outside the portable canonical-name profile."

    let value (PortableName value) = value

/// A record field name, a choice alternative name, or a lifecycle feature name.
[<StructuralEquality; StructuralComparison>]
type PortableMemberToken = private PortableMemberToken of string

[<RequireQualifiedAccess>]
module PortableMemberToken =
    let private isTokenCharacter (character: char) =
        (character >= 'A' && character <= 'Z')
        || (character >= 'a' && character <= 'z')
        || (character >= '0' && character <= '9')
        || character = '_'
        || character = '-'

    let tryCreate (value: string) : PortableResult<PortableMemberToken> =
        if String.IsNullOrEmpty value
           || value.Length > PortableName.MaxBytes
           || not (Seq.forall isTokenCharacter value) then
            malformed "member-token" $"'{value}' is outside the portable member-token profile."
        else
            Ok(PortableMemberToken value)

    let value (PortableMemberToken value) = value

/// The structured canonical form every identity space shares: a portable name and a version of at
/// least one. It is never an identity by itself; each identity space wraps it in its own type.
[<StructuralEquality; StructuralComparison>]
type PortableCanonical =
    private
        { Name: PortableName
          Version: int }

    override this.ToString() = $"{PortableName.value this.Name}@{this.Version}"

[<RequireQualifiedAccess>]
module PortableCanonical =
    let tryCreate (name: string) (version: int) : PortableResult<PortableCanonical> =
        if version < 1 then
            malformed "reference-version" "A canonical reference version must be at least 1."
        else
            PortableName.tryCreate name
            |> Result.map (fun parsed -> { Name = parsed; Version = version })

    let name canonical = PortableName.value canonical.Name
    let version canonical = canonical.Version
    let text (canonical: PortableCanonical) = canonical.ToString()

    /// Parses the text rendering, splitting on the last '@'. The rendering is never a distinct
    /// identity, so parsing it produces the same structured form the wire would have carried.
    let tryParseText (value: string) : PortableResult<PortableCanonical> =
        let separator = value.LastIndexOf '@'

        if separator <= 0 then
            malformed "reference-text" $"'{value}' is not a canonical '<name>@<version>' reference."
        else
            match Int32.TryParse(value.Substring(separator + 1)) with
            | true, parsedVersion -> tryCreate (value.Substring(0, separator)) parsedVersion
            | _ -> malformed "reference-text" $"'{value}' carries no reference version."

/// The Component whose contract is established.
[<StructuralEquality; StructuralComparison>]
type PortableComponentRef =
    private
    | PortableComponentRef of PortableCanonical

    override this.ToString() =
        let (PortableComponentRef canonical) = this in canonical.ToString()

/// The endpoint offering the Component.
[<StructuralEquality; StructuralComparison>]
type PortableProviderRef =
    private
    | PortableProviderRef of PortableCanonical

    override this.ToString() =
        let (PortableProviderRef canonical) = this in canonical.ToString()

/// A negotiated Operation.
[<StructuralEquality; StructuralComparison>]
type PortableOperationRef =
    private
    | PortableOperationRef of PortableCanonical

    override this.ToString() =
        let (PortableOperationRef canonical) = this in canonical.ToString()

/// An input, result, or detail Shape.
[<StructuralEquality; StructuralComparison>]
type PortableShapeRef =
    private
    | PortableShapeRef of PortableCanonical

    override this.ToString() =
        let (PortableShapeRef canonical) = this in canonical.ToString()

/// A declared Fragment.
[<StructuralEquality; StructuralComparison>]
type PortableFragmentRef =
    private
    | PortableFragmentRef of PortableCanonical

    override this.ToString() =
        let (PortableFragmentRef canonical) = this in canonical.ToString()

/// A declared profile, binding, feature, or resource-flavor dependency.
[<StructuralEquality; StructuralComparison>]
type PortableDependencyRef =
    private
    | PortableDependencyRef of PortableCanonical

    override this.ToString() =
        let (PortableDependencyRef canonical) = this in canonical.ToString()

[<RequireQualifiedAccess>]
module PortableComponentRef =
    let ofCanonical canonical = PortableComponentRef canonical
    let tryCreate name version = PortableCanonical.tryCreate name version |> Result.map ofCanonical
    let canonical (PortableComponentRef canonical) = canonical
    let text reference = (canonical reference).ToString()

[<RequireQualifiedAccess>]
module PortableProviderRef =
    let ofCanonical canonical = PortableProviderRef canonical
    let tryCreate name version = PortableCanonical.tryCreate name version |> Result.map ofCanonical
    let canonical (PortableProviderRef canonical) = canonical
    let text reference = (canonical reference).ToString()

[<RequireQualifiedAccess>]
module PortableOperationRef =
    let ofCanonical canonical = PortableOperationRef canonical
    let tryCreate name version = PortableCanonical.tryCreate name version |> Result.map ofCanonical
    let canonical (PortableOperationRef canonical) = canonical
    let text reference = (canonical reference).ToString()

[<RequireQualifiedAccess>]
module PortableShapeRef =
    let ofCanonical canonical = PortableShapeRef canonical
    let tryCreate name version = PortableCanonical.tryCreate name version |> Result.map ofCanonical
    let canonical (PortableShapeRef canonical) = canonical
    let name reference = PortableCanonical.name (canonical reference)
    let version reference = PortableCanonical.version (canonical reference)
    let text reference = (canonical reference).ToString()

[<RequireQualifiedAccess>]
module PortableFragmentRef =
    let ofCanonical canonical = PortableFragmentRef canonical
    let tryCreate name version = PortableCanonical.tryCreate name version |> Result.map ofCanonical
    let canonical (PortableFragmentRef canonical) = canonical
    let name reference = PortableCanonical.name (canonical reference)
    let version reference = PortableCanonical.version (canonical reference)

    /// The rendering used where a single string key is structurally required, which is the fragment
    /// map of a record value.
    let text reference = (canonical reference).ToString()

    let tryParseText value =
        PortableCanonical.tryParseText value |> Result.map ofCanonical

[<RequireQualifiedAccess>]
module PortableDependencyRef =
    let ofCanonical canonical = PortableDependencyRef canonical
    let tryCreate name version = PortableCanonical.tryCreate name version |> Result.map ofCanonical
    let canonical (PortableDependencyRef canonical) = canonical
    let text reference = (canonical reference).ToString()

/// Identifies one binding scope. It is opaque and is never persisted as identity.
[<StructuralEquality; StructuralComparison>]
type PlanId = private PlanId of string

/// Channel correlation for one established binding.
[<StructuralEquality; StructuralComparison>]
type ChannelId = private ChannelId of string

/// Channel correlation for one request within one binding; also the replay identity.
[<StructuralEquality; StructuralComparison>]
type ChannelRequestId = private ChannelRequestId of string

/// Optional Channel correlation, carried only when the peer echoes it.
[<StructuralEquality; StructuralComparison>]
type ChannelExecutionId = private ChannelExecutionId of string

/// The host's own Execution identity. The Channel identity rule requires it to stay distinct from
/// every Channel correlation identity, so it is a separate type rather than a reused string.
[<StructuralEquality; StructuralComparison>]
type HostExecutionId = private HostExecutionId of string

[<RequireQualifiedAccess>]
module PlanId =
    let next () = PlanId $"plan-{Guid.NewGuid():n}"
    let value (PlanId value) = value

[<RequireQualifiedAccess>]
module ChannelId =
    let next () = ChannelId $"ch-{Guid.NewGuid():n}"

    /// Adopts an identity a peer carried. It stays opaque: nothing here reads structure into it.
    let received (value: string) = ChannelId value

    let value (ChannelId value) = value

[<RequireQualifiedAccess>]
module ChannelRequestId =
    let next () = ChannelRequestId $"rq-{Guid.NewGuid():n}"
    let received (value: string) = ChannelRequestId value
    let value (ChannelRequestId value) = value

[<RequireQualifiedAccess>]
module ChannelExecutionId =
    let next () = ChannelExecutionId $"ex-{Guid.NewGuid():n}"
    let received (value: string) = ChannelExecutionId value
    let value (ChannelExecutionId value) = value

[<RequireQualifiedAccess>]
module HostExecutionId =
    let next () = HostExecutionId $"host-exec-{Guid.NewGuid():n}"
    let value (HostExecutionId value) = value

[<RequireQualifiedAccess>]
type IdentitySpace =
    | Operation
    | Shape
    | Fragment

[<RequireQualifiedAccess>]
module IdentitySpace =
    let token space =
        match space with
        | IdentitySpace.Operation -> "operation"
        | IdentitySpace.Shape -> "shape"
        | IdentitySpace.Fragment -> "fragment"

    let tryParse value =
        match value with
        | "operation" -> Ok IdentitySpace.Operation
        | "shape" -> Ok IdentitySpace.Shape
        | "fragment" -> Ok IdentitySpace.Fragment
        | other -> malformed "compact-space" $"'{other}' is not a compact-identifier space."

/// A binding-scoped compact identifier. It is assigned only after canonical negotiation succeeds,
/// is meaningless outside its binding, and never appears in a plan fact or observation as identity.
[<StructuralEquality; StructuralComparison>]
type CompactId = private CompactId of int

[<RequireQualifiedAccess>]
module CompactId =
    [<Literal>]
    let MaxValue = 65535

    let tryCreate value : PortableResult<CompactId> =
        if value < 0 || value > MaxValue then
            unsupportedContract "compact-identifier-range" "A compact identifier occupies the unsigned range 0..65535."
        else
            Ok(CompactId value)

    let value (CompactId value) = value

/// One compact-identifier assignment, recorded as plan data scoped to one binding.
type CompactAssignment =
    { Space: IdentitySpace
      Reference: string
      Compact: CompactId }
