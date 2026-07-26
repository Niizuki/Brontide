namespace Brontide.Minimal.Binding.Portable

[<RequireQualifiedAccess>]
type DependencyKind =
    | Operation
    | Profile
    | Binding
    | ResourceFlavor
    | Feature

[<RequireQualifiedAccess>]
type RequirementStrength =
    | Required
    | Preferred
    | Optional
    | Opposed

[<RequireQualifiedAccess>]
type FragmentPolicy =
    | Open
    | Closed

[<RequireQualifiedAccess>]
type PortableScalar =
    | Text
    | Boolean
    | Signed64
    | Decimal
    | Bytes

[<RequireQualifiedAccess>]
type AuthorityMode =
    | LocalDomainEvaluated
    | CrossTrustNoCapabilityTransfer

[<RequireQualifiedAccess>]
type Realization =
    | FixedDirectCall
    | NegotiatedProcess

[<RequireQualifiedAccess>]
module DependencyKind =
    let token kind =
        match kind with
        | DependencyKind.Operation -> "operation"
        | DependencyKind.Profile -> "profile"
        | DependencyKind.Binding -> "binding"
        | DependencyKind.ResourceFlavor -> "resource-flavor"
        | DependencyKind.Feature -> "feature"

    let tryParse value : PortableResult<DependencyKind> =
        match value with
        | "operation" -> Ok DependencyKind.Operation
        | "profile" -> Ok DependencyKind.Profile
        | "binding" -> Ok DependencyKind.Binding
        | "resource-flavor" -> Ok DependencyKind.ResourceFlavor
        | "feature" -> Ok DependencyKind.Feature
        | other -> malformed "dependency-kind-enumeration" $"'{other}' is outside the declared dependency-kind enumeration."

[<RequireQualifiedAccess>]
module RequirementStrength =
    let token strength =
        match strength with
        | RequirementStrength.Required -> "required"
        | RequirementStrength.Preferred -> "preferred"
        | RequirementStrength.Optional -> "optional"
        | RequirementStrength.Opposed -> "opposed"

    /// An unknown strength is refused rather than defaulted to a permissive member.
    let tryParse value : PortableResult<RequirementStrength> =
        match value with
        | "required" -> Ok RequirementStrength.Required
        | "preferred" -> Ok RequirementStrength.Preferred
        | "optional" -> Ok RequirementStrength.Optional
        | "opposed" -> Ok RequirementStrength.Opposed
        | other -> malformed "requirement-strength-enumeration" $"'{other}' is outside the declared strength enumeration."

[<RequireQualifiedAccess>]
module FragmentPolicy =
    let token policy =
        match policy with
        | FragmentPolicy.Open -> "open"
        | FragmentPolicy.Closed -> "closed"

    let tryParse value : PortableResult<FragmentPolicy> =
        match value with
        | "open" -> Ok FragmentPolicy.Open
        | "closed" -> Ok FragmentPolicy.Closed
        | other -> malformed "fragment-policy-enumeration" $"'{other}' is outside the declared fragment-policy enumeration."

[<RequireQualifiedAccess>]
module PortableScalar =
    let token scalar =
        match scalar with
        | PortableScalar.Text -> "Text"
        | PortableScalar.Boolean -> "Boolean"
        | PortableScalar.Signed64 -> "Integer.Signed64"
        | PortableScalar.Decimal -> "Decimal"
        | PortableScalar.Bytes -> "Bytes"

    let tryParse value : PortableResult<PortableScalar> =
        match value with
        | "Text" -> Ok PortableScalar.Text
        | "Boolean" -> Ok PortableScalar.Boolean
        | "Integer.Signed64" -> Ok PortableScalar.Signed64
        | "Decimal" -> Ok PortableScalar.Decimal
        | "Bytes" -> Ok PortableScalar.Bytes
        | other -> malformed "scalar-kind-enumeration" $"'{other}' is outside the declared scalar enumeration."

