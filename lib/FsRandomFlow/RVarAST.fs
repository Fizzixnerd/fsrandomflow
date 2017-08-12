//Copyright 2017 Barend Venter
//This code liscensed under the MIT license, see LICENSE
//Get it at github.com/barendventer/fsrandomflow

namespace FsRandomFlow
//This is how trees describing random calculations are built

//Extra types of random variables could always be added to help with
//optimization.

//Also, a more clever implementation of the syntax builder might include
//optimizations for cases like mapping over a constant.

///The underlying RVar abstract syntax tree
module RVarAST =
    open Twister
    open System.Collections.Generic

    ///This random variable exposes the underlying uniformly-distributed values produced by the random source.
    type StdUniformClass() =
        interface RVar<int> with
            override this.Sample(rsource : IEnumerator<int>) = 
                let res = rsource.Current
                rsource.MoveNext() |> ignore
                res

    type MappedVar<'T,'U>(v : RVar<'T>, f : 'T -> 'U) = 
        interface RVar<'U> with
            override this.Sample rsource = f(v.Sample(rsource))

    type ApplyVar<'T,'U>(v : RVar<'T -> 'U>, w : RVar<'T>) =
        interface RVar<'U> with
            override this.Sample rsource = 
                let f = v.Sample(rsource)
                let v' = w.Sample(rsource)
                f v'

    type BindVar<'T,'U>(v : RVar<'T>, kleisli : 'T -> RVar<'U>) = 
        interface RVar<'U> with
            override this.Sample rsource =
                let v' = v.Sample(rsource)
                (kleisli v').Sample(rsource)

    type CombineVar<'T,'U>(prev : RVar<unit>, next : RVar<'U>) = 
        interface RVar<'U> with
            override this.Sample rsource =
                prev.Sample(rsource)
                next.Sample(rsource)

    type ConstVar<'T>(v) =
        interface RVar<'T> with
            override this.Sample rsource = v

    ///This threads a different random generator through some branch, either to run it in parallel 
    ///or to allow the computation to be safely aborted. It always steps the underlying generator 
    ///exactly once.
    type Spark<'T>(v : RVar<'T>) =
        static member Branch n = twister(n) //TODO: Use dynamic creation, don't just change the seed
        interface RVar<'T> with
            override this.Sample rsource = 
                let newBranch = Spark<_>.Branch rsource.Current
                rsource.MoveNext() |> ignore
                v.Sample(newBranch.GetEnumerator())

    type Streaming<'T>(v : RVar<'T>) = 
        interface RVar<'T seq> with
            override this.Sample rsource = 
                let newBranch = Spark<_>.Branch rsource.Current
                rsource.MoveNext() |> ignore
                newBranch.GetEnumerator()
                |> Seq.unfold (fun gen -> Some(v.Sample(gen),gen))    

    //Note: SequenceVar is head-strict rather than strict by choice.
    //While this might be a mistake, the reasoning is that user will not
    //directly create SequenceVar instances, so it is better to leave it flexible
    //and push the work of assuring determinism on the library.
    //Either wrap it in spark if you plan to use the head-strict behavior, or 
    //convert the sequence to a fully strict data type if you do not.
    type SequenceVar<'T>(vs: RVar<'T> seq) =
        interface RVar<'T seq> with
            override this.Sample rsource = 
                Seq.map(fun (x: RVar<'T>) -> x.Sample rsource) vs
    
    type UnfoldVar<'T,'State>(kleisli: 'State -> RVar<('T * 'State) option>, state: 'State) =
        interface RVar<'T seq> with
            override this.Sample rsource =
                let gen = (Spark<_>.Branch rsource.Current).GetEnumerator()
                Seq.unfold(fun x -> (kleisli x).Sample(gen)) state
    
    type StrictSequenceVar<'T>(vs: RVar<'T> seq) =
        interface RVar<'T seq> with
            override this.Sample rsource =
                Seq.fold(fun xs (x: RVar<'T>) -> List.Cons(x.Sample rsource,xs)) List.Empty vs :> 'T seq

    type TakeVar<'T>(v : RVar<'T>, count : int) =
        interface RVar<'T array> with
            override this.Sample rsource = Array.init count (fun _ -> v.Sample(rsource))

    type WhileVar<'T>(v : RVar<'T>, pred : 'T -> bool) =
        interface RVar<'T> with
            override this.Sample rsource = 
                let mutable result = v.Sample rsource
                while(pred result) do result <- v.Sample rsource
                result
    
    //This seems to be as much as you can do with random computations. Yield cannot differ meaningfully
    //from return so it is not provided. No useful zero exists.
    type RandomlyBuilder() =
        interface IRandomlyBuilder with
            member this.Bind(v, f) = BindVar(v,f) :> RVar<'T>
            member this.Return(v) = ConstVar(v) :> RVar<'T>
            member this.ReturnFrom(v) = v
            member this.For(vs, f) = Spark(SequenceVar(Seq.map f vs)) :> RVar<'T seq>
            //member this.Combine(v1, v2) = CombineVar(v1, v2) :> RVar<'T>