//Copyright 2017 Barend Venter
//This code liscensed under the MIT license, see LICENSE
//Get it at github.com/barendventer/fsrandomflow

namespace FsRandomFlowTests

///Failing these tests do not mean your implementation is incorrect.
///It does mean that the changes will force a new change in major version.
module Canaries = 
    open Xunit
    open FsRandomFlow
    open FsRandomFlow.RVar

    let seeds = RVar.take 4 StdUniform
                |> RVar.runrvar 123456789
    
    let testRun rvar = 
        seeds
        |> Array.map (fun x -> RVar.runrvar x rvar)

    //These tests should be updated each time the seeds are broken.
    //Change the tests to ensure that they pass.
    
    //Distributions

    [<Fact>]
    let canaryStdUniform () = [|1287638892; 1840282602; 928205646; 470358147|] = seeds

    [<Fact>]
    let canaryStdNormal () = [|-0.3205875829; -1.323026481; -0.4755294123; 0.3722125042|] = testRun (RVar.Normal(0.0,1.0))

    [<Fact>]
    let canaryExponential () = [|0.2985867874; 1.753716246; 8.98085485; 0.15788101|] = testRun (RVar.Exponential(0.5))

    [<Fact>]
    let canaryWeibull () = [|0.0111442587; 0.3844400839; 10.08196923; 0.003115801664|] = testRun (RVar.Weibull(0.5,0.5))

    [<Fact>]
    let canaryPoisson () = [|8; 6; 2; 2|] = testRun (RVar.Poisson(5.5))