module UseData.Xml.Test.Elem

open System
open System.Collections.Generic
open System.Xml

open NUnit.Framework

open UseData.Xml

let toElem str =
    let noOpTracer = { new ITracer with
                         override _.OnUnused(_, _, _, _) = failwith "Unexpected unused content"
                     }
    Elem.parseFromString noOpTracer str

module Basic =
    let ``xml declaration - input`` = """<?xml version="1.0" encoding="utf-16"?><animals />"""

    [<Test>]
    let ``xml declaration`` ()  =
        use e = ``xml declaration - input`` |> toElem
        Assert.AreEqual("animals", e.Name)

    let ``child elements - input`` = """
    <animals>
        <dog>A</dog>
        <pig>B</pig>
        <dog>D</dog>
    </animals>
    """

    [<Test>]
    let ``child elements`` ()  =
        use e = ``child elements - input`` |> toElem
        let dogs = e |> Elem.children "dog" (Elem.text Parse.string)
        let pig = e |> Elem.child "pig" (Elem.text Parse.string)
        let cat = e |> Elem.childOpt "cat" (Elem.text Parse.string)
        Assert.AreEqual([| "A"; "D" |], dogs)
        Assert.AreEqual("B", pig)
        Assert.AreEqual(None, cat)

    let ``attributes - input`` = """
    <people>
        <person name="Pete" />
        <person name="Harry" address="Little Whinging" />
    </people>
    """

    [<Test>]
    let attributes ()  =
        use e = ``attributes - input`` |> toElem
        let people =
            e
            |> Elem.children "person" (fun e ->
                {| Name = e |> Elem.attr "name" Parse.string
                   Address = e |> Elem.attrOpt "address" Parse.string |})
        Assert.AreEqual(
            [| {| Name = "Pete"; Address = None |}
               {| Name = "Harry"; Address = Some "Little Whinging" |} |],
            people)

module Errors =
    let ``multiple children when single child is expected - input`` = """
    <person>
        <name>Peter</name>
        <name>James</name>
    </person>
    """

    [<Test>]
    let ``multiple children when single child is expected`` () =
        use e = ``multiple children when single child is expected - input`` |> toElem
        let exn =
            Assert.Throws<WrongCount>(fun () ->
                e |> Elem.child "name" Elem.ignoreAll)
        Assert.AreEqual(Root "person", exn.which)

    let ``multiple children when at most one child is expected - input`` =
        ``multiple children when single child is expected - input``

    [<Test>]
    let ``multiple children when at most one child is expected`` () =
        use e = ``multiple children when at most one child is expected - input`` |> toElem
        let exn =
            Assert.Throws<WrongCount>(fun () ->
                e |> Elem.childOpt "name" Elem.ignoreAll |> ignore)
        Assert.AreEqual(Root "person", exn.which)

    let ``no child when one child is expected - input`` = """
    <dog></dog>
    """

    [<Test>]
    let ``no child when one child is expected`` () =
        use e = ``no child when one child is expected - input`` |> toElem
        let exn =
            Assert.Throws<WrongCount>(fun () ->
                e |> Elem.child "name" Elem.ignoreAll)
        Assert.AreEqual(Root "dog", exn.which)

    let ``no attribute when one attribute is expected - input`` = """
    <city></city>
    """

    [<Test>]
    let ``no attribute when one attribute is expected`` () =
        use e = ``no attribute when one attribute is expected - input`` |> toElem
        let exn =
            Assert.Throws<WrongCount>(fun () ->
                e |> Elem.attr "name" Parse.string |> ignore)
        Assert.AreEqual(Root "city", exn.which)

    let ``mixed content in elements is not allowed - input`` = """
    <country>
      Czech Republic
      <president>Vaclav Havel</president>
    </country>
    """

    [<Test>]
    let ``mixed content in elements is not allowed`` () =
        let exn =
            Assert.Throws<Exception>(fun () ->
                use e = ``mixed content in elements is not allowed - input`` |> toElem
                ())
        Assert.AreEqual("Element Root \"country\" contains mixed content", exn.Message)

