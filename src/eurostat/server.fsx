#if INTERACTIVE
#I "../../packages"
#r "System.Xml.Linq.dll"
#r "FSharp.Data/lib/net40/FSharp.Data.dll"
#r "Newtonsoft.Json/lib/net40/Newtonsoft.Json.dll"
#r "Suave/lib/net40/Suave.dll"
#load "../serializer.fs"
#load "vocab.fs" "request.fs" "dictionary.fs" "data.fs" "tree.fs" "eurostat-sci-domain.fs"
#else
module Services.Eurostat
#endif
#nowarn "1104"
open System
open System.IO
open FSharp.Data
open System.Collections.Generic
open Eurostat
open Eurostat.Domain
open Services.Serializer

let eurostatScience = Domain.getTree

open Suave
open Suave.Filters
open Suave.Operators

type ThingSchema = { ``@context``:string; ``@type``:string; name:string; }
type GenericType = { name:string; ``params``:obj[] }
type TypePrimitive = { kind:string; ``type``:obj; endpoint:string }
type TypeNested = { kind:string; endpoint:string }
type Member = { name:string; returns:obj; trace:string[]; schema:ThingSchema }

let noSchema = Unchecked.defaultof<ThingSchema>
let makeSchemaThing kind name =
  { ``@context`` = "http://schema.org/"; ``@type`` = kind; name = name }
let makeSchemaExt kind name =
  { ``@context`` = "http://thegamma.net/eurostat"; ``@type`` = kind; name = name }

let memberPath s f = 
  path s >=> request (fun _ -> f() |> Array.ofSeq |> toJson |> Successful.OK)

let memberPathf fmt f = 
  pathScan fmt (fun b -> f b |> Array.ofSeq |> toJson |> Successful.OK)

let (|Lookup|_|) k (dict:IDictionary<_,_>) =
  match dict.TryGetValue k with
  | true, v -> Some v
  | _ -> None

let app =
  // printfn "%A" eurostatScience
  choose [ 
    // memberPath "/" (fun () ->
    //   [ { name="science"; returns= {kind="nested"; endpoint="/pickModule"}
    //       trace=[| |]; schema = noSchema } 
    //   ])
    // memberPath "/pickModule" (fun () ->
    //   let rootFolderData = "scitech" 
    //   let (scienceModules, scienceDatasets) = Domain.getChildren(eurostatScience,rootFolderData)
    //   [ for (theme,code) in scienceModules ->
    //       let endpointUri = sprintf "/%s/pickModule" code
    //       { name=theme; returns={kind="nested"; endpoint=endpointUri}
    //         trace=[| |]; schema = noSchema } ])

    memberPath "/" (fun () ->
      let rootFolderData = "scitech" 
      let (scienceModules, scienceDatasets) = Domain.getChildren(eurostatScience,rootFolderData)
      [ for (theme,code) in scienceModules ->
          let endpointUri = sprintf "/%s/pickModule" code
          { name=theme; returns={kind="nested"; endpoint=endpointUri}
            trace=[| |]; schema = noSchema } ])

    memberPathf "/%s/pickModule" (fun rootFolderData ->
      let (scienceModules, sciencDatasets) = Domain.getChildren(eurostatScience,rootFolderData)
      let modulesList = [ for (theme,code) in scienceModules ->
                            let endpointUri = sprintf "/%s/pickModule" code
                            { name=theme; returns={kind="nested"; endpoint=endpointUri}
                              trace=[| |]; schema = noSchema } ]
      let datasetsList = [ for (theme,code) in sciencDatasets ->
                            { name=theme; returns={kind="nested"; endpoint="/data"}
                              trace=[|"data=" + code|]; schema = noSchema } ]
      modulesList @ datasetsList)
        
  ]