[<RequireQualifiedAccess>]
module AuthorityMode =
    let token mode =
        match mode with
        | AuthorityMode.LocalDomainEvaluated -> "local-domain-evaluated"
        | AuthorityMode.CrossTrustNoCapabilityTransfer -> "cross-trust-no-capability-transfer"

    let tryParse value : PortableResult<AuthorityMode> =
        match value with
        | "local-domain-evaluated" -> Ok AuthorityMode.LocalDomainEvaluated
        | "cross-trust-no-capability-transfer" -> Ok AuthorityMode.CrossTrustNoCapabilityTransfer
        | other -> malformed "presentation-mode-enumeration" $"'{other}' is outside the declared presentation-mode enumeration."

[<RequireQualifiedAccess>]
module Realization =
    let token realization =
        match realization with
        | Realization.FixedDirectCall -> "fixed-direct-call"
        | Realization.NegotiatedProcess -> "negotiated-process"

/// The declared payload representations and framings.
[<RequireQualifiedAccess>]
module PortableRepresentations =
    /// The portable wire for the negotiated process realization.
    [<Literal>]
    let PortableCborCore = "portable-cbor-core-0.1"

    /// The retained baseline representation. It is diagnostic and legacy only and is not the
    /// portable wire contract, so a portable binding refuses it rather than negotiating it.
    [<Literal>]
    let InlineTaggedJson = "inline-tagged-json"

    [<Literal>]
    let LengthDelimited = "length-delimited"

    [<Literal>]
    let DirectCall = "direct-call"

type Provision =
    { Kind: DependencyKind
      Reference: PortableDependencyRef
      ProviderSpecific: bool }

type Requirement =
    { Kind: DependencyKind
      Reference: PortableDependencyRef
      Strength: RequirementStrength
      ProviderSpecific: bool }

type FieldDeclaration =
    { Name: string
      Shape: PortableShapeRef
      Required: bool }

type AlternativeDeclaration = { Name: string; Shape: PortableShapeRef }

/// The Shape's structure.
///
/// Modelling the body as a union rather than as a bag of members that are required for one kind and
/// forbidden for the rest makes the "forbidden otherwise" rule structural: a sequence Shape has no
/// place to put a field list.
type ShapeBody =
    | RecordBody of policy: FragmentPolicy * fields: FieldDeclaration list
    | SequenceBody of item: PortableShapeRef
    | ChoiceBody of alternatives: AlternativeDeclaration list
    | ScalarBody of PortableScalar
    | UnitBody

type ShapeDeclaration =
    { Reference: PortableShapeRef
      Body: ShapeBody }

type FragmentDeclaration =
    { Reference: PortableFragmentRef
      HostShape: PortableShapeRef
      Fields: FieldDeclaration list }

type OperationDeclaration =
    { Reference: PortableOperationRef
      InputShape: PortableShapeRef
      ResultShape: PortableShapeRef
      /// The Shape of a shaped failed Outcome. It is required because a semantic failure must be
      /// shaped rather than exception-borne.
      DetailShape: PortableShapeRef
      RequiredFragments: PortableFragmentRef list
      ResourceFlavors: string list }

type AuthorityDeclaration =
    { PresentationMode: AuthorityMode
      TrustBoundaryCrossed: bool
      NoCapabilityTransfer: bool
      ConstraintPolicy: string }

type RepresentationDeclaration =
    { Representation: string
      Framing: string
      ResourceFlavors: string list
      AcceptedResourceHandles: string list }

type LifecycleDeclaration =
    { ReplayProtectionDeclared: bool
      ReplayWindow: string
      Features: Map<string, bool> }

/// One versioned Component contract, established before any provider effect.
///
/// The document replaces the two divergent baseline manifest encodings: it names the detail Shape
/// explicitly rather than reaching it only through a failure path, and it uses one structured
/// canonical reference rather than mixing structured and text forms.
type ContractDocument =
    { ContractVersion: int
      Component: PortableComponentRef
      Provider: PortableProviderRef
      Provisions: Provision list
      Requirements: Requirement list
      Operations: OperationDeclaration list
      Shapes: ShapeDeclaration list
      Fragments: FragmentDeclaration list
      Authority: AuthorityDeclaration
      Representation: RepresentationDeclaration
      Limits: PortableLimits
      Lifecycle: LifecycleDeclaration }

