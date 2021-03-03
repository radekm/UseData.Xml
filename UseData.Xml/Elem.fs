namespace UseData.Xml

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Xml.Linq

exception WrongCount of which:WhichElem * msg:string

[<Sealed>]
type Elem private (which : WhichElem, tracer : ITracer, xElem : XElement) =
    let attrs' =
        xElem.Attributes()
        |> List.ofSeq
        |> List.groupBy (fun attr -> attr.Name.LocalName)
        |> List.map (function
            | _name, [] -> failwith "Absurd"
            | name, [attr] -> name, attr
            | name, attrs ->
                failwithf "%s %s"
                    $"Element %A{which} contains multiple attributes "
                    $"with same local name %s{name}: %A{attrs}")
        |> Map.ofList
    // We need the prime to ensure the name differs from the static method.
    let children' =
        xElem.Elements()
        |> List.ofSeq
        |> List.groupBy (fun el -> el.Name.LocalName)
        |> Map.ofList
    let text' =
        let text =            
            xElem.Nodes()
            |> Seq.map (function
                | :? XText as node -> node.Value
                | _ -> "")
            |> String.concat ""
        if not children'.IsEmpty then
            if text.Trim().Length = 0
            then ""
            else failwith $"Element %A{which} contains mixed content"
        else text
    let mutable disposed = false

    member _.Name = which.Name
    member _.Which = which
    member _.Attrs = attrs'
    member _.Children = children'
    member _.Text = text'
    member private _.Tracer = tracer

    member val private UsedAttrs = HashSet<string>()
        with get
    member val private UsedChildren = HashSet<string>()
        with get
    member val private UsedText = false
        with get, set
        
    member private _.Check() = if disposed then raise <| ObjectDisposedException $"Elem %A{which}"

    interface IDisposable with
        override me.Dispose() =
            if not disposed then
                disposed <- true

            let removeKeysFromMap (keys : HashSet<'K>) map : Map<'K, 'V> =
                map |> Map.filter (fun k _ ->  keys.Contains k |> not)
                
            let unusedAttrs = removeKeysFromMap me.UsedAttrs me.Attrs
            let unusedChildren = removeKeysFromMap me.UsedChildren me.Children
            // Only if the text is non-whitespace.
            let unusedText = if me.UsedText || me.Text.Trim().Length = 0 then None else Some me.Text
            if not (unusedAttrs.IsEmpty && unusedChildren.IsEmpty && unusedText.IsNone) then
                tracer.OnUnused(which, unusedAttrs, unusedChildren, unusedText)
    
    static member make (tracer : ITracer) (xElem : XElement) = new Elem(WhichElem.make xElem, tracer, xElem)
    
    static member private attrHelper (name : string) (p : StringParser<'T>) (elem : Elem) : 'T option =
        elem.Check()
        
        if elem.UsedAttrs.Contains name then
            failwithf $"Attribute %s{name} in elem %A{elem.Which} already used"
        elem.UsedAttrs.Add name |> ignore

        elem.Attrs
        |> Map.tryFind name
        |> Option.map (fun attr -> p elem.Which (Some name) attr.Value)        
        
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
            parsedValues = Option.toList parsed)
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
                parsedValues = [parsed])
            parsed

    static member private childHelper (name : string) (p : Elem -> 'T) (elem : Elem) : 'T list =
        elem.Check()

        if elem.UsedChildren.Contains name then
            failwithf $"Children %s{name} in elem %A{elem.Which} already used"
        elem.UsedChildren.Add name |> ignore

        elem.Children
        |> Map.tryFind name
        |> Option.defaultValue []
        |> List.mapi (fun i xElem ->
            use elem = new Elem(Child (elem.Which, name, i), elem.Tracer, xElem)
            p elem)

    static member children
        ( name : string,
          [<CallerFilePath; Optional; DefaultParameterValue("")>] file : string,
          [<CallerLineNumber; Optional; DefaultParameterValue(0)>] line : int
        ) : (Elem -> 'T) -> Elem -> 'T list = fun p elem ->
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
            | [] -> None
            | [x] -> Some x
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
            | [x] -> x
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
            parsedValues = [parsed])
        parsed

    static member ignoreAll (elem : Elem) =
        elem.Check()
        
        elem.Attrs |> Seq.iter (fun kv -> elem.UsedAttrs.Add kv.Key |> ignore)
        elem.Children |> Seq.iter (fun kv -> elem.UsedChildren.Add kv.Key |> ignore)
        elem.UsedText <- true
