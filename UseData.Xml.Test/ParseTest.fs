module UseData.Xml.Test.Parse

open System
open NUnit.Framework

open UseData.Xml

let which = Root "person"
let selector = Some "address"

[<Test>]
let ``parse int`` () =
    Assert.AreEqual(4532, Parse.int which selector "4532")

[<Test>]
let ``parse bool`` () =
    Assert.AreEqual(true, Parse.bool which selector "true")

[<Test>]
let ``parse date time offset`` () =
    Assert.AreEqual(
        DateTimeOffset(2021, 5, 31, 23, 12, 4, TimeSpan.Zero),
        Parse.dateTimeOffset which selector "2021-05-31T23:12:04Z")

[<Test>]
let ``parse date time offset (failure)`` () =
    let wrongInput = "2021-05-31 23:12:04Z"
    let exn =
        Assert.Throws<ParseError>(fun () ->
            Parse.dateTimeOffset which selector wrongInput
            |> ignore)
    Assert.AreEqual(which, exn.which)
    Assert.AreEqual(selector, exn.selector)
    Assert.AreEqual(wrongInput, exn.input)
