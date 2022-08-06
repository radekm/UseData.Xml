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

    // `dateTimeOffset` must be defined before overriding `int.`
    /// Accepts either format without fraction seconds or format with up to 7 digits of fractional seconds:
    /// - Without fractional seconds `YYYY-mm-ddTHH:mm:ssZ` or `YYYY-mm-dd HH:mm:ssZ` (string length 20).
    /// - With fractional seconds `YYYY-mm-ddTHH:mm:ss.fffZ` or `YYYY-mm-dd HH:mm:ss.fffZ` (string length 22-28).
    let dateTimeOffset : StringParser<DateTimeOffset> = fromFunction <| fun s ->
        let error = Result.Error "Expected date time offset"
        let inline readDigit i =
            let d = int s[i] - int '0'
            if d < 0 || d > 9
            then failwithf "Not a digit %c" s[i]
            else d

        if s.Length < 20 || s[4] <> '-' || s[7] <> '-' || (s[10] <> 'T' && s[10] <> ' ') || s[13] <> ':' || s[16] <> ':'
        then error
        else
            try
                let year = ((readDigit 0 * 10 + readDigit 1) * 10 + readDigit 2) * 10 + readDigit 3
                let month = readDigit 5 * 10 + readDigit 6
                let day = readDigit 8 * 10 + readDigit 9
                let hour = readDigit 11 * 10 + readDigit 12
                let minute = readDigit 14 * 10 + readDigit 15
                let second = readDigit 17 * 10 + readDigit 18

                let withoutFractionalSeconds = DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero) 

                match s.Length with
                | 20 when s[19] = 'Z' -> Result.Ok withoutFractionalSeconds
                | len when len >= 22 && len <= 28 && s[19] = '.' && s[len - 1] = 'Z' ->
                    let mutable ticks = 0
                    // Read fractional digits.
                    for i = 20 to len - 2 do
                        ticks <- ticks * 10 + readDigit i
                    // Padding.
                    for i = 1 to 28 - len do
                        ticks <- ticks * 10
                    Result.Ok (withoutFractionalSeconds.AddTicks ticks)
                | _ -> error
            with _ -> error

    let int = fromTryParse "Expected int" Int32.TryParse
    let uint = fromTryParse "Expected uint" UInt32.TryParse

    let int64 = fromTryParse "Expected int64" Int64.TryParse
    let uint64 = fromTryParse "Expected uint64" UInt64.TryParse

    let decimal =
        fromTryParse "Expected decimal" <| fun s ->
            Decimal.TryParse(
                s,
                System.Globalization.NumberStyles.AllowDecimalPoint |||
                System.Globalization.NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture)

    let decimalNonNegative = decimal |> validate (function
        | x when x < 0m -> Some "Expected non-negative decimal"
        | _ -> None)

    let bool = fromTryParse "Expected bool" Boolean.TryParse

    /// This is very slow.
    let dateTimeOffsetFormats (formats : string list) : StringParser<DateTimeOffset> =
        fromTryParse $"Expected DateTimeOffset with one of following formats: %A{formats}" <| fun s ->
            DateTimeOffset.TryParseExact(
                s,
                List.toArray formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal)
