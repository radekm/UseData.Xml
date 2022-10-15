namespace UseData.Xml

open System
open System.Collections.Generic
open System.IO
open System.Xml

exception WrongCount of which:WhichElem * msg:string
    with
        override me.Message = $"Error when parsing %A{me.which}: %s{me.msg}"

[<Sealed>]
type Elem internal ( which : WhichElem,
                     tracer : ITracer,
                     unusedAttrs : Dictionary<string, string>,
                     // Note: It would be nicer to have `unusedChildren : Dictionary<string, Elem[]>`
                     // but then `parse` would need to do additional conversions and allocations
                     // which we avoided by using directly data structure from `parse`.
                     // One disadvantage is that `Elem[]` uses less memory
                     // than `{| Prefix : string; Elems : ResizeArray<Elem> |}`.
                     unusedChildren : Dictionary<string, {| Prefix : string; Elems : ResizeArray<Elem> |}>,
                     text : string,
                     textShallBeUsed : bool ) =
    let mutable disposed = false

    member val private UsedAttrs = HashSet<string>(unusedAttrs.Count)
        with get
    member val private UsedChildren = HashSet<string>(unusedChildren.Count)
        with get
    member val private UsedText = false
        with get, set

    member _.Name = which.Name
    member _.Which = which
    member private _.UnusedAttrs = unusedAttrs
    member private _.UnusedChildren = unusedChildren
    member private _.Text = text

    member private _.Check() = if disposed then raise <| ObjectDisposedException $"Elem %A{which}"

    interface IDisposable with
        override me.Dispose() =
            if not disposed then
                disposed <- true

                // Only if the text is non-whitespace.
                let unusedText = if me.UsedText || not textShallBeUsed then None else Some me.Text

                if unusedAttrs.Count > 0 || unusedChildren.Count > 0 || unusedText.IsSome then
                    let unusedChildren =
                          unusedChildren
                          |> Seq.map (fun kv -> KeyValuePair(kv.Key, kv.Value.Elems.ToArray()))
                          |> Dictionary
                    tracer.OnUnused(which, unusedAttrs, unusedChildren, unusedText)

    static member private AttrHelper(name : string, p : StringParser<'T>, elem : Elem) : 'T option =
        elem.Check()

        if elem.UsedAttrs.Add name |> not then
            failwithf $"Attribute %s{name} in elem %A{elem.Which} already used"

        let found, value = elem.UnusedAttrs.Remove name
        if found
        then Some (p elem.Which (Some name) value)
        else None

    static member attrOpt (name : string) (p : StringParser<'T>) (elem : Elem) : 'T option =
        let parsed = Elem.AttrHelper(name, p, elem)
        parsed

    static member attr (name : string) (p : StringParser<'T>) (elem : Elem) : 'T =
        match Elem.AttrHelper(name, p, elem) with
        | None -> raise <| WrongCount (elem.Which, $"Expected one attribute %s{name}")
        | Some parsed -> parsed

    static member private ChildHelper(name : string, p : Elem -> 'T, elem : Elem) : 'T[] =
        elem.Check()

        if elem.UsedChildren.Add name |> not then
            failwithf $"Children %s{name} in elem %A{elem.Which} already used"

        let found, children = elem.UnusedChildren.Remove name
        if found
        then
            Array.init children.Elems.Count (fun i ->
                use elem = children.Elems[i]
                p elem)
        else Array.empty

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

        elem.UnusedAttrs |> Seq.iter (fun kv -> elem.UsedAttrs.Add kv.Key |> ignore)
        elem.UnusedAttrs.Clear()
        elem.UnusedChildren |> Seq.iter (fun kv -> elem.UsedChildren.Add kv.Key |> ignore)
        elem.UnusedChildren.Clear()
        elem.UsedText <- true

and ITracer =
    abstract member OnUnused :
        which:WhichElem *
        attrs:Dictionary<string, string> *
        children:Dictionary<string, Elem[]> *
        text:option<string> -> unit

module Elem =
    /// `reader` must ignore comments and processing instructions.
    // Assumes that `reader` checks that root element exists
    // and that elements are properly nested.
    let parse (tracer : ITracer) (reader : XmlReader) =
        // We can share one `StringBuilder` because mixed content is not allowed.
        let text = System.Text.StringBuilder()

        // Expected initial state: Opening tag with attributes was processed.
        // Final state: `reader.NodeType` is `XmlNodeType.EndElement`.
        let rec parseContent (which : WhichElem) =
            let children = Dictionary<string, {| Prefix : string; Elems : ResizeArray<Elem> |}>()
            let mutable significantText = false  // We found non-whitespace text or CDATA.

            let inline processCData (value : string) =
                if children.Count <> 0 then
                    failwith $"Element %A{which} contains mixed content"
                significantText <- true
                text.Append(value) |> ignore

            let inline processWhitespace (value : string) =
                // Ignore whitespace around child elements.
                // Otherwise we save whitespace
                // because it could be part of text
                // (if there are no child elements we return text even if it contains only whitespace).
                if children.Count = 0 then
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
                        match children.TryGetValue(name) with
                        | true, existingChildren ->
                            if existingChildren.Prefix <> prefix then
                                failwithf "%s %s %s"
                                    $"Element %A{which} contains multiple child elements "
                                    $"with same local name %s{name} "
                                    $"but different prefixes %s{existingChildren.Prefix} and %s{prefix}"
                            existingChildren
                        | false, _ ->
                            let newChildren = {| Prefix = prefix; Elems = ResizeArray() |}
                            children.Add(name, newChildren)
                            newChildren
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
                          if children.Count = 0
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
            let attributes = Dictionary<string, string>()
            while reader.MoveToNextAttribute() do
                let name = reader.LocalName
                let value = reader.Value
                if attributes.TryAdd(name, value) |> not then
                    failwithf "%s %s"
                        $"Element %A{which} contains multiple attributes "
                        $"with same local name %s{name}"
            if isEmptyElement then
                // Element without content.
                new Elem(which, tracer, attributes, Dictionary(), "", false)
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
