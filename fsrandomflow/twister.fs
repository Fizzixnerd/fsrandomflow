//Copyright (c) 2017 Barend Venter
//This code liscensed under the MIT license, see LICENSE

namespace fsrandomflow

module Twister =

    //TODO: Implement Matsumoto&Nishimura's dynamic creation, for generating new twisters
    // with coprime characteristic polynomials to prevent (as far as possible) spurious 
    // correlations in unrelated parts of simulations when a random generator branches

    open System
    open System.IO
    open System.Collections.Generic
    open System.Collections

    //32 bit version

    //From wikipedia - heavily based on the Haskell example there

    ///Word Size
    let w = 32
    ///Degree of Recurrence
    let n = 624
    ///Middle word
    let m = 397
    ///Separation point
    let r = 31
    ///Coefficients of the rational normal form twist matrix
    let a = 0x9908B0DF
    ///TGFSR(R) Tempering Bitmask
    let b = 0x9D2C5680
    ///TGFSR(R) Tempering Bitmask
    let c = 0xEFC60000
    ///TGFSR(R) Tempering Bit Shift
    let s = 7
    ///TGFSR(R) Tempering Bit Shift
    let t = 15
    ///Mersenne Twister Tempering Bit Mask
    let d = 0xFFFFFFFF
    ///Mersenne Twister Tempering Bit Shift
    let u = 11
    ///Mersenne Twister Tempering Bit Shift
    let l = 18
    ///Additional parameter used in state initialization
    let f = 1812433253

    // These will be used to create a streaming defn', w/ buffer
    let nextState mword word0 word1 = 
        let upper x = (x >>> r) <<< r
        let lower x = (x <<< r) >>> r
        let multA x = if (x &&& 1) = 0 then (x >>> 1) else (x >>> 1) ^^^ a
        mword ^^^ multA (upper(word0) ||| lower(word1))

    let getValue lword = 
        let mutable x = lword
        x <- x ^^^ ((x >>> u) &&& d)
        x <- x ^^^ ((x <<< s) &&& b)
        x <- x ^^^ ((x <<< t) &&& c)
        x ^^^ (x >>> l)
      
    // Initializes the state array for mersenne twister
    let initSeq seed = 
        (seed,0)
        |> Seq.unfold(
            fun (prev,i) ->
                if i < n
                then
                    let next = f * (prev ^^^ (prev >>> (w-2))) + i
                    Some(next, (next,i+1))
                else None
                )
        |> Seq.toArray

    // Resets an array in place
    let resetBuffer seed (arr : int array) = 
        (seed,0)
        |> Seq.unfold(
            fun (prev,i) ->
                    if i < n
                    then
                        let next = f * (prev ^^^ (prev >>> (w-2))) + i
                        Some(next, (next,i+1))
                    else None
                )
        |> Seq.iteri(fun v i -> arr.[i] <- v)

    // A proper modulo, since F# % is the remainder function
    let modulus x m =
        if x > 0 then x % m
        else (m + (x % m)) % m

    // This is the write head for a particular mersenne twister thread
    type TwistGen(seed) =
        let state = initSeq seed
        let mutable head = 0
        member this.ixRelative offset = 
            modulus (head + offset) n
        member this.incrHead() = head <- this.ixRelative 1
        member this.writeState(newStateByte) =
           state.[head] <- newStateByte
           this.incrHead()
        member this.Current = 
               let il = this.ixRelative (-1)
               getValue (state.[il])
        member this.advance() =
               let i0 = this.ixRelative 0
               let i1 = this.ixRelative 1
               let im = this.ixRelative m
               let nstate = nextState (state.[im]) (state.[i0]) (state.[i1])
               this.writeState(nstate)
        interface IDisposable with
            override this.Dispose () = ()
        interface IEnumerator with
            override this.get_Current () = this.Current :> obj
            override this.MoveNext() = 
               this.advance()
               true
            override this.Reset() =
               resetBuffer seed state
               head <- 0
        interface IEnumerator<int> with 
            override this.get_Current () = this.Current

    type Twister(seed) =
        interface IEnumerable with
            member this.GetEnumerator() =
                new TwistGen(seed) :> IEnumerator
        interface IEnumerable<int> with
            member this.GetEnumerator() =
                new TwistGen(seed) :> IEnumerator<int>

    let twister seed = Twister(seed) :> IEnumerable<int>