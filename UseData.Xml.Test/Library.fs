module UseData.Xml.Test

open System.Xml.Linq
open NUnit.Framework

let toElem str =
    let noOpTracer = { new ITracer with
                         override _.OnParsed(_, _, _, _, _, _) = ()
                         override _.OnUnused(_, _, _, _) = ()
                     }
    str
    |> XDocument.Parse
    |> fun doc -> doc.Root
    |> Elem.make noOpTracer

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


// TODO multiple children when single child is expected
// TODO multiple children when single childOpt is expected
// TODO no child when one is expected
// TODO no attribute when it's expected
// TODO element with mixed content

// TODO unused element
// TODO unused attribute
// TODO unused text

// TODO tracing
