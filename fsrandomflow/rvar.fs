//Copyright 2017, Barend Venter
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

module RVar =
    ///This random variable exposes the underlying uniformly-distributed values produced by the random source.
    let StdUniform = StdUniformClass() :> RVar<int>

    type MappedVar<'T,'U>(BaseVar : RVar<'T>, MapFn : 'T -> 'U) = 
        interface RVar<'U> with
            override this.sample rsource = MapFn(BaseVar.sample(rsource))

    let map f v = MappedVar(v,f) :> RVar<'T>

    type ApplyVar<'T,'U>(BaseVar : RVar<'T -> 'U>, ArgVar : RVar<'T>) =
        interface RVar<'U> with
            override this.sample rsource = 
                let f = BaseVar.sample(rsource)
                let v = ArgVar.sample(rsource)
                f v

    let apply vv vf = ApplyVar(vf,vv) :> RVar<'T>

    type BindVar<'T,'U>(BaseVar : RVar<'T>, Kleisli : 'T -> RVar<'U>) = 
        interface RVar<'U> with
            override this.sample rsource =
                let v = BaseVar.sample(rsource)
                (Kleisli v).sample(rsource)

    let concatMap f v = BindVar(v,f) :> RVar<'T>

    type TakeVar<'T>(BaseVar : RVar<'T>, count : int) =
        interface RVar<'T array> with
            override this.sample rsource = 
                (fun _ -> BaseVar.sample(rsource))
                |> Array.init count

    let take count v = TakeVar(v,count) :> RVar<'T array>

    let zip v1 v2 =
        v1
        |> map (fun x -> (fun y -> (x,y)))
        |> apply v2 :> RVar<'T * 'U>

    type ConstVar<'T>(v) =
        interface RVar<'T> with
            override this.sample rsource = v

    let constant v = ConstVar(v) :> RVar<'T>