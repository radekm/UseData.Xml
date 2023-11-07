module UseData.Xml.Benchmark.M7Message

open System

// M7 specification DFS180 - M7 - Public Message Interface can be found at
// https://www.semopx.com/documents/general-publications/DFS180-API-Specifications-M7-6.13-v1.0.pdf
let message = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<PblcOrdrBooksDeltaRprt xmlns="http://www.deutsche-boerse.com/m7/v6">
    <StandardHeader marketId="EPEX"/>
    <OrdrbookList>
        <OrdrBook contractId="1790055" dlvryAreaId="10YDE-EON------1" lastPx="8670" lastQty="4700" totalQty="452000" lastTradeTime="2022-11-11T23:39:36.351Z" pxDir="1" revisionNo="1973" highPx="8670" lowPx="4850">
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982385" qty="0" px="57" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1790096" dlvryAreaId="10YDE-EON------1" lastPx="5570" lastQty="3000" totalQty="564900" lastTradeTime="2022-11-12T08:34:09.252Z" pxDir="0" revisionNo="2064" highPx="8890" lowPx="4680">
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982401" qty="0" px="59" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1790073" dlvryAreaId="10YDE-EON------1" lastPx="7304" lastQty="1200" totalQty="571300" lastTradeTime="2022-11-12T08:31:25.094Z" pxDir="0" revisionNo="1660" highPx="11300" lowPx="4920">
            <SellOrdrList>
                <OrdrBookEntry ordrId="505982400" qty="0" px="30058" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </SellOrdrList>
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982399" qty="0" px="58" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1790139" dlvryAreaId="10YDE-EON------1" lastPx="5820" lastQty="1900" totalQty="490000" lastTradeTime="2022-11-12T08:35:34.727Z" pxDir="0" revisionNo="2294" highPx="11200" lowPx="3040">
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982387" qty="0" px="57" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1790198" dlvryAreaId="10YDE-EON------1" lastPx="7339" lastQty="3000" totalQty="571000" lastTradeTime="2022-11-11T23:10:25.999Z" pxDir="0" revisionNo="1539" highPx="10170" lowPx="4780">
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982391" qty="0" px="57" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1789926" dlvryAreaId="10YDE-EON------1" lastPx="6880" lastQty="3900" totalQty="478000" lastTradeTime="2022-11-11T23:39:34.928Z" pxDir="1" revisionNo="1931" highPx="10130" lowPx="3350">
            <SellOrdrList>
                <OrdrBookEntry ordrId="505982384" qty="0" px="30056" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </SellOrdrList>
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982383" qty="0" px="56" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1789885" dlvryAreaId="10YDE-EON------1" lastPx="6231" lastQty="19000" totalQty="517100" lastTradeTime="2022-11-12T09:14:29.074Z" pxDir="0" revisionNo="2201" highPx="11670" lowPx="2910">
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982395" qty="0" px="58" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1789944" dlvryAreaId="10YDE-EON------1" lastPx="6717" lastQty="100" totalQty="475100" lastTradeTime="2022-11-12T10:02:36.990Z" pxDir="0" revisionNo="1901" highPx="8120" lowPx="5300">
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982409" qty="0" px="60" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1790010" dlvryAreaId="10YDE-EON------1" lastPx="5806" lastQty="3400" totalQty="574800" lastTradeTime="2022-11-12T09:55:13.336Z" pxDir="0" revisionNo="1988" highPx="8990" lowPx="3990">
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982407" qty="0" px="59" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1789903" dlvryAreaId="10YDE-EON------1" lastPx="7390" lastQty="900" totalQty="544000" lastTradeTime="2022-11-12T08:25:52.932Z" pxDir="0" revisionNo="2061" highPx="8620" lowPx="3310">
            <SellOrdrList>
                <OrdrBookEntry ordrId="505982398" qty="0" px="30058" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </SellOrdrList>
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982397" qty="0" px="58" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1789987" dlvryAreaId="10YDE-EON------1" lastPx="6858" lastQty="3600" totalQty="613300" lastTradeTime="2022-11-12T09:55:15.341Z" pxDir="0" revisionNo="2049" highPx="10900" lowPx="2620">
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982403" qty="0" px="59" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1789838" dlvryAreaId="10YDE-EON------1" lastPx="7850" lastQty="100" totalQty="584400" lastTradeTime="2022-11-12T09:16:08.308Z" pxDir="-1" revisionNo="2009" highPx="10030" lowPx="1050">
            <SellOrdrList>
                <OrdrBookEntry ordrId="505982406" qty="0" px="30059" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </SellOrdrList>
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982405" qty="0" px="59" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1789772" dlvryAreaId="10YDE-EON------1" lastPx="7330" lastQty="2500" totalQty="561000" lastTradeTime="2022-11-11T23:39:33.082Z" pxDir="1" revisionNo="2310" highPx="8500" lowPx="5060">
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982389" qty="0" px="57" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
        <OrdrBook contractId="1790235" dlvryAreaId="10YDE-EON------1" lastPx="5789" lastQty="13500" totalQty="596000" lastTradeTime="2022-11-12T08:52:24.847Z" pxDir="0" revisionNo="1786" highPx="10760" lowPx="1600">
            <BuyOrdrList>
                <OrdrBookEntry ordrId="505982393" qty="0" px="58" ordrEntryTime="2022-11-12T10:02:54.553Z"/>
            </BuyOrdrList>
        </OrdrBook>
    </OrdrbookList>
