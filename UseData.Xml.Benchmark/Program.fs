module UseData.Xml.Benchmark.Program

open System

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Diagnosers
open BenchmarkDotNet.Running

[<EventPipeProfiler(EventPipeProfile.CpuSampling)>]
[<MemoryDiagnoser>]
type Benchmarks() =

    [<Benchmark>]
    member _.ParseM7Message() = M7Message.parse ()

    [<Benchmark>]
    member _.ParseM7MessageByExperimentalCallbackApi() = M7Message.parseByExperimentalCallbackApi ()

    [<Benchmark>]
    member _.ReadM7MessageByXmlReader() = M7Message.readByXmlReader ()

[<EntryPoint>]
let main args =
    BenchmarkRunner.Run<Benchmarks>() |> ignore
    0
