namespace UseData.Xml

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Xml.Linq

exception WrongCount of which:WhichElem * msg:string
    with
        override me.Message = $"Error when parsing %A{me.which}: %s{me.msg}"

[<Sealed>]
type Elem private (which : WhichElem, tracer : ITracer, xElem : XElement) =
    let unusedAttrs = Dictionary<string, string>()    
    do for attr in xElem.Attributes() do
        if unusedAttrs.TryAdd(attr.Name.LocalName, attr.Value) |> not then
            failwithf "%s %s"
                $"Element %A{which} contains multiple attributes "
                $"with same local name %s{attr.Name.LocalName}: %A{xElem}"

    let unusedChildren = Dictionary<string, ResizeArray<XElement>>()
    do for el in xElem.Elements() do
        let found, elems = unusedChildren.TryGetValue(el.Name.LocalName)
        if found then
            if elems[0].Name = el.Name then
                elems.Add el
            else
                failwithf
                    $"Element %A{which} contains multiple child elements "
                    $"with same local name %s{el.Name.LocalName}: %A{xElem}"
        else
            let elems = ResizeArray()
            elems.Add el
            unusedChildren.Add(el.Name.LocalName, elems)

    // Prime is to avoid conflicts with a function `text`. 
    let text' =
        if unusedChildren.Count = 0 then
            xElem.Nodes()
            |> Seq.map (function
                | :? XText as node -> node.Value
                | _ -> "")
            |> String.concat ""
        else
            for node in xElem.Nodes() do
                match node with
                | :? XText as node when not (String.IsNullOrWhiteSpace node.Value) ->
                    failwith $"Element %A{which} contains mixed content"
                | _ -> ()
            ""

    let mutable disposed = false

    member _.Name = which.Name
    member _.Which = which
    member internal _.UnusedAttrs = unusedAttrs
    member internal _.UnusedChildren = unusedChildren
    member internal _.Text = text'
    member private _.Tracer = tracer

    member val private UsedAttrs = HashSet<string>(unusedAttrs.Count)
        with get
    member val private UsedChildren = HashSet<string>(unusedChildren.Count)
        with get
    member val private UsedText = false
        with get, set

    member private _.Check() = if disposed then raise <| ObjectDisposedException $"Elem %A{which}"

    interface IDisposable with
        override me.Dispose() =
            if not disposed then
                disposed <- true

                // Only if the text is non-whitespace.
                let unusedText = if me.UsedText || String.IsNullOrWhiteSpace me.Text then None else Some me.Text

                if unusedAttrs.Count > 0 || unusedChildren.Count > 0 || unusedText.IsSome then
                    tracer.OnUnused(which, unusedAttrs, unusedChildren, unusedText)

    static member make (tracer : ITracer) (xElem : XElement) = new Elem(WhichElem.make xElem, tracer, xElem)

    static member private attrHelper (name : string) (p : StringParser<'T>) (elem : Elem) : 'T option =
        elem.Check()

        if elem.UsedAttrs.Add name |> not then
            failwithf $"Attribute %s{name} in elem %A{elem.Which} already used"

        let found, value = elem.UnusedAttrs.Remove name
        if found
        then Some (p elem.Which (Some name) value)
        else None

    static member attrOpt
        ( name : string,
          [<CallerFilePath; Optional; DefaultParameterValue("")>] file : string,
          [<CallerLineNumber; Optional; DefaultParameterValue(0)>] line : int
        ) : StringParser<'T> -> Elem -> 'T option = fun p elem ->
        let parsed = elem |> Elem.attrHelper name p
        elem.Tracer.OnParsed(
            calledFunc = nameof Elem.attrOpt,
            callerFile = file,
            callerLine = line,
            which = elem.Which,
            selector = Some name,
            parsedValues = Option.toArray parsed)
        parsed

    static member attr
        ( name : string,
          [<CallerFilePath; Optional; DefaultParameterValue("")>] file : string,
          [<CallerLineNumber; Optional; DefaultParameterValue(0)>] line : int
        ) : StringParser<'T> -> Elem -> 'T = fun p elem ->
        match elem |> Elem.attrHelper name p with
        | None -> raise <| WrongCount (elem.Which, $"Expected one attribute %s{name}")
        | Some parsed ->
            elem.Tracer.OnParsed(
                calledFunc = nameof Elem.attr,
                callerFile = file,
                callerLine = line,
                which = elem.Which,
                selector = Some name,
                parsedValues = [| parsed |])
            parsed

    static member private childHelper (name : string) (p : Elem -> 'T) (elem : Elem) : 'T[] =
        elem.Check()

        if elem.UsedChildren.Add name |> not then
            failwithf $"Children %s{name} in elem %A{elem.Which} already used"

        let found, children = elem.UnusedChildren.Remove name
        if found then
            Array.init children.Count (fun i ->
                use elem = new Elem(Child (elem.Which, name, i), elem.Tracer, children[i])
                p elem)
        else Array.empty

    static member children
        ( name : string,
          [<CallerFilePath; Optional; DefaultParameterValue("")>] file : string,
          [<CallerLineNumber; Optional; DefaultParameterValue(0)>] line : int
        ) : (Elem -> 'T) -> Elem -> 'T[] = fun p elem ->
        let parsed = elem |> Elem.childHelper name p
        elem.Tracer.OnParsed(
            calledFunc = nameof Elem.children,
            callerFile = file,
            callerLine = line,
            which = elem.Which,
            selector = Some name,
            parsedValues = parsed)
        parsed

    static member childOpt
        ( name : string,
          [<CallerFilePath; Optional; DefaultParameterValue("")>] file : string,
          [<CallerLineNumber; Optional; DefaultParameterValue(0)>] line : int
        ) : (Elem -> 'T) -> Elem -> 'T option = fun p elem ->
        let parsed = elem |> Elem.childHelper name p
        let result =
            match parsed with
            | [||] -> None
            | [| x |] -> Some x
            | _ -> raise <| WrongCount (elem.Which, $"Expected at most one child %s{name}")
        elem.Tracer.OnParsed(
            calledFunc = nameof Elem.childOpt,
            callerFile = file,
            callerLine = line,
            which = elem.Which,
            selector = Some name,
            parsedValues = parsed)
        result

    static member child
        ( name : string,
          [<CallerFilePath; Optional; DefaultParameterValue("")>] file : string,
          [<CallerLineNumber; Optional; DefaultParameterValue(0)>] line : int
        ) : (Elem -> 'T) -> Elem -> 'T = fun p elem ->
        let parsed = elem |> Elem.childHelper name p
        let result =
            match parsed with
            | [| x |] -> x
            | _ -> raise <| WrongCount (elem.Which, $"Expected one child %s{name}")
        elem.Tracer.OnParsed(
            calledFunc = nameof Elem.child,
            callerFile = file,
            callerLine = line,
            which = elem.Which,
            selector = Some name,
            parsedValues = parsed)
        result

    static member private textHelper (p : StringParser<'T>) (elem : Elem) : 'T =
        elem.Check()

        if elem.UsedText then
            failwithf $"Text in elem %A{elem.Which} already used"
        elem.UsedText <- true

        p elem.Which None elem.Text

    static member text
        ( p : StringParser<'T>,
          [<CallerFilePath; Optional; DefaultParameterValue("")>] file : string,
          [<CallerLineNumber; Optional; DefaultParameterValue(0)>] line : int
        ) : Elem -> 'T = fun elem ->
        let parsed = elem |> Elem.textHelper p
        elem.Tracer.OnParsed(
            calledFunc = nameof Elem.text,
            callerFile = file,
            callerLine = line,
            which = elem.Which,
            selector = None,
            parsedValues = [| parsed |])
        parsed

    static member ignoreAll (elem : Elem) =
        elem.Check()

        elem.UnusedAttrs |> Seq.iter (fun kv -> elem.UsedAttrs.Add kv.Key |> ignore)
        elem.UnusedAttrs.Clear()
        elem.UnusedChildren |> Seq.iter (fun kv -> elem.UsedChildren.Add kv.Key |> ignore)
        elem.UnusedChildren.Clear()
        elem.UsedText <- true