</PblcOrdrBooksDeltaRprt>
"""

open UseData.Xml

let ignoreStandardHeader (root : Elem) =
    root |> Elem.child "StandardHeader" (Elem.attr "marketId" Parse.string) |> ignore

type Order =
    { OrderId : uint64
      Price : int64
      Quantity : uint
      EntryTime : DateTimeOffset
    }

let parseOrders (e : Elem) =
    e
    |> Elem.children "OrdrBookEntry" (fun e ->
        { OrderId = e |> Elem.attr "ordrId" Parse.uint64
          Price = e |> Elem.attr "px" Parse.int64
          Quantity = e |> Elem.attr "qty" Parse.uint
          EntryTime = e |> Elem.attr "ordrEntryTime" Parse.dateTimeOffset
        })

type OrderBookDelta =
    { ContractId : uint64
      AreaId : string
      Revision : uint
      Buy : Order[]
      Sell : Order[]
    }

let parseOrderBookDelta (root : Elem) =
    root |> ignoreStandardHeader
    root |> Elem.childOpt "OrdrbookList" (fun e ->
        e |> Elem.children "OrdrBook" (fun e ->
            e |> Elem.attrOpt "lastPx" Parse.int64 |> ignore
            e |> Elem.attrOpt "lastQty" Parse.uint |> ignore
            e |> Elem.attrOpt "totalQty" Parse.uint |> ignore
            e |> Elem.attrOpt "lastTradeTime" Parse.dateTimeOffset |> ignore
            e |> Elem.attrOpt "pxDir" Parse.int |> ignore
            e |> Elem.attrOpt "highPx" Parse.int64 |> ignore
            e |> Elem.attrOpt "lowPx" Parse.int64 |> ignore
            { ContractId = e |> Elem.attr "contractId" Parse.uint64
              AreaId = e |> Elem.attr "dlvryAreaId" Parse.stringNonWhitespace
              Revision = e |> Elem.attr "revisionNo" Parse.uint
              Buy = e |> Elem.childOpt "BuyOrdrList" parseOrders |> Option.defaultValue [||]
              Sell = e |> Elem.childOpt "SellOrdrList" parseOrders |> Option.defaultValue [||]
            }))
    |> Option.defaultValue [||]

let tracer = { new ITracer with
                 override _.OnUnused(which, attrs, children, text) =
                     if attrs.Length > 0 || children.Length > 0 || text.IsSome then
                         printfn $"Unused in %A{which}: attrs %A{attrs}, children %A{children}, text %A{text}"
             }

let parse () =
    use root = Elem.parseFromString tracer message
    parseOrderBookDelta root

// -------------------------------------------------------------------------------------------
// Experimental callback-based API

[<Sealed>]
type OrderBuilder() =
    let mutable orderId = 0UL
    let mutable orderIdInitialized = false

    let mutable price = 0L
    let mutable priceInitialized = false

    let mutable quantity = 0u
    let mutable quantityInitialized = false

    let mutable entryTime = DateTimeOffset.MaxValue
    let mutable entryTimeInitialized = false

    member _.OrderId
        with get () = orderId
        and set v =
            if orderIdInitialized then
               failwith "OrderId is already initialized"
            orderId <- v
            orderIdInitialized <- true

    member _.Price
        with get () = price
        and set v =
            if priceInitialized then
               failwith "Price is already initialized"
            price <- v
            priceInitialized <- true

    member _.Quantity
        with get () = quantity
        and set v =
            if quantityInitialized then
               failwith "Quantity is already initialized"
            quantity <- v
            quantityInitialized <- true

    member _.EntryTime
        with get () = entryTime
        and set v =
            if entryTimeInitialized then
               failwith "EntryTime is already initialized"
            entryTime <- v
            entryTimeInitialized <- true

    member _.Build() =
        if not orderIdInitialized then
            failwith "OrderId is not initialized"
        if not priceInitialized then
            failwith "Price is not initialized"
        if not quantityInitialized then
            failwith "Quantity is not initialized"
        if not entryTimeInitialized then
            failwith "EntryTime is not initialized"
        { OrderId = orderId
          Price = price
          Quantity = quantity
          EntryTime = entryTime }

[<Sealed>]
type OrderBookDeltaBuilder() =
    let mutable contractId = 0UL
    let mutable contractIdInitialized = false

    let mutable areaId = Unchecked.defaultof<string>
    let mutable areaIdInitialized = false

    let mutable revision = 0u
    let mutable revisionInitialized = false

    let buy = ResizeArray<Order>()
    let sell = ResizeArray<Order>()

    member _.ContractId
        with get () = contractId
        and set v =
            if contractIdInitialized then
               failwith "ContractId is already initialized"
            contractId <- v
            contractIdInitialized <- true

    member _.AreaId
        with get () = areaId
        and set v =
            if areaIdInitialized then
               failwith "AreaId is already initialized"
            areaId <- v
            areaIdInitialized <- true

    member _.Revision
        with get () = revision
        and set v =
            if revisionInitialized then
               failwith "Revision is already initialized"
            revision <- v
            revisionInitialized <- true

    member _.AddBuy(item : Order) =
        buy.Add(item)

    member _.AddSell(item : Order) =
        sell.Add(item)

    member _.Build() =
        if not contractIdInitialized then
            failwith "ContractId is not initialized"
        if not areaIdInitialized then
            failwith "AreaId is not initialized"
        if not revisionInitialized then
            failwith "Revision is not initialized"
        { ContractId = contractId
          AreaId = areaId
          Revision = revision
          Buy = buy.ToArray()
          Sell = sell.ToArray() }

let dummyWhich = Root ""

let handleUnusedAttr (n : string) (v : string) (c : ElemX.Cursor) =
    printfn "Element %A contains unused attribute %s=%s" c.Which n v

let handleUnusedElem (n : string) (c : ElemX.Cursor) =
    printfn "Element %A contains unused element %s" c.Which n

let handleUnusedText (text : string) (c : ElemX.Cursor) =
    if not (String.IsNullOrWhiteSpace text) then
        printfn "Element %A contains unused text" c.Which

let inline parseOrderX (c : ElemX.Cursor) =

    let b = OrderBuilder()
    c
    |> ElemX.parseElem
        (fun n v ->
            match n with
            | "ordrId" -> b.OrderId <- Parse.uint64 dummyWhich None v
            | "px" -> b.Price <- Parse.int64 dummyWhich None v
            | "qty" -> b.Quantity <- Parse.uint dummyWhich None v
            | "ordrEntryTime" -> b.EntryTime <- Parse.dateTimeOffset dummyWhich None v
            | _ -> handleUnusedAttr n v c)
        handleUnusedElem
        (fun text -> handleUnusedText text c)

    b.Build()

// NOTE: Even though this function doesn't check whether optional attributes and elements appear more than once
//       it's extremely complex when compared to `parse`.
let inline parseOrderBookDeltasX (c : ElemX.Cursor) =
    let result = ResizeArray()
    let mutable standardHeaderSeen = false

    c
    |> ElemX.parseElem
        (fun n v -> handleUnusedAttr n v c)
        (fun n c ->
            match n with
            | "StandardHeader" ->
                if standardHeaderSeen then
                    failwith "Duplicate StandardHeader"
                standardHeaderSeen <- true

                let mutable marketIdSeen = false

                c
                |> ElemX.parseElem
                    (fun n v ->
                        match n with
                        | "marketId" ->
                            if marketIdSeen then
                                failwith "Duplicate marketId"
                            marketIdSeen <- true
                        | _ -> handleUnusedAttr n v c)
                    handleUnusedElem
                    (fun text -> handleUnusedText text c)

                if not marketIdSeen then
                    failwith "Missing marketId"
            | "OrdrbookList" ->
                c
                |> ElemX.parseElem
                    (fun n v -> handleUnusedAttr n v c)
                    (fun n c ->
                        match n with
                        | "OrdrBook" ->
                            let b = OrderBookDeltaBuilder()

                            c
                            |> ElemX.parseElem
                                (fun n v ->
                                    match n with
                                    | "lastPx" -> Parse.int64 dummyWhich None v |> ignore
                                    | "lastQty" -> Parse.uint dummyWhich None v |> ignore
                                    | "totalQty" -> Parse.uint dummyWhich None v |> ignore
                                    | "lastTradeTime" -> Parse.dateTimeOffset dummyWhich None v |> ignore
                                    | "pxDir" -> Parse.int dummyWhich None v |> ignore
                                    | "highPx" -> Parse.int64 dummyWhich None v |> ignore
                                    | "lowPx" -> Parse.int64 dummyWhich None v |> ignore
                                    | "contractId" -> b.ContractId <- Parse.uint64 dummyWhich None v
                                    | "dlvryAreaId" -> b.AreaId <- Parse.stringNonWhitespace dummyWhich None v
                                    | "revisionNo" -> b.Revision <- Parse.uint dummyWhich None v
                                    | _ -> handleUnusedAttr n v c)
                                (fun n c ->
                                    match n with
                                    | "BuyOrdrList" ->
                                        c
                                        |> ElemX.parseElem
                                            (fun n v -> handleUnusedAttr n v c)
                                            (fun n c ->
                                                match n with
                                                | "OrdrBookEntry" -> b.AddBuy(parseOrderX c)
                                                | _ -> handleUnusedElem n c)
                                            (fun text -> handleUnusedText text c)
                                    | "SellOrdrList" ->
                                        c
                                        |> ElemX.parseElem
                                            (fun n v -> handleUnusedAttr n v c)
                                            (fun n c ->
                                                match n with
                                                | "OrdrBookEntry" -> b.AddSell(parseOrderX c)
                                                | _ -> handleUnusedElem n c)
                                            (fun text -> handleUnusedText text c)
                                    | _ -> handleUnusedElem n c)
                                (fun text -> handleUnusedText text c)

                            result.Add(b.Build())
                        | _ -> handleUnusedElem n c)
                    (fun text -> handleUnusedText text c)
            | _ -> handleUnusedElem n c)
        (fun text -> handleUnusedText text c)

    if not standardHeaderSeen then
        failwith "Missing StandardHeader"

    result

let parseByExperimentalCallbackApi () =
    let mutable result = [||]
    ElemX.parseFromString
        (fun _ c -> result <- (parseOrderBookDeltasX c).ToArray())
        message
    result

// -------------------------------------------------------------------------------------------
// XmlReader

open System.IO
open System.Xml

// To get only performance of functions in `UseData.Xml` we need to subtract time and memory
// used by `XmlReader` which are shown by `readByXmlReader`.
let readByXmlReader () =
    let mutable i = 0

    let settings = XmlReaderSettings()
    settings.IgnoreComments <- true
    settings.IgnoreProcessingInstructions <- true
    use stringReader = new StringReader(message)
    use reader = XmlReader.Create(stringReader, settings)
    while reader.Read() do
        match reader.NodeType with
        | XmlNodeType.Element ->
            i <- i + reader.Prefix.Length + reader.LocalName.Length
            if reader.IsEmptyElement then
                i <- i + 1000
            while reader.MoveToNextAttribute() do
                i <- i + reader.Prefix.Length + reader.LocalName.Length + reader.Value.Length
        | XmlNodeType.EndElement -> i <- i - reader.Prefix.Length - reader.LocalName.Length
        | XmlNodeType.Text -> i <- i + reader.Value.Length
        | XmlNodeType.Whitespace -> i <- i + reader.Value.Length
        | XmlNodeType.CDATA -> i <- i + reader.Value.Length
        | XmlNodeType.XmlDeclaration -> ()
        | t -> failwith $"Unsupported node type %A{t}"

    i
