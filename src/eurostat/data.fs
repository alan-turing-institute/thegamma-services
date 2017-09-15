namespace Eurostat
open System
open System.IO
open FSharp.Data
open System.Collections.Generic
open Dictionary

//http://ec.europa.eu/eurostat/estat-navtree-portlet-prod/BulkDownloadListing?sort=1&file=data%2Faact_ali01.tsv.gz
//http://ec.europa.eu/eurostat/estat-navtree-portlet-prod/BulkDownloadListing?sort=1&downfile=data%2Faact_ali01.tsv.gz
//http://ec.europa.eu/eurostat/estat-navtree-portlet-prod/BulkDownloadListing?sort=1&file=data%2Frd_p_persocc.tsv.gz

module Datasets =
  let urlRoot = "http://ec.europa.eu/eurostat/estat-navtree-portlet-prod/BulkDownloadListing?sort=1&file=data%2F"
  
  let dictionaries = getEurostatDictionaries
  let readHeaders (firstLine:string) = 
    let headersEnd = firstLine.IndexOf("\\time")
    let headers = firstLine.[0..headersEnd-1].Split [|','|] 
    let years = firstLine.[headersEnd+6..].Split [|'\t'|] |> Seq.map (fun year -> year.Replace(" ","")) 
    (headers, years)

  let readRow (headers: string [], aRow: string) = 
    let keysEnd = aRow.IndexOf("\t")
    let keys = aRow.[0..keysEnd-1].Split[|','|]
    let parsedKeys = keys |> Seq.mapi (fun i key -> getValue dictionaries headers.[i] key)
    let values = aRow.[keysEnd+1..].Split[|'\t'|]
    let parsedValues = values |> Seq.map (fun value -> if value.Contains(":") then "0" else value.Replace(" ",""))
    let row = (Seq.append parsedKeys parsedValues)
    let r = String.concat "," row
    r
      
  let readRows (headers: string [], contents: seq<string>) = 
    let parsedRows = contents |> Seq.map(fun row -> readRow(headers, row)) |> Seq.toArray
    parsedRows

  let readFile fileName = 
    let contents = System.IO.File.ReadAllLines(fileName)

    let (headers,years) = readHeaders (Seq.head contents)
    let parsedHeaders = String.concat "," headers 
    let parsedYears = String.concat "," years 
    let parsedFirstRow = sprintf "%s,%s" parsedHeaders parsedYears

    let parsedRows = readRows (headers, (Seq.tail contents))
    let dataset = Array.append [|parsedFirstRow|] parsedRows
    dataset
  
  let writeFile dataset outputFile= 
    File.WriteAllLines(outputFile, dataset) 
      