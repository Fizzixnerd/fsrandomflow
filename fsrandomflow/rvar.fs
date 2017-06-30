//Copyright 2017 Barend Venter
//Licensed under the MIT license, see LICENSE

namespace fsrandomflow

open System.Collections.Generic

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

type RandomlyBuilder() =
    member this.Bind(v, f) = BindVar(v,f) :> RVar<'T>
    member this.Return(v) = ConstVar(v) :> RVar<'T>
    member this.ReturnFrom(v) = v
    member this.Delay(f) = f()
    member this.Run(v) = v
    member this.Combine(v1, v2) = v2
    member this.For(vs, f) = Seq.map f vs
    //member this.TryFinally 
    //member this.TryWith
    //member this.Using
    //member this.While


module RVar =
    ///A random computation to be evaluated with a random seed supplied at sample time
    let randomly = RandomlyBuilder()

    ///This random variable exposes the underlying uniformly-distributed values produced by the random source.
    let StdUniform = StdUniformClass() :> RVar<int>

    let map f v = MappedVar(v,f) :> RVar<'T>

    let apply vv vf = ApplyVar(vf,vv) :> RVar<'T>

    let concatMap f v = BindVar(v,f) :> RVar<'T>

    type TakeVar<'T>(BaseVar : RVar<'T>, count : int) =
        interface RVar<'T array> with
            override this.sample rsource = 
                (fun _ -> BaseVar.sample(rsource))
                |> Array.init count

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

    let rec sequence (xs : RVar<'T> seq) = randomly {
            if Seq.isEmpty xs then return Seq.empty
            else let! nextOut = Seq.head xs
                 let! nextTodo = sequence (Seq.tail xs)
                 return seq {
                    yield nextOut
                    yield! nextTodo
                 } 
            }

    let rec UniformZeroAndUpBelow n = 
        let max = 0x7FFFFFFF
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