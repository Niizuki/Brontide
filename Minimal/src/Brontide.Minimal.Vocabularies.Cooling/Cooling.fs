namespace Brontide.Minimal.Vocabularies.Cooling

open Brontide.Minimal.Model

type CoolingState =
    { Loop: string
      TargetTemperature: decimal
      MeasuredTemperature: decimal
      CoolingEnabled: bool
      Revision: int64 }

type CoolingCommand =
    | SetTargetTemperature of decimal
    | RecordMeasurement of decimal
    | SetCoolingEnabled of bool

type CoolingEvent =
    | TargetTemperatureChanged of decimal
    | TemperatureMeasured of decimal
    | CoolingEnabledChanged of bool

type CoolingTransition =
    { Before: CoolingState
      Command: CoolingCommand
      After: CoolingState
      Events: CoolingEvent list }

[<RequireQualifiedAccess>]
module Cooling =
    let operation: OperationReference =
        { Name = CanonicalName.create "Brontide.Minimal:Cooling.Apply" }

    let changed: EventReference =
        { Name = CanonicalName.create "Brontide.Minimal:Cooling.Changed" }

    let initial loop target measured =
        { Loop = loop
          TargetTemperature = target
          MeasuredTemperature = measured
          CoolingEnabled = measured > target
          Revision = 0L }

    let private updateControl state =
        { state with CoolingEnabled = state.MeasuredTemperature > state.TargetTemperature }

    let apply command state =
        let after, primaryEvent =
            match command with
            | SetTargetTemperature target ->
                { state with
                    TargetTemperature = target
                    Revision = state.Revision + 1L },
                TargetTemperatureChanged target
            | RecordMeasurement measured ->
                { state with
                    MeasuredTemperature = measured
                    Revision = state.Revision + 1L },
                TemperatureMeasured measured
            | SetCoolingEnabled enabled ->
                { state with
                    CoolingEnabled = enabled
                    Revision = state.Revision + 1L },
                CoolingEnabledChanged enabled

        let controlled =
            match command with
            | SetCoolingEnabled _ -> after
            | _ -> updateControl after

        let events =
            if controlled.CoolingEnabled = state.CoolingEnabled then
                [ primaryEvent ]
            else
                [ primaryEvent; CoolingEnabledChanged controlled.CoolingEnabled ]

        { Before = state
          Command = command
          After = controlled
          Events = events }

    let encodeState state =
        RecordValue(
            Map.ofList
                [ "loop", TextValue state.Loop
                  "targetTemperature", DecimalValue state.TargetTemperature
                  "measuredTemperature", DecimalValue state.MeasuredTemperature
                  "coolingEnabled", BooleanValue state.CoolingEnabled
                  "revision", IntegerValue state.Revision ],
            Map.empty
        )

    let tryDecodeState value =
        match value with
        | RecordValue(fields, _) ->
            match
                Map.tryFind "loop" fields,
                Map.tryFind "targetTemperature" fields,
                Map.tryFind "measuredTemperature" fields,
                Map.tryFind "coolingEnabled" fields,
                Map.tryFind "revision" fields
            with
            | Some(TextValue loop),
              Some(DecimalValue target),
              Some(DecimalValue measured),
              Some(BooleanValue enabled),
              Some(IntegerValue revision) ->
                Ok
                    { Loop = loop
                      TargetTemperature = target
                      MeasuredTemperature = measured
                      CoolingEnabled = enabled
                      Revision = revision }
            | _ -> Error "The value is not a complete Cooling state."
        | _ -> Error "Cooling state must be a record value."
