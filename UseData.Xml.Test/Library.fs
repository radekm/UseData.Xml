module UseData.Xml.Test.Elem

open System.Xml.Linq
open NUnit.Framework

open UseData.Xml

let toElem str =
    let noOpTracer = { new ITracer with
                         override _.OnParsed(_, _, _, _, _, _) = ()
                         override _.OnUnused(_, _, _, _) = ()
                     }
    str
    |> XDocument.Parse
    |> fun doc -> doc.Root
    |> Elem.make noOpTracer

module Basic =
    let ``child elements - input`` = """
    <animals>
        <dog>A</dog>
        <pig>B</pig>
        <dog>D</dog>
    </animals>
    """

    [<Test>]
    let ``child elements`` ()  =
        let e = ``child elements - input`` |> toElem
        let dogs = e |> Elem.children "dog" (Elem.text Parse.string)
        let pig = e |> Elem.child "pig" (Elem.text Parse.string)
        let cat = e |> Elem.childOpt "cat" (Elem.text Parse.string)
        Assert.AreEqual(["A"; "D"], dogs)
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
        let e = ``attributes - input`` |> toElem
        let people =
            e
            |> Elem.children "person" (fun e ->
                {| Name = e |> Elem.attr "name" Parse.string
                   Address = e |> Elem.attrOpt "address" Parse.string |})
        Assert.AreEqual(
            [ {| Name = "Pete"; Address = None |}
              {| Name = "Harry"; Address = Some "Little Whinging" |} ],
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
        let e = ``multiple children when single child is expected - input`` |> toElem
        let exn =
            Assert.Throws<WrongCount>(fun () ->
                e |> Elem.child "name" Elem.ignoreAll)
        Assert.AreEqual(Root "person", exn.which)

    let ``multiple children when at most one child is expected - input`` =
        ``multiple children when single child is expected - input``

    [<Test>]
    let ``multiple children when at most one child is expected`` () =
        let e = ``multiple children when at most one child is expected - input`` |> toElem
        let exn =
            Assert.Throws<WrongCount>(fun () ->
                e |> Elem.childOpt "name" Elem.ignoreAll |> ignore)
        Assert.AreEqual(Root "person", exn.which)

    let ``no child when one child is expected - input`` = """
    <dog></dog>
    """

    [<Test>]
    let ``no child when one child is expected`` () =
        let e = ``no child when one child is expected - input`` |> toElem
        let exn =
            Assert.Throws<WrongCount>(fun () ->
                e |> Elem.child "name" Elem.ignoreAll)
        Assert.AreEqual(Root "dog", exn.which)

    // TODO no attribute when it's expected
    // TODO element with mixed content

// module TracingUnusedContent
// TODO unused element
// TODO unused attribute
// TODO unused text

// module TracingParsedContent
// TODO tracing parsed content
