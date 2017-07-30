namespace Canaries

module Canaries = 
    open FsCheck
    open FsCheck.Xunit
    open FsRandomFlow
    open FsRandomFlow.RVar
    open Xunit

    let seeds = RVar.take 4 StdUniform
                |> RVar.runrvar 123456789
    
    let testRun rvar = 
        seeds
        |> Array.map (fun x -> RVar.runrvar x rvar)
    
    //Distributions

    [<Fact>]
    let canaryStdUniform = [|1287638892; 1840282602; 928205646; 470358147|] = seeds

    [<Fact>]
    let canaryStdNormal = [|-0.3205875829; -1.323026481; -0.4755294123; 0.3722125042|] = testRun (RVar.Normal(0.0,1.0))

    [<Fact>]
    let canaryExponential = [|0.2985867874; 1.753716246; 8.98085485; 0.15788101|] = testRun (RVar.Exponential(0.5))

    [<Fact>]
    let canaryWeibull = [|0.0111442587; 0.3844400839; 10.08196923; 0.003115801664|] = testRun (RVar.Weibull(0.5,0.5))

    [<Fact>]
    let canaryPoisson = [|8; 6; 2; 2|] = testRun (RVar.Poisson(5.5))