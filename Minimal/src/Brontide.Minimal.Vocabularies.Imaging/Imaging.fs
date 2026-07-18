namespace Brontide.Minimal.Vocabularies.Imaging

open System
open Brontide.Minimal.Model

type PixelFormat =
    | Grayscale8
    | Rgb24

type Image =
    private
        { Width: int
          Height: int
          Format: PixelFormat
          Pixels: byte array }

type ImageOperation =
    | Invert
    | Threshold of byte

type ImageLayer =
    { Name: CanonicalName
      Image: Image
      Visible: bool }

type ImageWorkspace =
    { Layers: ImageLayer list
      ActiveLayer: CanonicalName option }

[<RequireQualifiedAccess>]
module Image =
    let private channelCount = function
        | Grayscale8 -> 1
        | Rgb24 -> 3

    let tryCreate width height format (pixels: byte array) =
        let expectedLength = width * height * channelCount format

        if width <= 0 || height <= 0 then
            Error "Image dimensions must be positive."
        elif pixels.Length <> expectedLength then
            Error $"Expected {expectedLength} pixel bytes, but received {pixels.Length}."
        else
            Ok
                { Width = width
                  Height = height
                  Format = format
                  Pixels = Array.copy pixels }

    let width image = image.Width
    let height image = image.Height
    let format image = image.Format
    let pixels image = Array.copy image.Pixels

    let apply operation image =
        let transform value =
            match operation with
            | Invert -> Byte.MaxValue - value
            | Threshold threshold -> if value >= threshold then Byte.MaxValue else Byte.MinValue

        { image with Pixels = image.Pixels |> Array.map transform }

    let histogram image =
        image.Pixels
        |> Array.countBy id
        |> Map.ofArray

[<RequireQualifiedAccess>]
module Workspace =
    let empty = { Layers = []; ActiveLayer = None }

    let addLayer layer workspace =
        if workspace.Layers |> List.exists (fun existing -> existing.Name = layer.Name) then
            Error "An image layer with that name already exists."
        else
            Ok
                { Layers = workspace.Layers @ [ layer ]
                  ActiveLayer = Some layer.Name }

    let setVisibility layerName visible workspace =
        if workspace.Layers |> List.exists (fun layer -> layer.Name = layerName) then
            Ok
                { workspace with
                    Layers =
                        workspace.Layers
                        |> List.map (fun layer ->
                            if layer.Name = layerName then
                                { layer with Visible = visible }
                            else
                                layer) }
        else
            Error "The image layer is unknown."

    let applyToActive operation workspace =
        match workspace.ActiveLayer with
        | None -> Error "The image workspace has no active layer."
        | Some active ->
            Ok
                { workspace with
                    Layers =
                        workspace.Layers
                        |> List.map (fun layer ->
                            if layer.Name = active then
                                { layer with Image = Image.apply operation layer.Image }
                            else
                                layer) }
