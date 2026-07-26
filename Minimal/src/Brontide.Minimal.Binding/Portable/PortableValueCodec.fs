namespace Brontide.Minimal.Binding.Portable

/// The schema-guided value codec: the negotiated Shape determines the type of every position, so
/// values carry no kind discriminator on the wire.
///
/// This is the difference from the retained inline-tagged JSON representation, whose values are
/// self-describing. Because the wire carries no discriminator, a value that does not match its
/// declared Shape is a refusal rather than a coercion.
[<RequireQualifiedAccess>]
module PortableValueCodec =

    let private mismatch reference expected =
        invalidPayload "shape-mismatch" $"Shape {PortableShapeRef.text reference} requires a {expected} value."

    let private requireBound count bound what =
        if count > bound then
            limitExceeded $"{what}-bound" $"A {what} of {count} exceeds the declared bound of {bound}."
        else
            Ok()

    let private encodeScalar scalar reference value =
        match scalar, value with
        | PortableScalar.Text, PortableText text -> Ok(CborText text)
        | PortableScalar.Boolean, PortableBoolean flag -> Ok(CborBoolean flag)
        | PortableScalar.Signed64, PortableInteger number -> Ok(CborInteger number)
        | PortableScalar.Decimal, PortableDecimalValue(exponent, mantissa) ->
            CborDecimal.normalize exponent mantissa
            |> Result.map (fun (exponent, mantissa) -> CborDecimal(exponent, mantissa))
        | PortableScalar.Bytes, PortableBytesValue bytes -> Ok(CborBytes bytes)
        | _ -> mismatch reference (PortableScalar.token scalar)

    let private decodeScalar scalar reference item =
        match scalar, item with
        | PortableScalar.Text, CborText text -> Ok(PortableText text)
        | PortableScalar.Boolean, CborBoolean flag -> Ok(PortableBoolean flag)
        | PortableScalar.Signed64, CborInteger number -> Ok(PortableInteger number)
        | PortableScalar.Decimal, CborDecimal(exponent, mantissa) -> Ok(PortableDecimalValue(exponent, mantissa))
        | PortableScalar.Bytes, CborBytes bytes -> Ok(PortableBytesValue bytes)
        | _ -> mismatch reference (PortableScalar.token scalar)

    let private alternativeOf alternatives reference name =
        match alternatives |> List.tryFind (fun (candidate: AlternativeDeclaration) -> candidate.Name = name) with
        | Some alternative -> Ok alternative
        | None ->
            invalidPayload
                "unknown-alternative"
                $"Shape {PortableShapeRef.text reference} declares no alternative '{name}'; the envelope kind was recognized, so this is a payload decision."

    /// Refuses a Fragment the negotiated Operation does not declare on a closed Shape, an
    /// undeclared Fragment, and a Fragment whose host Shape is not this one.
    let private admitFragment catalog (shapeRef: PortableShapeRef) policy operationFragments reference =
        portable {
            do!
                ensure (policy = FragmentPolicy.Open || List.contains reference operationFragments) (fun () ->
                    invalidPayload
                        "closed-fragment-policy"
                        $"Shape {PortableShapeRef.text shapeRef} is closed and refuses Fragment {PortableFragmentRef.text reference}, which the negotiated Operation does not declare.")

            let! declaration =
                match ShapeCatalog.tryFragment reference catalog with
                | Some declaration -> Ok declaration
                | None ->
                    invalidPayload
                        "undeclared-fragment"
                        $"Fragment {PortableFragmentRef.text reference} is not declared by the established contract, so its fields have no Shape."

            do!
                ensure
                    (PortableShapeRef.name declaration.HostShape = PortableShapeRef.name shapeRef
                     && PortableShapeRef.version declaration.HostShape <= PortableShapeRef.version shapeRef)
                    (fun () ->
                        invalidPayload
                            "fragment-host"
                            $"Fragment {PortableFragmentRef.text reference} is not attachable to {PortableShapeRef.text shapeRef}.")

            return declaration
        }

    let private requireRequiredFields owner (declared: FieldDeclaration list) (present: string -> bool) =
        declared
        |> iterate (fun field ->
            if field.Required && not (present field.Name) then
                invalidPayload "required-field-absent" $"{owner} requires field '{field.Name}'."
            else
                Ok())

    // -- encoding -----------------------------------------------------------

    let rec encode catalog reference value : PortableResult<CborItem> =
        portable {
            let! declaration = ShapeCatalog.shape reference catalog

            match declaration.Body with
            | UnitBody ->
                match value with
                | PortableUnit -> return CborNull
                | _ -> return! mismatch reference "unit"
            | ScalarBody scalar -> return! encodeScalar scalar reference value
            | SequenceBody item ->
                match value with
                | PortableSequence items ->
                    do! requireBound (List.length items) (ShapeCatalog.limits catalog).MaxSequenceItems "sequence"
                    let! encoded = items |> traverse (encode catalog item)
                    return CborArray encoded
                | _ -> return! mismatch reference "sequence"
            | ChoiceBody alternatives ->
                match value with
                | PortableChoice(name, inner) ->
                    let! alternative = alternativeOf alternatives reference name
                    let! encoded = encode catalog alternative.Shape inner
                    return CborArray [ CborText name; encoded ]
                | _ -> return! mismatch reference "choice"
            | RecordBody(_, declaredFields) -> return! encodeRecord catalog reference declaredFields value
        }

    and private encodeRecord catalog reference declaredFields value =
        match value with
        | PortableRecord(fields, fragments) ->
            portable {
                let bounds = ShapeCatalog.limits catalog
                do! requireBound (Map.count fields) bounds.MaxRecordFields "record"
                do! requireBound (Map.count fragments) bounds.MaxFragmentsPerRecord "fragment map"

                let declaredNames = declaredFields |> List.map (fun field -> field.Name) |> Set.ofList

                do!
                    fields
                    |> Map.toList
                    |> iterate (fun (name, _) ->
                        if Set.contains name declaredNames then
                            Ok()
                        else
                            invalidPayload
                                "undeclared-field"
                                $"Shape {PortableShapeRef.text reference} declares no field '{name}'.")

                // An optional field carrying no value is omitted rather than encoded as null;
                // omission is what makes additive projection possible.
                let! encodedFields =
                    declaredFields
                    |> traverse (fun field ->
                        match Map.tryFind field.Name fields with
                        | Some fieldValue -> encode catalog field.Shape fieldValue |> Result.map (fun item -> [ field.Name, item ])
                        | None when field.Required ->
                            invalidPayload
                                "required-field-absent"
                                $"Shape {PortableShapeRef.text reference} requires field '{field.Name}'."
                        | None -> Ok [])

                let! encodedFragments =
                    fragments
                    |> Map.toList
                    |> traverse (fun (fragmentRef, fragmentFields) ->
                        portable {
                            let! declaration = ShapeCatalog.fragment fragmentRef catalog

                            let! encoded =
                                declaration.Fields
                                |> traverse (fun field ->
                                    match Map.tryFind field.Name fragmentFields with
                                    | Some fieldValue ->
                                        encode catalog field.Shape fieldValue
                                        |> Result.map (fun item -> [ field.Name, item ])
                                    | None when field.Required ->
                                        invalidPayload
                                            "required-field-absent"
                                            $"Fragment {PortableFragmentRef.text fragmentRef} requires field '{field.Name}'."
                                    | None -> Ok [])

                            return PortableFragmentRef.text fragmentRef, CborMap(List.concat encoded)
                        })

                return CborArray [ CborMap(List.concat encodedFields); CborMap encodedFragments ]
            }
        | _ -> mismatch reference "record"

    // -- decoding -----------------------------------------------------------

    let rec decode catalog reference operationFragments item : PortableResult<PortableValue> =
        portable {
            let! declaration = ShapeCatalog.shape reference catalog

            match declaration.Body with
            | UnitBody ->
                match item with
                | CborNull -> return PortableUnit
                | _ -> return! mismatch reference "unit"
            | ScalarBody scalar -> return! decodeScalar scalar reference item
            | SequenceBody itemShape ->
                match item with
                | CborArray items ->
                    do! requireBound (List.length items) (ShapeCatalog.limits catalog).MaxSequenceItems "sequence"
                    let! decoded = items |> traverse (decode catalog itemShape operationFragments)
                    return PortableSequence decoded
                | _ -> return! mismatch reference "sequence"
            | ChoiceBody alternatives ->
                match item with
                | CborArray [ CborText name; body ] ->
                    let! alternative = alternativeOf alternatives reference name
                    let! decoded = decode catalog alternative.Shape operationFragments body
                    return PortableChoice(name, decoded)
                | _ -> return! mismatch reference "choice"
            | RecordBody(policy, declaredFields) ->
                return! decodeRecord catalog reference policy declaredFields operationFragments item
        }

    and private decodeRecord catalog reference policy declaredFields operationFragments item =
        match item with
        | CborArray [ CborMap fieldEntries; CborMap fragmentEntries ] ->
            portable {
                let bounds = ShapeCatalog.limits catalog
                do! requireBound (List.length fieldEntries) bounds.MaxRecordFields "record"
                do! requireBound (List.length fragmentEntries) bounds.MaxFragmentsPerRecord "fragment map"

                let declaredByName =
                    declaredFields |> List.map (fun field -> field.Name, field) |> Map.ofList

                let! fields =
                    fieldEntries
                    |> traverse (fun (name, value) ->
                        match Map.tryFind name declaredByName with
                        | Some field ->
                            decode catalog field.Shape operationFragments value
                            |> Result.map (fun decoded -> name, decoded)
                        | None ->
                            invalidPayload
                                "undeclared-field"
                                $"Shape {PortableShapeRef.text reference} declares no field '{name}'.")

                let fieldMap = Map.ofList fields

                do!
                    requireRequiredFields
                        $"Shape {PortableShapeRef.text reference}"
                        declaredFields
                        (fun name -> Map.containsKey name fieldMap)

                let! fragments =
                    fragmentEntries
                    |> traverse (fun (key, value) ->
                        portable {
                            let! fragmentRef = PortableFragmentRef.tryParseText key
                            let! declaration = admitFragment catalog reference policy operationFragments fragmentRef
                            let! entries = CborAccess.requireMap value "fragment"
                            let! decoded = decodeFragmentFields catalog declaration operationFragments entries
                            return fragmentRef, decoded
                        })

                return PortableRecord(fieldMap, Map.ofList fragments)
            }
        | _ -> mismatch reference "record"

    and private decodeFragmentFields catalog (declaration: FragmentDeclaration) operationFragments entries =
        portable {
            let declaredByName =
                declaration.Fields |> List.map (fun field -> field.Name, field) |> Map.ofList

            let! fields =
                entries
                |> traverse (fun (name, value) ->
                    match Map.tryFind name declaredByName with
                    | Some field ->
                        decode catalog field.Shape operationFragments value
                        |> Result.map (fun decoded -> name, decoded)
                    | None ->
                        invalidPayload
                            "undeclared-field"
                            $"Fragment {PortableFragmentRef.text declaration.Reference} declares no field '{name}'.")

            let fieldMap = Map.ofList fields

            do!
                requireRequiredFields
                    $"Fragment {PortableFragmentRef.text declaration.Reference}"
                    declaration.Fields
                    (fun name -> Map.containsKey name fieldMap)

            return fieldMap
        }

    // -- validation ---------------------------------------------------------

    /// Validates an in-memory value against its declared Shape without producing bytes.
    ///
    /// The fixed direct-call realization has no wire, so this is where it applies the same
    /// conformance rules the decoder applies. Encoding and decoding to check a direct call would
    /// manufacture a copy the realization does not make, and copy accounting is a reported fact.
    let rec validate catalog reference operationFragments value : PortableResult<unit> =
        portable {
            let! declaration = ShapeCatalog.shape reference catalog

            match declaration.Body with
            | UnitBody ->
                match value with
                | PortableUnit -> ()
                | _ -> return! mismatch reference "unit"
            | ScalarBody scalar ->
                let! _ = encodeScalar scalar reference value
                ()
            | SequenceBody itemShape ->
                match value with
                | PortableSequence items ->
                    do! requireBound (List.length items) (ShapeCatalog.limits catalog).MaxSequenceItems "sequence"
                    do! items |> iterate (validate catalog itemShape operationFragments)
                | _ -> return! mismatch reference "sequence"
            | ChoiceBody alternatives ->
                match value with
                | PortableChoice(name, inner) ->
                    let! alternative = alternativeOf alternatives reference name
                    do! validate catalog alternative.Shape operationFragments inner
                | _ -> return! mismatch reference "choice"
            | RecordBody(policy, declaredFields) ->
                match value with
                | PortableRecord(fields, fragments) ->
                    let bounds = ShapeCatalog.limits catalog
                    do! requireBound (Map.count fields) bounds.MaxRecordFields "record"
                    do! requireBound (Map.count fragments) bounds.MaxFragmentsPerRecord "fragment map"

                    let declaredByName =
                        declaredFields |> List.map (fun field -> field.Name, field) |> Map.ofList

                    do!
                        fields
                        |> Map.toList
                        |> iterate (fun (name, fieldValue) ->
                            match Map.tryFind name declaredByName with
                            | Some field -> validate catalog field.Shape operationFragments fieldValue
                            | None ->
                                invalidPayload
                                    "undeclared-field"
                                    $"Shape {PortableShapeRef.text reference} declares no field '{name}'.")

                    do!
                        requireRequiredFields
                            $"Shape {PortableShapeRef.text reference}"
                            declaredFields
                            (fun name -> Map.containsKey name fields)

                    do!
                        fragments
                        |> Map.toList
                        |> iterate (fun (fragmentRef, fragmentFields) ->
                            portable {
                                let! declaration = admitFragment catalog reference policy operationFragments fragmentRef

                                let declaredFragmentFields =
                                    declaration.Fields |> List.map (fun field -> field.Name, field) |> Map.ofList

                                do!
                                    fragmentFields
                                    |> Map.toList
                                    |> iterate (fun (name, fieldValue) ->
                                        match Map.tryFind name declaredFragmentFields with
                                        | Some field -> validate catalog field.Shape operationFragments fieldValue
                                        | None ->
                                            invalidPayload
                                                "undeclared-field"
                                                $"Fragment {PortableFragmentRef.text fragmentRef} declares no field '{name}'.")

                                do!
                                    requireRequiredFields
                                        $"Fragment {PortableFragmentRef.text fragmentRef}"
                                        declaration.Fields
                                        (fun name -> Map.containsKey name fragmentFields)
                            })
                | _ -> return! mismatch reference "record"
        }

    // -- projection ---------------------------------------------------------

    /// Projects a presented value onto the Shape version the consumer recognizes, reporting the
    /// mapping obligations the projection discharged.
    ///
    /// Projection discards unrecognized additive structure. It is therefore never applied to an
    /// authority or control position: discarding narrowing semantics would widen authority.
    let project catalog presented accepted value : PortableResult<PortableValue * string list> =
        if presented = accepted then
            Ok(value, [])
        elif not (ShapeCatalog.isAdditiveOver presented accepted catalog) then
            invalidPayload
                "non-additive-projection"
                $"Shape {PortableShapeRef.text presented} does not differ from {PortableShapeRef.text accepted} additively, so it cannot be projected."
        else
            let obligation =
                $"projected:{PortableShapeRef.text presented}->{PortableShapeRef.text accepted}"

            ShapeCatalog.shape accepted catalog
            |> Result.map (fun declaration ->
                match declaration.Body, value with
                | RecordBody(_, declaredFields), PortableRecord(fields, fragments) ->
                    let declaredNames = declaredFields |> List.map (fun field -> field.Name) |> Set.ofList

                    let retained =
                        fields |> Map.filter (fun name _ -> Set.contains name declaredNames)

                    let dropped =
                        fields
                        |> Map.toList
                        |> List.map fst
                        |> List.filter (fun name -> not (Set.contains name declaredNames))
                        |> List.sort

                    PortableRecord(retained, fragments), obligation :: (dropped |> List.map (fun name -> $"field-projected:{name}"))
                | _ -> value, [ obligation ])
