//Copyright 2017 Barend Venter
//Licensed under the MIT license, see LICENSE

namespace FsRandomFlow

module RVar =
    open System.Collections.Generic
    open Twister
    open System
    open RVarAST

    let randomly = RandomlyBuilder() :> IRandomlyBuilder

    let StdUniform = StdUniformClass() :> RVar<int>

    let runrvar seed (rvar : RVar<'T>) = rvar.Sample(twister(seed).GetEnumerator())
    
    //Note: if you use System.DateTime.Now.Ticks directly
    // then F# will emit a compiler warning
    //This warning warns us about something that we want anyway
    // (that we won't be able to set the Ticks property) so
    // use the spurious let to supress the warning.
    let runrvarIO rvar = 
        runrvar (int(let x = System.DateTime.Now in x.Ticks)) rvar
    
    let map f v = MappedVar(v,f) :> RVar<'T>

    let apply vv vf = ApplyVar(vf,vv) :> RVar<'T>

    let concatMap f v = BindVar(v,f) :> RVar<'T>

    let repeat v = Streaming(v) :> RVar<'T seq>

    let constant v = ConstVar(v) :> RVar<'T>

    let take count v = TakeVar(v,count) :> RVar<'T array>
    
    //2 divides evenly into a 32 bit integer, so no correction
    //is needed here.
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

    let sequence (xs : RVar<'T> seq) = map Array.ofSeq (SequenceVar(xs))
    
    let sequenceStreaming (xs : RVar<'T> seq) = 
        Spark(SequenceVar(xs)) :> RVar<'T seq>

    //Parallel evaluation:

    let sequenceParallel (xs: RVar<'T> seq) = randomly {
            let! seeds = repeat StdUniform
            let pending = Seq.zip seeds xs |> Seq.toArray
            return Array.Parallel.map(fun (seed, (x: RVar<'T>)) -> x.Sample(twister(seed).GetEnumerator())) pending
        }

    let takeParallel count xs = sequenceParallel (Seq.replicate count xs) 

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

    //[0,1], [0,1), (0,1], (0,1) intervals:

    let UniformZeroToOne = randomly {
            let max = float System.Int32.MaxValue
            let! actual = StdUniform
            return float actual / max
        }

    let UniformZeroToBelowOne = filter (fun x -> x < 1.0) UniformZeroToOne
    
    let UniformAboveZeroToOne = filter (fun x -> x < 1.0) UniformZeroToOne

    let UniformAboveZeroBelowOne = filter (fun x -> x > 0.0 && x < 1.0) UniformZeroToOne
    
    let UniformInterval(minVal, maxVal, minOpen, maxOpen) = randomly {
            let! outcome = if(minOpen) then if(maxOpen) then UniformAboveZeroBelowOne
                                            else UniformAboveZeroToOne
                           else if(maxOpen) then UniformZeroToBelowOne
                                else UniformZeroToOne
            let intervalSize = maxVal - minVal
            return intervalSize * outcome + minVal
        }
    
    let UniformIntervalClosed(x, y) = UniformInterval(min x y, max x y, false, false)

    let UniformIntervalOpen(x, y) = UniformInterval(min x y, max x y, true, true)

    let probability p = 
        if p >= 1.0 then constant true
        else if p <= 0.0 then constant false
        else randomly {
            let! outcome = UniformZeroToOne
            return outcome < p
        }

    let oneOf xs = randomly {
            let! i = UniformZeroAndUpBelow(Seq.length xs)
            return Seq.item i xs
        }
    
    let oneOfWeighted xs = 
            let scrubbedXs = Seq.filter(fun (x,_) -> x > 0.0) xs
            if Seq.isEmpty scrubbedXs then raise (invalidArg "xs" "No positively weighted operands were provided")
            else let certainty = Seq.sumBy(fst) scrubbedXs
                 let rec findOutcome ix acc ys = 
                     let acc' = acc + fst (Seq.head ys)
                     if ix <= acc' then snd (Seq.head ys)
                     else findOutcome ix acc' (Seq.tail ys)
                 randomly {
                     let! outcome = UniformInterval(0.0,certainty,false,true) //[0.0,certainty)
                     return findOutcome outcome 0.0 scrubbedXs
                 }
                 
    let union xs = randomly {
            let! (x : RVar<'T>) = oneOf xs
            return! x
        }

    let unionWeighted xs = randomly {
            let! (x : RVar<'T>) = oneOfWeighted xs
            return! x
        }

    let shuffle xs = 
            let rec doShuffle i (arr : 'T []) = randomly {
                    let! outcome = UniformZeroAndUpBelow (arr.Length - i)
                    let me = arr.[i]
                    arr.[i] <- arr.[i+outcome]
                    arr.[i+outcome] <- me
                    if i < arr.Length - 2 then return! doShuffle (i+1) arr
                    else return Array.ofSeq arr
                }
            doShuffle 0 (Array.ofSeq xs)

    let tableRow (arr : 'T [,]) = randomly {
            let y = arr.GetLength(0)
            let x = arr.GetLength(1)
            let! r = UniformZeroAndUpBelow y
            return 0
                   |> Seq.unfold(fun i ->
                       if i < x then Some(arr.[r,i], i+1)
                       else None)
        }

    let tableColumn (arr : 'T [,]) = randomly {
            let y = arr.GetLength(0)
            let x = arr.GetLength(1)
            let! c = UniformZeroAndUpBelow x
            return 0
                   |> Seq.unfold(fun i ->
                       if i < y then Some(arr.[i,c], i+1)
                       else None)
        }

    let tableRowWise (arr : 'T [,]) = randomly {
                let y = arr.GetLength(0)
                let x = arr.GetLength(1)
                for i in [0 .. x-1] do
                      let! j = UniformZeroAndUpBelow y
                      return arr.[j,i]
            }

    let tableColumnWise (arr : 'T [,]) = randomly {
                let y = arr.GetLength(0)
                let x = arr.GetLength(1)
                for j in [0 .. y-1] do
                    let! i = UniformZeroAndUpBelow x
                    return arr.[j,i]
            }

    let standardNormalPdf x = (Math.E ** (-0.5 * (x ** 2.0))) / Math.Sqrt(2.0 * Math.PI)
    
    let standardNormalInversePdf x = Math.Sqrt((-2.0) *  Math.Log(Math.Sqrt(2.0*Math.PI) * x))

    let rec ziggarut layers pdf fallback = randomly {
            let xsub i = fst (Seq.item i layers)
            let ysub i = snd (Seq.item i layers)
            let! i = UniformInt(0,(Seq.length layers)-2)
            let! U0 = UniformZeroToBelowOne
            let x = xsub i * U0
            if x < xsub (i+1) then return x
            else let! U1 = UniformZeroToBelowOne
                 let y = ysub i + U1 * (ysub (i+1) - ysub i)
                 if i = 0 then return! fallback
                 else if y < pdf(x) then return x
                      else return! ziggarut layers pdf fallback
        }

    let rec normalFallback(x1,y1) = randomly {
            let! U1 = UniformZeroToBelowOne
            let! U2 = UniformZeroToBelowOne
            let x = -Math.Log(U1)/x1
            let y = -Math.Log(U2)
            if x**2.0 < 2.0*y then return x + x1
            else return! normalFallback(x1,y1)
        }

//While the ziggarut algorithm itself written, generating a ziggarut table is beyond me.
//For now, use Box-Mueller Transform:

    let boxMueller = randomly {
            let! U1 = UniformAboveZeroBelowOne
            let! U2 = UniformAboveZeroBelowOne
            return Math.Sqrt(-2.0*Math.Log(U1))*Math.Cos(2.0*Math.PI*U2)
        }


//TODO: Use ziggarut
    let StandardNormal = boxMueller
//        ziggarut normalZiggarutSteps standardNormalPdf (normalFallback(normalZiggarutSteps.[1]))
//        |> concatMap RandomlySignedDouble

    let Normal(mean,stdev) = map (fun outcome -> mean + stdev * outcome) StandardNormal

    let Exponential(inverseScale) = map (fun x -> (-Math.Log(x))/inverseScale) UniformAboveZeroBelowOne

    let Weibull(k,lambda) = map (fun x -> lambda * (Math.Pow(-Math.Log(x),(1.0/k)))) UniformAboveZeroBelowOne
