namespace UseData.Xml

open System

exception ParseError of which:WhichElem * selector:string option * input:string * msg:string
    with
        override me.Message = $"Error when parsing attribute or content %A{me.selector} of %A{me.which}: %s{me.msg}"

module Parse =
    let inline fromTryParse (msg : string) (tryParse : string -> bool * 'T) : StringParser<'T> = fun which selector s ->
        let parsed, res = tryParse s
        if parsed
        then res
        else raise <| ParseError (which, selector, s, msg)

    let inline fromFunction (f : string -> Result<'T, string>) = fun which selector s ->
        match f s with
        | Result.Ok res -> res
        | Result.Error err -> raise <| ParseError (which, selector, s, err)

    let inline transform (f : 'T -> Result<'U, string>) (p : StringParser<'T>) : StringParser<'U> = fun which selector s ->
        let parsed = p which selector s
        match f parsed with
        | Result.Ok res -> res
        | Result.Error err -> raise <| ParseError (which, selector, s, err)

    let inline validate (f : 'T -> string option) (p: StringParser<'T>) : StringParser<'T> = fun which selector s ->
        let parsed = p which selector s
        match f parsed with
        | None -> parsed
        | Some err -> raise <| ParseError (which, selector, s, err)

    let string : StringParser<string> = fun _ _ s -> s

    let stringNonEmpty = fromFunction <| function
        | "" -> Result.Error "Expected non-empty string"
        | s -> Result.Ok s

    let stringNonWhitespace = fromFunction <| fun s ->
        if s.Trim().Length > 0
        then Result.Ok s
        else Result.Error "Expected string with at least one non-whitespace character"

    let stringOneOf (cases : string list) : StringParser<string> = fromFunction <| fun s ->
        if List.contains s cases
        then Result.Ok s
        else Result.Error $"Expected one of following strings: %A{cases}"

    let enum (cases : list<string * 'T>) : StringParser<'T> = fromFunction <| fun s ->
        cases
        |> List.tryFind (fun (key, _) -> key = s)
        |> function
            | None -> Result.Error $"Expected one of following strings %A{cases |> List.map fst}"
            | Some (_, res) -> Result.Ok res

    let int = fromTryParse "Expected int" Int32.TryParse
    let uint = fromTryParse "Expected uint" UInt32.TryParse

    let int64 = fromTryParse "Expected int64" Int64.TryParse
    let uint64 = fromTryParse "Expected uint64" UInt64.TryParse

    let decimal = fromTryParse "Expected decimal" Decimal.TryParse
    let decimalNonNegative = decimal |> validate (function
        | x when x < 0m -> Some "Expected non-negative decimal"
        | _ -> None)

    let bool = fromTryParse "Expected bool" Boolean.TryParse

    let dateTimeOffsetFormats (formats : string list) : StringParser<DateTimeOffset> =
        fromTryParse $"Expected DateTimeOffset with one of following formats: %A{formats}" <| fun s ->
            DateTimeOffset.TryParseExact(
                s,
                List.toArray formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal)

    let dateTimeOffset = dateTimeOffsetFormats ["yyyy-MM-dd'T'HH:mm:ssK"]
