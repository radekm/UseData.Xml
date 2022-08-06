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
let ``parse decimal parses leading sign`` () =
    Assert.AreEqual(4532M, Parse.decimal which selector "+4532")
    Assert.AreEqual(-4532M, Parse.decimal which selector "-4532")

[<Test>]
let ``parse decimal parses dot`` () =
    Assert.AreEqual(4532.72M, Parse.decimal which selector "4532.72")
    Assert.AreEqual(0.1M, Parse.decimal which selector "0.1")
    Assert.AreEqual(0.03M, Parse.decimal which selector "00.03")
    Assert.AreEqual(0M, Parse.decimal which selector "0.0")
    Assert.AreEqual(0M, Parse.decimal which selector "0.0")

[<Test>]
let ``parse decimal does not parse comma`` () =
    Assert.Throws<ParseError>(fun () ->
        Parse.decimal which selector "4532,7" |> ignore)
    |> ignore

[<Test>]
let ``parse bool`` () =
    Assert.AreEqual(true, Parse.bool which selector "true")

[<Test>]
let ``parse date time offset without fractional seconds`` () =
    Assert.AreEqual(
        DateTimeOffset(2021, 5, 31, 23, 12, 4, TimeSpan.Zero),
        Parse.dateTimeOffset which selector "2021-05-31T23:12:04Z")

[<Test>]
let ``parse date time offset with milliseconds`` () =
    Assert.AreEqual(
        DateTimeOffset(2021, 5, 31, 23, 12, 4, 999, TimeSpan.Zero),
        Parse.dateTimeOffset which selector "2021-05-31T23:12:04.999Z")

[<Test>]
let ``parse date time offset with 1 digit of fractional seconds`` () =
    Assert.AreEqual(
        DateTimeOffset(2022, 7, 25, 6, 0, 0, 400, TimeSpan.Zero),
        Parse.dateTimeOffset which selector "2022-07-25T06:00:00.4Z")

[<Test>]
let ``parse date time offset with 7 digits of fractional seconds`` () =
    Assert.AreEqual(
        DateTimeOffset(2022, 7, 25, 6, 0, 0, TimeSpan.Zero).AddTicks(1340144L),        
        Parse.dateTimeOffset which selector "2022-07-25T06:00:00.1340144Z")

[<Test>]
let ``parsing date time offset doesn't lose information - original string can be produced`` () =
    let s = "2022-07-25T06:01:02.1340144Z"
    let parsed = Parse.dateTimeOffset which selector s
    let formatted = parsed.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'")
    Assert.AreEqual(s, formatted)

[<Test>]
let ``parse date time offset where date is separated from time by single space`` () =
    Assert.AreEqual(
        DateTimeOffset(2021, 5, 31, 23, 12, 4, TimeSpan.Zero),
        Parse.dateTimeOffset which selector "2021-05-31 23:12:04Z")

[<Test>]
let ``parse date time offset (failure)`` () =
    let wrongInput = "2021-13-31 23:12:04Z"
    let exn =
        Assert.Throws<ParseError>(fun () ->
            Parse.dateTimeOffset which selector wrongInput
            |> ignore)
    Assert.AreEqual(which, exn.which)
    Assert.AreEqual(selector, exn.selector)
    Assert.AreEqual(wrongInput, exn.input)
