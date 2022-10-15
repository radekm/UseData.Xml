namespace UseData.Xml

open System.Collections.Generic
open System.Xml.Linq

/// Designates an element in a document.
type WhichElem =
    | Root of elemName:string
    | Child of parent:WhichElem * elemName:string * indexAmongElemsWithSameName:int

    /// Name of the designated element.
    member me.Name =
        match me with
        | Root name -> name
        | Child (_, name, _) -> name

module WhichElem =
    // Slow when `elem` has lots of siblings.
    // Fast when `elem` is a root.
    let rec make (elem : XElement) : WhichElem =
        let name = elem.Name.LocalName
        match elem.Parent with
        | null -> Root name
        | parent ->
            let indexAmongElemsWithSameName =
                parent.Elements()
                |> Seq.filter (fun e -> e.Name.LocalName = name)
                |> Seq.findIndex (LanguagePrimitives.PhysicalEquality elem)
            Child (make parent, name, indexAmongElemsWithSameName)

/// A parser which parses a string from XML into type `'T`.
///
/// A parser `p` is called with three arguments `p which selector str`.
/// Where `str` is the string which will be parsed. `which`
/// and `selector` serve only for error reporting.
/// `which` is the element where the string originates.
/// `selector` is either `None` if the string is the content of the element.
/// Otherwise the string must originate from an attribute
/// and `selector` contains the name of the attribute.
type StringParser<'T> = WhichElem -> string option -> string -> 'T
