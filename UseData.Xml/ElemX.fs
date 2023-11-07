namespace UseData.Xml

open System
open System.IO
open System.Xml

/// Designates an element in a document.
type WhichElemX = { Name : string; IndexAmongChildren : int }

// Experimental callback-based API which should be faster than the existing API.
//
// This API is very hard to use.
// You need at least generator for record builders.
module ElemX =
    type Cursor = { Reader : XmlReader
                    // We can share one `StringBuilder` because mixed content is not allowed.
                    SB : System.Text.StringBuilder
                    mutable Which : WhichElemX list }

    let inline parseElem
        ([<InlineIfLambda>] onAttr : string -> string -> unit)
        ([<InlineIfLambda>] onChild : string -> Cursor -> unit)
        ([<InlineIfLambda>] onText : string -> unit)
        (c : Cursor) =

        // Invariants:
        // We assume that new XML element has just been opened and added to `c.Which`.
        if c.Reader.NodeType <> XmlNodeType.Element then
            failwith "Reader must be at beginning of element"
        if c.Which.Head.Name <> c.Reader.Name then
            failwith "Which is inconsistent with Reader"
        if c.SB.Length <> 0 then
            failwith "SB must not be used"

        let isEmptyElement = c.Reader.IsEmptyElement  // Calling this after reading attributes doesn't work.
        while c.Reader.MoveToNextAttribute() do
            let name = c.Reader.LocalName
            if c.Reader.Prefix <> "xmlns" && name <> "xmlns" then
                let value = c.Reader.Value
                onAttr name value

        // Parse content of the element (child elements or text).
        if not isEmptyElement then
            let mutable hasSignificantText = false  // `true` iff text which has at least one non-whitespace character.
            let mutable childCount = 0

            let inline processCData (value : string) =
                if childCount > 0 then
                    failwith $"Element %A{c.Which} contains mixed content"
                hasSignificantText <- true
                c.SB.Append(value) |> ignore

            let inline processWhitespace (value : string) =
                // Ignore whitespace around child elements.
                // Otherwise we save whitespace
                // because it could be part of text
                // (if there are no child elements we return text even if it contains only whitespace).
                if childCount = 0 then
                    c.SB.Append(value) |> ignore

            // We assume that if we reach EOF here, the exception will be thrown
            // (meaning `reader.Read()` here doesn't return false).
            // The reason is that we must first reach closing element before EOF.
            while c.Reader.Read() && c.Reader.NodeType <> XmlNodeType.EndElement do
                match c.Reader.NodeType with
                | XmlNodeType.Element ->
                    if hasSignificantText then
                        failwith $"Element %A{c.Which} contains mixed content"

                    // TODO: Should we do something with `c.Reader.Prefix`?
                    //       What if we have two elements with the same name but different prefixes?

                    childCount <- childCount + 1
                    let name = c.Reader.LocalName
                    c.Which <- { Name = name; IndexAmongChildren = childCount } :: c.Which

                    // Since mixed content is not allowed and we found child element then we won't have text.
                    // Thus `text` `StringBuilder` won't be used in this element anymore
                    // and can be reused in child elements.
                    c.SB.Clear() |> ignore

                    onChild name c
                | XmlNodeType.Text ->
                    let value = c.Reader.Value
                    if String.IsNullOrWhiteSpace value
                    then processWhitespace value
                    else processCData value
                | XmlNodeType.Whitespace -> processWhitespace c.Reader.Value
                | XmlNodeType.CDATA -> processCData c.Reader.Value
                | nodeType -> failwith $"Element %A{c.Which} contains unexpected node: %A{nodeType}"

            // We assume that we have received `c.Reader.NodeType = XmlNodeType.EndElement`.
            if childCount = 0 then
                let text = c.SB.ToString()
                c.SB.Clear() |> ignore
                onText text

        // Content of the element has been processed.
        c.Which <- c.Which.Tail

    let inline private parseRoot
        ([<InlineIfLambda>] onRoot : string -> Cursor -> unit)
        (reader : XmlReader) =

        // Skip nodes until root element is found.
        while reader.Read() && reader.NodeType <> XmlNodeType.Element do
            match reader.NodeType with
            | XmlNodeType.Whitespace
            | XmlNodeType.XmlDeclaration -> ()
            | nodeType -> failwithf "Unexpected node before root element: %A" nodeType

        let name = reader.LocalName
        let cursor = { Reader = reader
                       SB = System.Text.StringBuilder()
                       Which = [{ Name = name; IndexAmongChildren = 0 }] }
        onRoot name cursor

        if not cursor.Which.IsEmpty then
            failwith "Which not empty after parsing root element"

        // Check nodes after root element.
        while reader.Read() do
            match reader.NodeType with
            | XmlNodeType.Whitespace -> ()
            | nodeType -> failwithf "Unexpected node after root element: %A" nodeType

    let inline parseFromTextReader
        ([<InlineIfLambda>] onRoot : string -> Cursor -> unit)
        (reader : TextReader) =

        let settings = XmlReaderSettings()
        settings.IgnoreComments <- true
        settings.IgnoreProcessingInstructions <- true
        use reader = XmlReader.Create(reader, settings)
        parseRoot onRoot reader

    let inline parseFromString
        ([<InlineIfLambda>] onRoot : string -> Cursor -> unit)
        (str : string) =

        use reader = new StringReader(str)
        parseFromTextReader onRoot reader
