namespace UseData.Xml

open System
open System.IO
open System.Xml

exception WrongCount of which:WhichElem * msg:string
    with
        override me.Message = $"Error when parsing %A{me.which}: %s{me.msg}"

/// Dictionary searches for keys sequentially.
/// Designed for small number of key-value pairs.
/// Each key-value pair from dictionary can be used (retrieved) at most once.
/// Values are replaced by null after first use so when someone tries
/// to use key-value pair again exception is raised.
[<Sealed>]
type internal DictionaryWithUsage<'T when 'T : not struct>() =
    let mutable capacity = 32
    let mutable length = 0
    let mutable hashes : int[] = Array.zeroCreate capacity
    let mutable keys : string[] = Array.zeroCreate capacity
    let mutable values : 'T[] = Array.zeroCreate capacity
    let mutable unusedCount = 0

    /// Searches first by `hash` then by comparing `key`.
    let indexOf (hash : int) (key : string) =
        let rec find from =
            if from < length then
                match hashes.AsSpan().Slice(from, length - from).IndexOf(hash) with
                | -1 -> -1
                | i when keys[from + i] = key -> from + i
                // Different key had same hash.
                | i -> find (from + i + 1)
            else -1
        find 0

    member _.Enlarge(orig : byref<'U[]>) =
        let bigger = Array.zeroCreate capacity
        orig.AsSpan().Slice(0, length).CopyTo(bigger.AsSpan().Slice(0, length))
        orig <- bigger

    member me.EnsureCapacity() =
        if length = capacity then
            capacity <- 2 * capacity

            me.Enlarge(&keys)
            me.Enlarge(&hashes)
            me.Enlarge(&values)

    /// Both `create` and `update` are allowed to raise exception.
    member inline me.AddOrUpdate( key : string,
                                  [<InlineIfLambda>] create : unit -> 'T,
                                  [<InlineIfLambda>] update : 'T -> unit ) : 'T =

        let hash = key.GetHashCode()
        match indexOf hash key with
        | -1 ->
            let value = create ()
            me.EnsureCapacity()
            hashes[length] <- hash
            keys[length] <- key
            values[length] <- value
            length <- length + 1
            unusedCount <- unusedCount + 1
            value
        | index ->
            let value = values[index]
            update value
            value

    member inline me.Use(key : string, [<InlineIfLambda>] errorIfUsed : unit -> string) : ValueOption<'T> =
        let hash = key.GetHashCode()
        match indexOf hash key with
        | -1 ->
            me.EnsureCapacity()
            hashes[length] <- hash
            keys[length] <- key
            values[length] <- Unchecked.defaultof<_>  // Used values are replaced by `null`.
            length <- length + 1
            ValueNone
        | index ->
            let value = values[index]
            if isNull (value :> obj)
            then failwith (errorIfUsed ())
            else
                values[index] <- Unchecked.defaultof<_>
                unusedCount <- unusedCount - 1
                ValueSome value

    member _.MarkRemainingAsUsed() =
        if unusedCount > 0 then
            for i = 0 to length - 1 do
                values[i] <- Unchecked.defaultof<_>
            unusedCount <- 0

    member _.UnusedCount = unusedCount

    member _.ToArrayWithUnused() =
        let mutable dest = 0
        let result = Array.zeroCreate unusedCount
        for i = 0 to length - 1 do
            let value = values[i]
            if not (isNull (value :> obj)) then
                result[dest] <- keys[i], value
                dest <- dest + 1
        result

[<Sealed>]
type Elem internal ( which : WhichElem,
                     tracer : ITracer,
                     attrs : DictionaryWithUsage<string>,
                     // Note: It would be nicer to have `unusedChildren : Dictionary<string, Elem[]>`
                     // but then `parse` would need to do additional conversions and allocations
                     // which we avoided by using directly data structure from `parse`.
                     // One disadvantage is that `Elem[]` uses less memory
                     // than `{| Prefix : string; Elems : ResizeArray<Elem> |}`.
                     children : DictionaryWithUsage<{| Prefix : string; Elems : ResizeArray<Elem> |}>,
                     text : string,
                     textShallBeUsed : bool ) =
    let mutable disposed = false

    member val private Attrs = attrs
    member private _.Children = children
    member private _.Text = text
    member val private UsedText = false
        with get, set

    member _.Name = which.Name
    member _.Which = which

    member private _.Check() = if disposed then raise <| ObjectDisposedException $"Elem %A{which}"

    interface IDisposable with
        override me.Dispose() =
            if not disposed then
                disposed <- true

                // Only if the text is non-whitespace.
                let unusedText = if me.UsedText || not textShallBeUsed then None else Some me.Text

                if attrs.UnusedCount > 0 || children.UnusedCount > 0 || unusedText.IsSome then
                    let unusedChildren = children.ToArrayWithUnused() |> Array.map fst
                    tracer.OnUnused(which, attrs.ToArrayWithUnused(), unusedChildren, unusedText)

    static member private AttrHelper(name : string, p : StringParser<'T>, elem : Elem) : 'T option =
        elem.Check()

        match elem.Attrs.Use(name, fun () -> $"Attribute %s{name} in elem %A{elem.Which} already used") with
        | ValueNone -> None
        | ValueSome value -> Some (p elem.Which (Some name) value)

    static member attrOpt (name : string) (p : StringParser<'T>) (elem : Elem) : 'T option =
        let parsed = Elem.AttrHelper(name, p, elem)
        parsed

    static member attr (name : string) (p : StringParser<'T>) (elem : Elem) : 'T =
        match Elem.AttrHelper(name, p, elem) with
        | None -> raise <| WrongCount (elem.Which, $"Expected one attribute %s{name}")
        | Some parsed -> parsed

    static member private ChildHelper(name : string, p : Elem -> 'T, elem : Elem) : 'T[] =
        elem.Check()

        match elem.Children.Use(name, fun () -> $"Children %s{name} in elem %A{elem.Which} already used") with
        | ValueNone -> Array.empty
        | ValueSome children ->
            Array.init children.Elems.Count (fun i ->
                use elem = children.Elems[i]
                p elem)

    static member children (name : string) (p : Elem -> 'T) (elem : Elem) : 'T[] =
        let parsed = Elem.ChildHelper(name, p, elem)
        parsed

    static member childOpt (name : string) (p : Elem -> 'T) (elem : Elem) : 'T option =
        let parsed = Elem.ChildHelper(name, p, elem)
        let result =
            match parsed with
            | [||] -> None
            | [| x |] -> Some x
            | _ -> raise <| WrongCount (elem.Which, $"Expected at most one child %s{name}")
        result

    static member child (name : string) (p : Elem -> 'T) (elem : Elem) : 'T =
        let parsed = Elem.ChildHelper(name, p, elem)
        let result =
            match parsed with
            | [| x |] -> x
            | _ -> raise <| WrongCount (elem.Which, $"Expected one child %s{name}")
        result

    static member private TextHelper(p : StringParser<'T>, elem : Elem) : 'T =
        elem.Check()

        if elem.UsedText then
            failwithf $"Text in elem %A{elem.Which} already used"
        elem.UsedText <- true

        p elem.Which None elem.Text

    static member text (p : StringParser<'T>) (elem : Elem) : 'T =
        let parsed = Elem.TextHelper(p, elem)
        parsed

    static member ignoreAll (elem : Elem) =
        elem.Check()

        elem.Attrs.MarkRemainingAsUsed()
        elem.Children.MarkRemainingAsUsed()
        elem.UsedText <- true

and ITracer =
    abstract member OnUnused :
        which:WhichElem *
        attrs:(string * string)[] *
        children:string[] *
        text:option<string> -> unit

module Elem =
    /// `reader` must ignore comments and processing instructions.
    /// Prefixes and namespaces are ignored.
    // Assumes that `reader` checks that root element exists
    // and that elements are properly nested.
    let parse (tracer : ITracer) (reader : XmlReader) =
        // We can share one `StringBuilder` because mixed content is not allowed.
        let text = System.Text.StringBuilder()

        // Expected initial state: Opening tag with attributes was processed.
        // Final state: `reader.NodeType` is `XmlNodeType.EndElement`.
        let rec parseContent (which : WhichElem) =
            let children = DictionaryWithUsage<{| Prefix : string; Elems : ResizeArray<Elem> |}>()
            let mutable significantText = false  // We found non-whitespace text or CDATA.

            let inline processCData (value : string) =
                if children.UnusedCount <> 0 then
                    failwith $"Element %A{which} contains mixed content"
                significantText <- true
                text.Append(value) |> ignore

            let inline processWhitespace (value : string) =
                // Ignore whitespace around child elements.
                // Otherwise we save whitespace
                // because it could be part of text
                // (if there are no child elements we return text even if it contains only whitespace).
                if children.UnusedCount = 0 then
                    text.Append(value) |> ignore

            // We assume that if we reach EOF here, the exception will be thrown
            // (meaning `reader.Read()` here doesn't return false).
            // The reason is that we must first reach closing element before EOF.
            while reader.Read() && reader.NodeType <> XmlNodeType.EndElement do
                match reader.NodeType with
                | XmlNodeType.Element ->
                    if significantText then
                        failwith $"Element %A{which} contains mixed content"
                    // Since mixed content is not allowed and we found child element then we won't have text.
                    // Thus `text` `StringBuilder` won't be used in this element anymore
                    // and can be reused in child elements.
                    text.Clear() |> ignore

                    let name = reader.LocalName
                    let prefix = reader.Prefix
                    let children =
                        children.AddOrUpdate(
                            name,
                            (fun () -> {| Prefix = prefix; Elems = ResizeArray() |}),
                            (fun children ->
                                if children.Prefix <> prefix then
                                    failwithf "%s %s %s"
                                        $"Element %A{which} contains multiple child elements "
                                        $"with same local name %s{name} "
                                        $"but different prefixes %s{children.Prefix} and %s{prefix}"))
                    let which = Child (which, name, children.Elems.Count)
                    let elem = parseElement which
                    children.Elems.Add elem
                | XmlNodeType.Text ->
                    let value = reader.Value
                    if String.IsNullOrWhiteSpace value
                    then processWhitespace value
                    else processCData value
                | XmlNodeType.Whitespace -> processWhitespace reader.Value
                | XmlNodeType.CDATA -> processCData reader.Value
                | nodeType -> failwith $"Element %A{which} contains unexpected node: %A{nodeType}"
            struct {| Children = children
                      Text =
                          if children.UnusedCount = 0
                          then
                              let result = text.ToString()
                              text.Clear() |> ignore
                              result
                          else ""
                      SignificantText = significantText
                   |}
        // Expected initial state: `reader.NodeType` is `XmlNodeType.Element`.
        // Final state: Element was processed. If it was empty element (`reader.IsEmptyElement`)
        //              then opening tag with attributes was processed.
        //              If it was non-empty element then `reader.NodeType` is `XmlNodeType.EndElement`.
        and parseElement (which : WhichElem) =
            let isEmptyElement = reader.IsEmptyElement  // Calling this after reading attributes doesn't work.
            let attributes = DictionaryWithUsage<string>()
            while reader.MoveToNextAttribute() do
                let name = reader.LocalName
                if reader.Prefix <> "xmlns" && name <> "xmlns" then
                    let value = reader.Value
                    attributes.AddOrUpdate(
                        name,
                        (fun () -> value),
                        (fun _ ->
                            failwithf "%s %s"
                                $"Element %A{which} contains multiple attributes "
                                $"with same local name %s{name}"))
                    |> ignore
            if isEmptyElement then
                // Element without content.
                new Elem(which, tracer, attributes, DictionaryWithUsage(), "", false)
            else
                let content = parseContent which
                new Elem(which, tracer, attributes, content.Children, content.Text, content.SignificantText)

        // Skip nodes until root element is found.
        while reader.Read() && reader.NodeType <> XmlNodeType.Element do
            match reader.NodeType with
            | XmlNodeType.Whitespace
            | XmlNodeType.XmlDeclaration -> ()
            | nodeType -> failwithf "Unexpected node before root element: %A" nodeType

        let root = parseElement (Root reader.LocalName)

        // Check nodes after root element.
        while reader.Read() do
            match reader.NodeType with
            | XmlNodeType.Whitespace -> ()
            | nodeType -> failwithf "Unexpected node after root element: %A" nodeType

        root

    let parseFromTextReader (tracer : ITracer) (reader : TextReader) =
        let settings = XmlReaderSettings()
        settings.IgnoreComments <- true
        settings.IgnoreProcessingInstructions <- true
        use reader = XmlReader.Create(reader, settings)
        parse tracer reader

    let parseFromString (tracer : ITracer) (str : string) =
        use reader = new StringReader(str)
        parseFromTextReader tracer reader
