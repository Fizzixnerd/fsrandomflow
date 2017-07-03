namespace fsrandomflow

///The underlying RVar abstract syntax tree
module RVarAST =
    open Twister
    open System.Collections.Generic

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

