namespace Canaries

module Canaries = 
    open FsCheck
    open FsCheck.Xunit
    open FsRandomFlow
    open FsRandomFlow.RVar

    let seeds = RVar.take 25 StdUniform
                |> RVar.runrvar 123456789
        
    //[<Fact>]
    //let 