namespace Brontide.Minimal.Binding.Portable

/// The Shape floor's built-in references, which no contract needs to redeclare.
[<RequireQualifiedAccess>]
module PortableBuiltInShapes =

    let private literal name version =
        match PortableShapeRef.tryCreate name version with
        | Ok reference -> reference
        // A built-in outside the portable profile would be a defect in this module, not a
        // condition any peer can present, so it is not part of the portable failure model.
        | Error _ -> invalidOp $"'{name}@{version}' is not a portable Shape reference."

    let text = literal "Text" 1
    let boolean = literal "Boolean" 1
    let signed64 = literal "Integer.Signed64" 1
    let decimal = literal "Decimal" 1
    let bytes = literal "Bytes" 1
    let unit = literal "Unit" 1

    let declarations =
        [ { Reference = text; Body = ScalarBody PortableScalar.Text }
          { Reference = boolean; Body = ScalarBody PortableScalar.Boolean }
          { Reference = signed64; Body = ScalarBody PortableScalar.Signed64 }
          { Reference = decimal; Body = ScalarBody PortableScalar.Decimal }
          { Reference = bytes; Body = ScalarBody PortableScalar.Bytes }
          { Reference = unit; Body = UnitBody } ]

/// The Shape graph of one established contract, plus the projection rule that decides when a
/// version difference is additive.
///
/// An unresolvable reference fails as unsupported-contract rather than as a decode refusal, because
/// an unknown Shape, Fragment, or dependency is a contract fact and not a byte-level one.
[<StructuralEquality; NoComparison>]
type ShapeCatalog =
    private
        { Declarations: Map<PortableShapeRef, ShapeDeclaration>
          FragmentDeclarations: Map<PortableFragmentRef, FragmentDeclaration>
          Bounds: PortableLimits }

[<RequireQualifiedAccess>]
module ShapeCatalog =

    let limits catalog = catalog.Bounds

    let tryShape reference catalog = Map.tryFind reference catalog.Declarations

    let shape reference catalog : PortableResult<ShapeDeclaration> =
        match tryShape reference catalog with
        | Some declaration -> Ok declaration
        | None ->
            unsupportedContract
                "unknown-shape"
                $"Shape {PortableShapeRef.text reference} is not part of the established contract."

    let tryFragment reference catalog = Map.tryFind reference catalog.FragmentDeclarations

    let fragment reference catalog : PortableResult<FragmentDeclaration> =
        match tryFragment reference catalog with
        | Some declaration -> Ok declaration
        | None ->
            unsupportedContract
                "unknown-fragment"
                $"Fragment {PortableFragmentRef.text reference} is not part of the established contract."

    let private fieldsOf body =
        match body with
        | RecordBody(_, fields) -> fields
        | SequenceBody _
        | ChoiceBody _
        | ScalarBody _
        | UnitBody -> []

    let private requireUniqueFieldNames (owner: string) (fields: FieldDeclaration list) =
        let rec walk seen remaining =
            match remaining with
            | [] -> Ok()
            | (field: FieldDeclaration) :: tail ->
                match PortableMemberToken.tryCreate field.Name with
                | Error error -> Error error
                | Ok _ ->
                    if Set.contains field.Name seen then
                        malformed "duplicate-field" $"{owner} declares field '{field.Name}' more than once."
                    else
                        walk (Set.add field.Name seen) tail

        walk Set.empty fields

    let private addShape (declarations: Map<PortableShapeRef, ShapeDeclaration>) (declaration: ShapeDeclaration) =
        if Map.containsKey declaration.Reference declarations then
            malformed "duplicate-shape" $"Shape {PortableShapeRef.text declaration.Reference} is declared more than once."
        else
            requireUniqueFieldNames $"Shape {PortableShapeRef.text declaration.Reference}" (fieldsOf declaration.Body)
            |> Result.map (fun () -> Map.add declaration.Reference declaration declarations)

    let private addFragment (declarations: Map<PortableFragmentRef, FragmentDeclaration>) (declaration: FragmentDeclaration) =
        if Map.containsKey declaration.Reference declarations then
            malformed
                "duplicate-fragment"
                $"Fragment {PortableFragmentRef.text declaration.Reference} is declared more than once."
        else
            requireUniqueFieldNames $"Fragment {PortableFragmentRef.text declaration.Reference}" declaration.Fields
            |> Result.map (fun () -> Map.add declaration.Reference declaration declarations)

    let private requireResolvable (document: ContractDocument) catalog =
        let resolveShape reference = shape reference catalog |> Result.map ignore
        let resolveFragment reference = fragment reference catalog |> Result.map ignore

        portable {
            for declaration in document.Shapes do
                match declaration.Body with
                | RecordBody(_, fields) -> do! fields |> iterate (fun field -> resolveShape field.Shape)
                | SequenceBody item -> do! resolveShape item
                | ChoiceBody alternatives ->
                    do! alternatives |> iterate (fun alternative -> resolveShape alternative.Shape)
                | ScalarBody _
                | UnitBody -> ()

            for declaration in document.Fragments do
                do! resolveShape declaration.HostShape
                do! declaration.Fields |> iterate (fun field -> resolveShape field.Shape)

            for operation in document.Operations do
                do! resolveShape operation.InputShape
                do! resolveShape operation.ResultShape
                do! resolveShape operation.DetailShape
                do! operation.RequiredFragments |> iterate resolveFragment
        }

    let fromContract (document: ContractDocument) : PortableResult<ShapeCatalog> =
        let seedShapes =
            PortableBuiltInShapes.declarations
            |> List.map (fun declaration -> declaration.Reference, declaration)
            |> Map.ofList

        let rec foldShapes declarations remaining =
            match remaining with
            | [] -> Ok declarations
            | head :: tail -> addShape declarations head |> Result.bind (fun next -> foldShapes next tail)

        let rec foldFragments declarations remaining =
            match remaining with
            | [] -> Ok declarations
            | head :: tail -> addFragment declarations head |> Result.bind (fun next -> foldFragments next tail)

        portable {
            let! shapes = foldShapes seedShapes document.Shapes
            let! fragments = foldFragments Map.empty document.Fragments

            let catalog =
                { Declarations = shapes
                  FragmentDeclarations = fragments
                  Bounds = document.Limits }

            do! requireResolvable document catalog
            return catalog
        }

    /// Decides whether the later Shape version differs from the earlier one only additively: new
    /// optional fields. A new required field, a removed field, a changed field Shape, or a changed
    /// cardinality is not additive.
    let isAdditiveOver later earlier catalog =
        if PortableShapeRef.name later <> PortableShapeRef.name earlier
           || PortableShapeRef.version later < PortableShapeRef.version earlier then
            false
        elif later = earlier then
            true
        else
            match tryShape later catalog, tryShape earlier catalog with
            | Some laterShape, Some earlierShape ->
                match laterShape.Body, earlierShape.Body with
                | RecordBody(laterPolicy, laterFields), RecordBody(earlierPolicy, earlierFields) ->
                    let laterByName =
                        laterFields |> List.map (fun field -> field.Name, field) |> Map.ofList

                    let earlierNames = earlierFields |> List.map (fun field -> field.Name) |> Set.ofList

                    laterPolicy = earlierPolicy
                    && earlierFields
                       |> List.forall (fun field ->
                           match Map.tryFind field.Name laterByName with
                           | Some candidate -> candidate = field
                           | None -> false)
                    && laterFields
                       |> List.forall (fun field -> Set.contains field.Name earlierNames || not field.Required)
                | laterBody, earlierBody -> laterBody = earlierBody
            | _ -> false
