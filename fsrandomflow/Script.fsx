// Copyright 2017 Barend Venter
// This code is licensed under the MIT license, see LICENSE

//Highlight this and right click "run in interactive" to open the library
#load "twister.fs"
#load "types.fs"
#load "RVarAST.fs"
#load "RVar.fs"
open FsRandomFlow
open FsRandomFlow.RVar

///Roll 4 d6, reroll the 1s, drop the lowest
let dnd5eAttrRollHouseRules = randomly {
        //A six-sided die
        let d6 = RVar.UniformInt(1,6)
        //A six-sided die that is rerolled if it lands on 1
        let d6rerolling1 = randomly {
                //Roll a 6 sided die
                let! result = d6
                //If it didn't land on 1, return the result
                if result > 1 then return result
                //Else, roll another d6 and return the result
                else return! d6
            }
        //Roll 4 six-sided die, rerolling the 1s
        let! initialRolls = RVar.take 4 d6rerolling1
        //Drop the lowest die
        let threeBestRolls = Array.sort initialRolls |> Seq.tail
        //Return the sum
        return Seq.sum threeBestRolls
    }

///Get a jagged random area underneath a triangle
let jaggedSubarea = randomly {
        for x in [1 .. 10] do return! RVar.UniformInt(1,x)
    }

RVar.runrvar 1256343118 <| RVar.take 12 (RVar.oneOfWeighted (Seq.zip [1.0..10.0] [1..10]));;

RVar.runrvar 1023092 <| RVar.oneOfWeighted (Seq.zip [55.0; 1.0; 2.0] [1; 2; 3])

RVar.runrvar 1023092 <| RVar.oneOfWeighted (Seq.zip [10.0; 50.0] [1; 2])

Seq.map