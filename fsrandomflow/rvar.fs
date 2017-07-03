//Copyright 2017 Barend Venter
//Licensed under the MIT license, see LICENSE

namespace fsrandomflow

open System.Collections.Generic
open Twister
open System

type RVar<'T> =
    /// Using a stream of random values, generate an example value of type 'T
    abstract member sample : IEnumerator<int> -> 'T

///This random variable exposes the underlying uniformly-distributed values produced by the random source.
type StdUniformClass() =
    interface RVar<int> with
        override this.sample(rsource : IEnumerator<int>) = 
            let res = rsource.Current;
            rsource.MoveNext() |> ignore;
            res

type MappedVar<'T,'U>(BaseVar : RVar<'T>, MapFn : 'T -> 'U) = 
    interface RVar<'U> with
        override this.sample rsource = MapFn(BaseVar.sample(rsource))

type ApplyVar<'T,'U>(BaseVar : RVar<'T -> 'U>, ArgVar : RVar<'T>) =
    interface RVar<'U> with
        override this.sample rsource = 
            let f = BaseVar.sample(rsource)
            let v = ArgVar.sample(rsource)
            f v

type BindVar<'T,'U>(BaseVar : RVar<'T>, Kleisli : 'T -> RVar<'U>) = 
    interface RVar<'U> with
        override this.sample rsource =
            let v = BaseVar.sample(rsource)
            (Kleisli v).sample(rsource)

type ConstVar<'T>(v) =
    interface RVar<'T> with
        override this.sample rsource = v

///This threads a different random generator through some branch, either to run it in parallel 
/// or to allow the computation to be safely aborted. It always steps the underlying generator 
/// exactly once.
type Spark<'T>(v : RVar<'T>) =
    static member branch n = twister(n) //TODO: Use dynamic creation, don't just change the seed
    interface RVar<'T> with
        override this.sample rsource = 
            let newBranch = Spark<_>.branch rsource.Current
            ignore(rsource.MoveNext())
            v.sample(newBranch.GetEnumerator())

type Streaming<'T>(v : RVar<'T>) = 
    interface RVar<'T seq> with
        override this.sample rsource = 
            let newBranch = Spark<_>.branch rsource.Current
            ignore (rsource.MoveNext())
            newBranch.GetEnumerator()
            |> Seq.unfold (fun gen -> Some(v.sample(gen),gen))    

type SequenceVar<'T>(vs: RVar<'T> seq) =
    interface RVar<'T seq> with
        override this.sample rsource = Seq.map(fun (x: RVar<'T>) -> x.sample rsource) vs

type RandomlyBuilder() =
    member this.Bind(v, f) = BindVar(v,f) :> RVar<'T>
    member this.Return(v) = ConstVar(v) :> RVar<'T>
    member this.ReturnFrom(v) = v
    member this.Delay(f) = f()
    member this.Run(v) = v
    member this.For(vs, f) = SequenceVar(Seq.map f vs) 


module RVar =
    ///A random computation to be evaluated with a random seed supplied at sample time
    let randomly = RandomlyBuilder()

    ///This random variable exposes the underlying uniformly-distributed values produced by the random source.
    let StdUniform = StdUniformClass() :> RVar<int>

    let runrvar seed (rvar : RVar<'T>) = rvar.sample(twister(seed).GetEnumerator())
    
    let runrvarIO rvar = rvar |> runrvar System.DateTime.Now.Millisecond
     
    let map f v = MappedVar(v,f) :> RVar<'T>

    let apply vv vf = ApplyVar(vf,vv) :> RVar<'T>

    let concatMap f v = BindVar(v,f) :> RVar<'T>

    type TakeVar<'T>(BaseVar : RVar<'T>, count : int) =
        interface RVar<'T array> with
            override this.sample rsource = 
                (fun _ -> BaseVar.sample(rsource))
                |> Array.init count

    let repeat v = Streaming(v) :> RVar<'T seq>

    let constant v = ConstVar(v) :> RVar<'T>

    type RandomlyBuilder() =
        member this.Bind(comp,func) = concatMap
    
    let take count v = TakeVar(v,count) :> RVar<'T array>
    
    ///This random variable simulates a coin flip ("Bernoulli trial").
    let CoinFlip = map (fun n -> n%2 = 0) StdUniform

    let RandomlySignedInt v = randomly {
            let! flip = CoinFlip
            return if flip then v else -v
        }

    let RandomlySignedDouble v = randomly {
            let! flip = CoinFlip
            return if flip then v else -1.0 * v
        }

    let RandomlySignedFloat v = randomly {
            let! flip = CoinFlip
            return if flip then v else -1.0f * v
        }

    let zip v1 v2 = randomly {
            let! v1' = v1
            let! v2' = v2
            return (v1',v2')
        }

    let zip3 v1 v2 v3 = randomly {
            let! v1' = v1
            let! v2' = v2
            let! v3' = v3
            return (v1',v2',v3')
        }

    let sequence (xs : RVar<'T> seq) = SequenceVar(xs) :> RVar<'T seq>

    let rec filter f x = randomly {
        let! result = x
        if f result then return result
        else return! filter f x
    }

    let rec filterRandomly (f : 'T -> RVar<bool>) (x : RVar<'T>) = randomly {
        let! result = x
        let! outcome = f result
        if outcome then return result
        else return! filterRandomly f x
    }

    let rec UniformZeroAndUpBelow n = 
        let max = System.Int32.MaxValue
        let cutoff = max - (max % n)
        randomly {
            let! res = StdUniform
            if res > cutoff then return! UniformZeroAndUpBelow n
            else return res % n
        }

    let UniformInt(n1, n2) =
        let nmin = min n1 n2
        let nmax = max n1 n2
        let range = nmax - nmin + 1
        randomly {
            let! res = UniformZeroAndUpBelow range
            return res + nmin
        }

    let UniformZeroToOne = randomly {
            let max = float System.Int32.MaxValue
            let! actual = StdUniform
            return float actual / max
        }

    let UniformZeroToBelowOne = filter (fun x -> x < 1.0) UniformZeroToOne
        
    let probability p = 
        if p >= 1.0 then constant true
        else if p <= 0.0 then constant false
        else randomly {
            let! outcome = UniformZeroToOne
            return outcome < p
        }
//
//    let standardNormalPdf x = (Math.E ** (0.5 * (x * x))) / Math.Sqrt(2.0 * Math.PI)
//
//    let ziggarut layers tailfn = randomly {
//        let! layer = UniformInt(0,Seq.length layers)
//        let zeroToOneExclusive = filter (fun x -> x < 1) UniformZeroToOne
//        let xi = Seq.item layer layers
//        let! x = RVar.map(fun x -> x * (Seq.item layer layers)) zeroToOneExclusive

//    let StandardNormal =
//        UniformZeroToOne
//        |> map (fun x -> x * 6.0)
//        |> filterRandomly(fun y -> probability(standardNormalPdf y))
//        |> concatMap RandomlySignedDouble