module InvalidXml =
    let ``missing root element - input`` = """<?xml version="1.0" encoding="utf-16"?>"""

    [<Test>]
    let ``missing root element`` () =
        let exn =
            Assert.Throws<XmlException>(fun () ->
                use _ = ``missing root element - input`` |> toElem
                ())
        Assert.AreEqual("Root element is missing.", exn.Message)

    let ``different closing tag - input`` = """
    <root>
      <a></b>
    </root>
    """

    [<Test>]
    let ``different closing tag`` () =
        Assert.Throws<XmlException>(fun () ->
            use _ = ``different closing tag - input`` |> toElem
            ())
        |> ignore

    let ``missing closing tag - input`` = """<root><person>"""

    [<Test>]
    let ``missing closing tag`` () =
        Assert.Throws<XmlException>(fun () ->
            use _ = ``missing closing tag - input`` |> toElem
            ())
        |> ignore

module TracingUnusedContent =
    type Unused = { Which : WhichElem
                    UnusedAttrs : Set<string>
                    UnusedChildren : Set<string>
                    UnusedText : string option }

    let toElem onUnused str =
        let keys (d : Dictionary<_, _>) = d.Keys |> Set.ofSeq
        let unusedTracer = { new ITracer with
                             override _.OnUnused(which, unusedAttrs, unusedChildren, unusedText) =
                                 onUnused { Which = which
                                            UnusedAttrs = unusedAttrs |> keys
                                            UnusedChildren = unusedChildren |> keys
                                            UnusedText = unusedText }
                           }
        Elem.parseFromString unusedTracer str

    let ``unused element - input`` = """
    <company>
        <address>
            <street>Baker Street</street>
            <city>London</city>
        </address>
    </company>
    """

    [<Test>]
    let ``unused element`` () =
        let expectedUnusedContent =
            [ { Which = Child (Root "company", "address", 0)
                UnusedAttrs = Set.empty
                UnusedChildren = Set.ofList ["city"]
                UnusedText = None
              } ]

        let log = ResizeArray()
        let street =
            use e = ``unused element - input`` |> toElem log.Add
            e
            |> Elem.child "address" (fun e ->
                e |> Elem.child "street" (Elem.text Parse.string))

        Assert.AreEqual("Baker Street", street)
        // We should convert `log` to a list after `e` is disposed.
        Assert.AreEqual(expectedUnusedContent, log |> Seq.toList)

    let ``unused attribute - input`` = """
    <company>
        <address street="Baker Street" city="London" />
    </company>
    """

    [<Test>]
    let ``unused attribute`` () =
        let expectedUnusedContent =
            [ { Which = Child (Root "company", "address", 0)
                UnusedAttrs = Set.ofList ["street"]
                UnusedChildren = Set.empty
                UnusedText = None
              } ]

        let log = ResizeArray()
        let city =
            use e = ``unused attribute - input`` |> toElem log.Add
            e
            |> Elem.child "address" (fun e ->
                e |> Elem.attr "city" Parse.string)

        Assert.AreEqual("London", city)
        // We should convert `log` to a list after `e` is disposed.
        Assert.AreEqual(expectedUnusedContent, log |> Seq.toList)

    let ``unused text - input`` = ``unused element - input``

    [<Test>]
    let ``unused text`` () =
        let expectedUnusedContent =
            [ { Which = Child (Child (Root "company", "address", 0), "city", 0)
                UnusedAttrs = Set.empty
                UnusedChildren = Set.empty
                UnusedText = Some "London"
              } ]

        let log = ResizeArray()
        let street =
            use e = ``unused element - input`` |> toElem log.Add
            e
            |> Elem.child "address" (fun e ->
                e |> Elem.child "city" ignore
                e |> Elem.child "street" (Elem.text Parse.string))

        Assert.AreEqual("Baker Street", street)
        // We should convert `log` to a list after `e` is disposed.
        Assert.AreEqual(expectedUnusedContent, log |> Seq.toList)
