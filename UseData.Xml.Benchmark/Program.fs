module UseData.Xml.Benchmark.Program

open System

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running

[<MemoryDiagnoser>]
type Benchmarks() =

    [<Benchmark>]
    member _.ParseM7Message() = M7Message.parse ()

    [<Benchmark>]
    member _.ReadM7MessageByXmlReader() = M7Message.readByXmlReader ()

[<EntryPoint>]
let main args =
    BenchmarkRunner.Run<Benchmarks>() |> ignore
    0