[<RequireQualifiedAccess>]
module ContractDocument =
    [<Literal>]
    let SupportedContractVersion = 1

    [<Literal>]
    let OnlyPermittedConstraintPolicy = "fail-closed"

    let tryOperation reference document =
        document.Operations |> List.tryFind (fun operation -> operation.Reference = reference)

/// Strict codec for the contract document; unknown fields and enumeration values fail closed.
[<RequireQualifiedAccess>]
module ContractCodec =

    let private documentFields =
        [ "contractVersion"
          "component"
          "provider"
          "provisions"
          "requirements"
          "operations"
          "shapes"
          "fragments"
          "authority"
          "representation"
          "limits"
          "lifecycle" ]

    let private limitFields =
        [ "maxFrameBytes"
          "maxNestingDepth"
          "maxRecordFields"
          "maxFragmentsPerRecord"
          "maxSequenceItems"
          "maxTextBytes"
          "maxByteStringBytes"
          "maxResourceBytes"
          "ioTimeoutMilliseconds"
          "maxConcurrentRequests" ]

    let private shapeReference reference =
        CborAccess.encodeCanonical (PortableShapeRef.canonical reference)

    let private fragmentReference reference =
        CborAccess.encodeCanonical (PortableFragmentRef.canonical reference)

    let private readShapeRef item what =
        CborAccess.readCanonical item what |> Result.map PortableShapeRef.ofCanonical

    let private readFragmentRef item what =
        CborAccess.readCanonical item what |> Result.map PortableFragmentRef.ofCanonical

    // -- fields and alternatives -------------------------------------------

    let private encodeField (field: FieldDeclaration) =
        CborMap
            [ "name", CborText field.Name
              "shape", shapeReference field.Shape
              "required", CborBoolean field.Required ]

    let private decodeField item =
        portable {
            let! entries = CborAccess.requireMap item "field"
            do! CborAccess.requireDeclaredFields entries "field" [ "name"; "shape"; "required" ]
            let! name = CborAccess.text entries "name"
            let! token = PortableMemberToken.tryCreate name
            let! shape = CborAccess.field entries "shape" |> Result.bind (fun item -> readShapeRef item "field-shape")
            let! required = CborAccess.boolean entries "required"

            return
                { Name = PortableMemberToken.value token
                  Shape = shape
                  Required = required }
        }

    let private encodeAlternative (alternative: AlternativeDeclaration) =
        CborMap [ "name", CborText alternative.Name; "shape", shapeReference alternative.Shape ]

    let private decodeAlternative item =
        portable {
            let! entries = CborAccess.requireMap item "alternative"
            do! CborAccess.requireDeclaredFields entries "alternative" [ "name"; "shape" ]
            let! name = CborAccess.text entries "name"
            let! token = PortableMemberToken.tryCreate name

            let! shape =
                CborAccess.field entries "shape"
                |> Result.bind (fun item -> readShapeRef item "alternative-shape")

            return
                { Name = PortableMemberToken.value token
                  Shape = shape }
        }

    // -- provisions and requirements ---------------------------------------

    let private encodeProvision (provision: Provision) =
        CborMap
            [ "kind", CborText(DependencyKind.token provision.Kind)
              "reference", CborAccess.encodeCanonical (PortableDependencyRef.canonical provision.Reference)
              "providerSpecific", CborBoolean provision.ProviderSpecific ]

    let private decodeProvision item =
        portable {
            let! entries = CborAccess.requireMap item "provision"
            do! CborAccess.requireDeclaredFields entries "provision" [ "kind"; "reference"; "providerSpecific" ]
            let! kindToken = CborAccess.text entries "kind"
            let! kind = DependencyKind.tryParse kindToken

            let! reference =
                CborAccess.field entries "reference"
                |> Result.bind (fun item -> CborAccess.readCanonical item "provision-reference")

            let! providerSpecific = CborAccess.boolean entries "providerSpecific"

            return
                { Kind = kind
                  Reference = PortableDependencyRef.ofCanonical reference
                  ProviderSpecific = providerSpecific }
        }

    let private encodeRequirement (requirement: Requirement) =
        CborMap
            [ "kind", CborText(DependencyKind.token requirement.Kind)
              "reference", CborAccess.encodeCanonical (PortableDependencyRef.canonical requirement.Reference)
              "strength", CborText(RequirementStrength.token requirement.Strength)
              "providerSpecific", CborBoolean requirement.ProviderSpecific ]

    let private decodeRequirement item =
        portable {
            let! entries = CborAccess.requireMap item "requirement"

            do!
                CborAccess.requireDeclaredFields entries "requirement" [ "kind"; "reference"; "strength"; "providerSpecific" ]

            let! kindToken = CborAccess.text entries "kind"
            let! kind = DependencyKind.tryParse kindToken

            let! reference =
                CborAccess.field entries "reference"
                |> Result.bind (fun item -> CborAccess.readCanonical item "requirement-reference")

            let! strengthToken = CborAccess.text entries "strength"
            let! strength = RequirementStrength.tryParse strengthToken
            let! providerSpecific = CborAccess.boolean entries "providerSpecific"

            return
                { Kind = kind
                  Reference = PortableDependencyRef.ofCanonical reference
                  Strength = strength
                  ProviderSpecific = providerSpecific }
        }

    // -- operations ---------------------------------------------------------

    let private encodeOperation (operation: OperationDeclaration) =
        CborMap
            [ "reference", CborAccess.encodeCanonical (PortableOperationRef.canonical operation.Reference)
              "inputShape", shapeReference operation.InputShape
              "resultShape", shapeReference operation.ResultShape
              "detailShape", shapeReference operation.DetailShape
              "requiredFragments", CborArray(operation.RequiredFragments |> List.map fragmentReference)
              "resourceFlavors", CborArray(operation.ResourceFlavors |> List.map CborText) ]

    let private decodeOperation item =
        portable {
            let! entries = CborAccess.requireMap item "operation"

            do!
                CborAccess.requireDeclaredFields
                    entries
                    "operation"
                    [ "reference"; "inputShape"; "resultShape"; "detailShape"; "requiredFragments"; "resourceFlavors" ]

            let! reference =
                CborAccess.field entries "reference"
                |> Result.bind (fun item -> CborAccess.readCanonical item "operation-reference")

            let! inputShape = CborAccess.field entries "inputShape" |> Result.bind (fun i -> readShapeRef i "inputShape")
            let! resultShape = CborAccess.field entries "resultShape" |> Result.bind (fun i -> readShapeRef i "resultShape")
            let! detailShape = CborAccess.field entries "detailShape" |> Result.bind (fun i -> readShapeRef i "detailShape")

            let! requiredFragments =
                CborAccess.arrayOf entries "requiredFragments" (fun i -> readFragmentRef i "requiredFragment")

            let! resourceFlavors =
                CborAccess.arrayOf entries "resourceFlavors" (fun i -> CborAccess.requireText i "resourceFlavor")

            return
                { Reference = PortableOperationRef.ofCanonical reference
                  InputShape = inputShape
                  ResultShape = resultShape
                  DetailShape = detailShape
                  RequiredFragments = requiredFragments
                  ResourceFlavors = resourceFlavors }
        }

    // -- shapes and fragments ------------------------------------------------

    let private encodeShape (shape: ShapeDeclaration) =
        let head = [ "reference", shapeReference shape.Reference ]

        let body =
            match shape.Body with
            | RecordBody(policy, fields) ->
                [ "kind", CborText "record"
                  "fragmentPolicy", CborText(FragmentPolicy.token policy)
                  "fields", CborArray(fields |> List.map encodeField) ]
            | SequenceBody item -> [ "kind", CborText "sequence"; "itemShape", shapeReference item ]
            | ChoiceBody alternatives ->
                [ "kind", CborText "choice"
                  "alternatives", CborArray(alternatives |> List.map encodeAlternative) ]
            | ScalarBody scalar -> [ "kind", CborText "scalar"; "scalar", CborText(PortableScalar.token scalar) ]
            | UnitBody -> [ "kind", CborText "unit" ]

        CborMap(head @ body)

    /// Reads the body for the declared kind and refuses every member the kind does not declare, so
    /// "required for one kind and forbidden for the rest" is enforced in both directions.
    let private decodeShapeBody entries kind =
        let forbid names =
            names
            |> iterate (fun name ->
                if CborAccess.contains entries name then
                    malformed "shape-members" $"'{name}' is not a member of a '{kind}' Shape."
                else
                    Ok())

        match kind with
        | "record" ->
            portable {
                do! forbid [ "itemShape"; "alternatives"; "scalar" ]
                let! policyToken = CborAccess.text entries "fragmentPolicy"
                let! policy = FragmentPolicy.tryParse policyToken
                let! fields = CborAccess.arrayOf entries "fields" decodeField
                return RecordBody(policy, fields)
            }
        | "sequence" ->
            portable {
                do! forbid [ "fragmentPolicy"; "fields"; "alternatives"; "scalar" ]
                let! item = CborAccess.field entries "itemShape" |> Result.bind (fun i -> readShapeRef i "itemShape")
                return SequenceBody item
            }
        | "choice" ->
            portable {
                do! forbid [ "fragmentPolicy"; "fields"; "itemShape"; "scalar" ]
                let! alternatives = CborAccess.arrayOf entries "alternatives" decodeAlternative
                return ChoiceBody alternatives
            }
        | "scalar" ->
            portable {
                do! forbid [ "fragmentPolicy"; "fields"; "itemShape"; "alternatives" ]
                let! token = CborAccess.text entries "scalar"
                let! scalar = PortableScalar.tryParse token
                return ScalarBody scalar
            }
        | "unit" ->
            portable {
                do! forbid [ "fragmentPolicy"; "fields"; "itemShape"; "alternatives"; "scalar" ]
                return UnitBody
            }
        | other -> malformed "shape-kind-enumeration" $"'{other}' is outside the declared shape-kind enumeration."

    let private decodeShape item =
        portable {
            let! entries = CborAccess.requireMap item "shape"

            do!
                CborAccess.requireDeclaredFields
                    entries
                    "shape"
                    [ "reference"; "kind"; "fragmentPolicy"; "fields"; "itemShape"; "alternatives"; "scalar" ]

            let! reference =
                CborAccess.field entries "reference"
                |> Result.bind (fun i -> readShapeRef i "shape-reference")

            let! kind = CborAccess.text entries "kind"
            let! body = decodeShapeBody entries kind
            return { Reference = reference; Body = body }
        }

    let private encodeFragment (fragment: FragmentDeclaration) =
        CborMap
            [ "reference", fragmentReference fragment.Reference
              "hostShape", shapeReference fragment.HostShape
              "fields", CborArray(fragment.Fields |> List.map encodeField) ]

    let private decodeFragment item =
        portable {
            let! entries = CborAccess.requireMap item "fragment"
            do! CborAccess.requireDeclaredFields entries "fragment" [ "reference"; "hostShape"; "fields" ]

            let! reference =
                CborAccess.field entries "reference"
                |> Result.bind (fun i -> readFragmentRef i "fragment-reference")

            let! hostShape = CborAccess.field entries "hostShape" |> Result.bind (fun i -> readShapeRef i "fragment-host")
            let! fields = CborAccess.arrayOf entries "fields" decodeField

            return
                { Reference = reference
                  HostShape = hostShape
                  Fields = fields }
        }

    // -- authority, representation, limits, lifecycle -------------------------

    let private encodeAuthority (authority: AuthorityDeclaration) =
        CborMap
            [ "presentationMode", CborText(AuthorityMode.token authority.PresentationMode)
              "trustBoundaryCrossed", CborBoolean authority.TrustBoundaryCrossed
              "noCapabilityTransfer", CborBoolean authority.NoCapabilityTransfer
              "constraintPolicy", CborText authority.ConstraintPolicy ]

    let private decodeAuthority entries =
        portable {
            do!
                CborAccess.requireDeclaredFields
                    entries
                    "authority"
                    [ "presentationMode"; "trustBoundaryCrossed"; "noCapabilityTransfer"; "constraintPolicy" ]

            let! modeToken = CborAccess.text entries "presentationMode"
            let! mode = AuthorityMode.tryParse modeToken
            let! trustBoundaryCrossed = CborAccess.boolean entries "trustBoundaryCrossed"
            let! noCapabilityTransfer = CborAccess.boolean entries "noCapabilityTransfer"
            let! constraintPolicy = CborAccess.text entries "constraintPolicy"

            return
                { PresentationMode = mode
                  TrustBoundaryCrossed = trustBoundaryCrossed
                  NoCapabilityTransfer = noCapabilityTransfer
                  ConstraintPolicy = constraintPolicy }
        }

    let private encodeRepresentation (representation: RepresentationDeclaration) =
        CborMap
            [ "representation", CborText representation.Representation
              "framing", CborText representation.Framing
              "resourceFlavors", CborArray(representation.ResourceFlavors |> List.map CborText)
              "acceptedResourceHandles", CborArray(representation.AcceptedResourceHandles |> List.map CborText) ]

    let private decodeRepresentation entries =
        portable {
            do!
                CborAccess.requireDeclaredFields
                    entries
                    "representation"
                    [ "representation"; "framing"; "resourceFlavors"; "acceptedResourceHandles" ]

            let! representation = CborAccess.text entries "representation"
            let! framing = CborAccess.text entries "framing"
            let! flavors = CborAccess.arrayOf entries "resourceFlavors" (fun i -> CborAccess.requireText i "resourceFlavor")

            let! handles =
                CborAccess.arrayOf entries "acceptedResourceHandles" (fun i ->
                    CborAccess.requireText i "acceptedResourceHandle")

            return
                { Representation = representation
                  Framing = framing
                  ResourceFlavors = flavors
                  AcceptedResourceHandles = handles }
        }

    let private encodeLimits (limits: PortableLimits) =
        CborMap
            [ "maxFrameBytes", CborInteger(int64 limits.MaxFrameBytes)
              "maxNestingDepth", CborInteger(int64 limits.MaxNestingDepth)
              "maxRecordFields", CborInteger(int64 limits.MaxRecordFields)
              "maxFragmentsPerRecord", CborInteger(int64 limits.MaxFragmentsPerRecord)
              "maxSequenceItems", CborInteger(int64 limits.MaxSequenceItems)
              "maxTextBytes", CborInteger(int64 limits.MaxTextBytes)
              "maxByteStringBytes", CborInteger(int64 limits.MaxByteStringBytes)
              "maxResourceBytes", CborInteger(int64 limits.MaxResourceBytes)
              "ioTimeoutMilliseconds", CborInteger(int64 limits.IoTimeoutMilliseconds)
              "maxConcurrentRequests", CborInteger(int64 limits.MaxConcurrentRequests) ]

    let private decodeLimits entries =
        portable {
            do! CborAccess.requireDeclaredFields entries "limits" limitFields
            let! maxFrameBytes = CborAccess.int32 entries "maxFrameBytes"
            let! maxNestingDepth = CborAccess.int32 entries "maxNestingDepth"
            let! maxRecordFields = CborAccess.int32 entries "maxRecordFields"
            let! maxFragmentsPerRecord = CborAccess.int32 entries "maxFragmentsPerRecord"
            let! maxSequenceItems = CborAccess.int32 entries "maxSequenceItems"
            let! maxTextBytes = CborAccess.int32 entries "maxTextBytes"
            let! maxByteStringBytes = CborAccess.int32 entries "maxByteStringBytes"
            let! maxResourceBytes = CborAccess.int32 entries "maxResourceBytes"
            let! ioTimeoutMilliseconds = CborAccess.int32 entries "ioTimeoutMilliseconds"
            let! maxConcurrentRequests = CborAccess.int32 entries "maxConcurrentRequests"

            let limits =
                { MaxFrameBytes = maxFrameBytes
                  MaxNestingDepth = maxNestingDepth
                  MaxRecordFields = maxRecordFields
                  MaxFragmentsPerRecord = maxFragmentsPerRecord
                  MaxSequenceItems = maxSequenceItems
                  MaxTextBytes = maxTextBytes
                  MaxByteStringBytes = maxByteStringBytes
                  MaxResourceBytes = maxResourceBytes
                  IoTimeoutMilliseconds = ioTimeoutMilliseconds
                  MaxConcurrentRequests = maxConcurrentRequests }

            do! PortableLimits.validate limits
            return limits
        }

    let private encodeLifecycle (lifecycle: LifecycleDeclaration) =
        CborMap
            [ "replayProtectionDeclared", CborBoolean lifecycle.ReplayProtectionDeclared
              "replayWindow", CborText lifecycle.ReplayWindow
              "features",
              CborMap(lifecycle.Features |> Map.toList |> List.map (fun (name, value) -> name, CborBoolean value)) ]

    let private decodeLifecycle entries =
        portable {
            do!
                CborAccess.requireDeclaredFields
                    entries
                    "lifecycle"
                    [ "replayProtectionDeclared"; "replayWindow"; "features" ]

            let! replayProtectionDeclared = CborAccess.boolean entries "replayProtectionDeclared"
            let! replayWindow = CborAccess.text entries "replayWindow"
            let! featureEntries = CborAccess.map entries "features"

            let! features =
                featureEntries
                |> traverse (fun (name, value) ->
                    match value with
                    | CborBoolean declared -> Ok(name, declared)
                    | _ -> malformed "feature-kind" $"Lifecycle feature '{name}' must be a boolean.")

            return
                { ReplayProtectionDeclared = replayProtectionDeclared
                  ReplayWindow = replayWindow
                  Features = Map.ofList features }
        }

    // -- document -------------------------------------------------------------

    let encode (document: ContractDocument) =
        CborMap
            [ "contractVersion", CborInteger(int64 document.ContractVersion)
              "component", CborAccess.encodeCanonical (PortableComponentRef.canonical document.Component)
              "provider", CborAccess.encodeCanonical (PortableProviderRef.canonical document.Provider)
              "provisions", CborArray(document.Provisions |> List.map encodeProvision)
              "requirements", CborArray(document.Requirements |> List.map encodeRequirement)
              "operations", CborArray(document.Operations |> List.map encodeOperation)
              "shapes", CborArray(document.Shapes |> List.map encodeShape)
              "fragments", CborArray(document.Fragments |> List.map encodeFragment)
              "authority", encodeAuthority document.Authority
              "representation", encodeRepresentation document.Representation
              "limits", encodeLimits document.Limits
              "lifecycle", encodeLifecycle document.Lifecycle ]

    let decode (item: CborItem) : PortableResult<ContractDocument> =
        portable {
            let! entries = CborAccess.requireMap item "contract"
            do! PortableForbiddenContent.requireCleanControl (CborMap entries)
            do! CborAccess.requireDeclaredFields entries "contract" documentFields
            let! contractVersion = CborAccess.int32 entries "contractVersion"

            do!
                ensure (contractVersion = ContractDocument.SupportedContractVersion) (fun () ->
                    refuse
                        ProtocolCategory.UnsupportedVersion
                        "contract-version"
                        $"Contract version {contractVersion} is not recognized.")

            let! component' =
                CborAccess.field entries "component"
                |> Result.bind (fun i -> CborAccess.readCanonical i "component")

            let! provider =
                CborAccess.field entries "provider"
                |> Result.bind (fun i -> CborAccess.readCanonical i "provider")

            let! provisions = CborAccess.arrayOf entries "provisions" decodeProvision
            let! requirements = CborAccess.arrayOf entries "requirements" decodeRequirement
            let! operations = CborAccess.arrayOf entries "operations" decodeOperation
            let! shapes = CborAccess.arrayOf entries "shapes" decodeShape
            let! fragments = CborAccess.arrayOf entries "fragments" decodeFragment
            let! authority = CborAccess.map entries "authority" |> Result.bind decodeAuthority
            let! representation = CborAccess.map entries "representation" |> Result.bind decodeRepresentation
            let! limits = CborAccess.map entries "limits" |> Result.bind decodeLimits
            let! lifecycle = CborAccess.map entries "lifecycle" |> Result.bind decodeLifecycle

            return
                { ContractVersion = contractVersion
                  Component = PortableComponentRef.ofCanonical component'
                  Provider = PortableProviderRef.ofCanonical provider
                  Provisions = provisions
                  Requirements = requirements
                  Operations = operations
                  Shapes = shapes
                  Fragments = fragments
                  Authority = authority
                  Representation = representation
                  Limits = limits
                  Lifecycle = lifecycle }
        }
