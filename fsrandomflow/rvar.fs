//Copyright 2017 Barend Venter
//Licensed under the MIT license, see LICENSE

namespace fsrandomflow

module RVar =
    open System.Collections.Generic
    open Twister
    open System
    open RVarAST

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
        
    let probability p = 
        if p >= 1.0 then constant true
        else if p <= 0.0 then constant false
        else randomly {
            let! outcome = UniformZeroToOne
            return outcome < p
        }

    let standardNormalPdf x = (Math.E ** (0.5 * (x * x))) / Math.Sqrt(2.0 * Math.PI)

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

    //Rather than implementing the probit function, use a precomputed table
    let normalZiggarutStepsRaw = 
        [| (0.0,0.0);(0.003906250,0.009791673);(0.00781250,0.01958429);(0.01171875,0.02937878);(0.01562500,0.03917609);(0.01953125,0.04897716)
           ;(0.02343750,0.05878294);(0.02734375,0.06859437);(0.03125000,0.07841241);(0.03515625,0.08823802);(0.03906250,0.09807215);(0.04296875,0.10791578)
           ;(0.0468750,0.1177699);(0.05078125,0.12763542);(0.0546875,0.1375134);(0.05859375,0.14740482);(0.0625000,0.1573107);(0.06640625,0.16723201)
           ;(0.0703125,0.1771698);(0.07421875,0.18712516);(0.0781250,0.1970991);(0.08203125,0.20709265);(0.0859375,0.2171069);(0.08984375,0.22714306)
           ;(0.0937500,0.2372021);(0.09765625,0.24728522);(0.1015625,0.2573935);(0.1054688,0.2675282);(0.1093750,0.2776904);(0.1132812,0.2878814)
           ;(0.1171875,0.2981024);(0.1210938,0.3083546);(0.1250000,0.3186394);(0.1289062,0.3289579);(0.1328125,0.3393116);(0.1367188,0.3497018)
           ;(0.1406250,0.3601299);(0.1445312,0.3705973);(0.1484375,0.3811055);(0.1523438,0.3916559);(0.1562500,0.4022501);(0.1601562,0.4128896)
           ;(0.1640625,0.4235761);(0.1679688,0.4343112);(0.1718750,0.4450965);(0.1757812,0.4559339);(0.1796875,0.4668251);(0.1835938,0.4777720)
           ;(0.1875000,0.4887764);(0.1914062,0.4998403);(0.1953125,0.5109658);(0.1992188,0.5221549);(0.2031250,0.5334097);(0.2070312,0.5447325)
           ;(0.2109375,0.5561256);(0.2148438,0.5675913);(0.2187500,0.5791322);(0.2226562,0.5907507);(0.2265625,0.6024495);(0.2304688,0.6142313)
           ;(0.234375,0.626099);(0.2382812,0.6380556);(0.2421875,0.6501041);(0.2460938,0.6622477);(0.2500000,0.6744898);(0.2539062,0.6868337)
           ;(0.2578125,0.6992833);(0.2617188,0.7118422);(0.2656250,0.7245144);(0.2695312,0.7373040);(0.2734375,0.7502154);(0.2773438,0.7632530)
           ;(0.2812500,0.7764218);(0.2851562,0.7897265);(0.2890625,0.8031726);(0.2929688,0.8167654);(0.2968750,0.8305109);(0.3007812,0.8444151)
           ;(0.3046875,0.8584845);(0.3085938,0.8727259);(0.3125000,0.8871466);(0.3164062,0.9017541);(0.3203125,0.9165567);(0.3242188,0.9315628)
           ;(0.3281250,0.9467818);(0.3320312,0.9622232);(0.3359375,0.9778975);(0.3398438,0.9938159);(0.34375,1.00999);(0.3476562,1.0264331)
           ;(0.3515625,1.0431583);(0.3554688,1.0601805);(0.359375,1.077516);(0.3632812,1.0951807);(0.3671875,1.1131943);(0.3710938,1.1315766)
           ;(0.375000,1.150349);(0.3789062,1.1695366);(0.3828125,1.1891644);(0.3867188,1.2092612);(0.390625,1.229859);(0.3945312,1.2509917)
           ;(0.3984375,1.2726986);(0.4023438,1.2950224);(0.406250,1.318011);(0.4101562,1.3417178);(0.4140625,1.3662038);(0.4179688,1.3915375)
           ;(0.421875,1.417797);(0.4257812,1.4450726);(0.4296875,1.4734676);(0.4335938,1.5031029);(0.437500,1.534121);(0.4414062,1.5666886)
           ;(0.4453125,1.6010087);(0.4492188,1.6373254);(0.453125,1.675940);(0.4570312,1.7172281);(0.4609375,1.7616704);(0.4648438,1.8098922)
           ;(0.468750,1.862732);(0.4726562,1.9213508);(0.4765625,1.9874279);(0.4804688,2.0635279);(0.484375,2.153875);(0.4882812,2.2662268)
           ;(0.4921875,2.4175590);(0.4960938,2.6600675) |]

    let normalZiggarutSteps = 
        normalZiggarutStepsRaw
        |> Array.rev
        |> Array.map(fun (y,x) -> (x,0.5-y))

    let StandardNormal = 
        ziggarut normalZiggarutSteps standardNormalPdf (normalFallback(normalZiggarutSteps.[1]))
        |> concatMap RandomlySignedDouble

    let Normal(mean,stdev) = map (fun outcome -> mean + stdev * outcome) StandardNormal

    let Exponential(inverseScale) = map (fun x -> (-Math.Log(x))/inverseScale) UniformAboveZeroBelowOne

//    let StandardNormal =
//        UniformZeroToOne
//        |> map (fun x -> x * 6.0)
//        |> filterRandomly(fun y -> probability(standardNormalPdf y))
//        |> concatMap RandomlySignedDouble