namespace Brontide.Minimal.Extensions.Events

open Brontide.Minimal.Model

type StreamEvent<'event> =
    { Stream: string
      Position: int64
      Event: 'event }

type EventStream<'event> =
    private
        { Name: string
          Events: StreamEvent<'event> list }

[<RequireQualifiedAccess>]
module EventStream =
    let empty name = { Name = name; Events = [] }
    let name stream = stream.Name
    let version stream = stream.Events |> List.tryLast |> Option.map _.Position |> Option.defaultValue -1L
    let read stream = stream.Events

    let append expectedVersion events stream =
        let actualVersion = version stream

        if expectedVersion <> actualVersion then
            Error $"Expected stream version {expectedVersion}, but found {actualVersion}."
        else
            let appended =
                events
                |> List.mapi (fun index event ->
                    { Stream = stream.Name
                      Position = actualVersion + int64 index + 1L
                      Event = event })

            Ok
                { stream with
                    Events = stream.Events @ appended }

    let fold evolve initial stream =
        stream.Events |> List.fold (fun state envelope -> evolve state envelope.Event) initial

type Causation =
    { Execution: ExecutionReference
      Occurrence: OccurrenceReference option }
