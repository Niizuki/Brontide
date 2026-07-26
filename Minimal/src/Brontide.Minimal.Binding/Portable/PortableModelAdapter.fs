namespace Brontide.Minimal.Binding.Portable

open System
open Brontide.Minimal.Model

/// The Minimal-owned adapter between the stack's own Shape value model and the neutral positions.
///
/// It is the only place where a portable value meets a Model value. The reusable layer never sees a
/// Model type and the Model never sees a portable one, which is what keeps the portable contract
/// implementable without importing either private model.
[<RequireQualifiedAccess>]
module PortableModelAdapter =

    let toModelShape (reference: PortableShapeRef) : ShapeReference =
        { Name = CanonicalName.create (PortableShapeRef.name reference)
          Version = PortableShapeRef.version reference }

    let toModelFragment (reference: PortableFragmentRef) : FragmentReference =
        { Name = CanonicalName.create (PortableFragmentRef.name reference)
          Version = PortableFragmentRef.version reference }

    let ofModelShape (reference: ShapeReference) =
        PortableShapeRef.tryCreate (CanonicalName.value reference.Name) reference.Version

    let ofModelFragment (reference: FragmentReference) =
        PortableFragmentRef.tryCreate (CanonicalName.value reference.Name) reference.Version

    /// The exact decimal fraction the portable core carries, rendered as the Model's decimal.
    ///
    /// The conversion is refused rather than rounded when the fraction does not fit: a silently
    /// rounded authority-adjacent number would be a semantic change disguised as a representation
    /// one.
    let toModelDecimal exponent (mantissa: int64) : PortableResult<decimal> =
        try
            if exponent >= 0 then
                Ok(decimal mantissa * pown 10m exponent)
            else
                Ok(decimal mantissa / pown 10m -exponent)
        with :? OverflowException ->
            invalidPayload "decimal-range" "The declared decimal fraction is outside the Model's decimal range."

    let ofModelDecimal (value: decimal) : PortableResult<int * int64> =
        let bits = Decimal.GetBits value
        let high = bits.[2]

        if high <> 0 then
            invalidPayload "decimal-range" "A decimal outside the Integer.Signed64 mantissa range is not portable."
        else
            let scale = int ((uint32 bits.[3] >>> 16) &&& 0xFFu)
            let magnitude = (uint64 (uint32 bits.[1]) <<< 32) ||| uint64 (uint32 bits.[0])

            if magnitude > uint64 Int64.MaxValue then
                invalidPayload "decimal-range" "A decimal outside the Integer.Signed64 mantissa range is not portable."
            else
                let negative = bits.[3] < 0
                let mantissa = if negative then -(int64 magnitude) else int64 magnitude
                CborDecimal.normalize -scale mantissa

    let rec toModel (value: PortableValue) : PortableResult<ShapeValue> =
        match value with
        | PortableUnit -> Ok UnitValue
        | PortableText text -> Ok(TextValue text)
        | PortableBoolean flag -> Ok(BooleanValue flag)
        | PortableInteger number -> Ok(IntegerValue number)
        | PortableDecimalValue(exponent, mantissa) -> toModelDecimal exponent mantissa |> Result.map DecimalValue
        | PortableBytesValue bytes -> Ok(BytesValue bytes)
        | PortableSequence items -> items |> traverse toModel |> Result.map SequenceValue
        | PortableChoice(alternative, inner) -> toModel inner |> Result.map (fun inner -> ChoiceValue(alternative, inner))
        | PortableRecord(fields, fragments) ->
            portable {
                let! modelFields =
                    fields
                    |> Map.toList
                    |> traverse (fun (name, child) -> toModel child |> Result.map (fun child -> name, child))

                let! modelFragments =
                    fragments
                    |> Map.toList
                    |> traverse (fun (reference, fragmentFields) ->
                        fragmentFields
                        |> Map.toList
                        |> traverse (fun (name, child) -> toModel child |> Result.map (fun child -> name, child))
                        |> Result.map (fun fields ->
                            toModelFragment reference, RecordValue(Map.ofList fields, Map.empty)))

                return RecordValue(Map.ofList modelFields, Map.ofList modelFragments)
            }

    let rec ofModel (value: ShapeValue) : PortableResult<PortableValue> =
        match value with
        | UnitValue -> Ok PortableUnit
        | TextValue text -> Ok(PortableText text)
        | BooleanValue flag -> Ok(PortableBoolean flag)
        | IntegerValue number -> Ok(PortableInteger number)
        | DecimalValue number ->
            ofModelDecimal number
            |> Result.map (fun (exponent, mantissa) -> PortableDecimalValue(exponent, mantissa))
        | BytesValue bytes -> Ok(PortableBytesValue bytes)
        | SequenceValue items -> items |> traverse ofModel |> Result.map PortableSequence
        | ChoiceValue(alternative, inner) ->
            ofModel inner |> Result.map (fun inner -> PortableChoice(alternative, inner))
        | RecordValue(fields, fragments) ->
            portable {
                let! portableFields =
                    fields
                    |> Map.toList
                    |> traverse (fun (name, child) -> ofModel child |> Result.map (fun child -> name, child))

                let! portableFragments =
                    fragments
                    |> Map.toList
                    |> traverse (fun (reference, fragmentValue) ->
                        portable {
                            let! reference = ofModelFragment reference

                            match fragmentValue with
                            | RecordValue(fragmentFields, _) ->
                                let! fields =
                                    fragmentFields
                                    |> Map.toList
                                    |> traverse (fun (name, child) -> ofModel child |> Result.map (fun child -> name, child))

                                return reference, Map.ofList fields
                            | _ ->
                                return!
                                    invalidPayload
                                        "fragment-shape"
                                        "An attached Fragment carries a record of its declared fields."
                        })

                return PortableRecord(Map.ofList portableFields, Map.ofList portableFragments)
            }
