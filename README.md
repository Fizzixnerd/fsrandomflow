#fsrandomflow

##Introduction

**fsrandomflow** allows random computations to be expressed without explicitly sampling a random generator until the end. This means that you can seed the generator once you are ready to run the full computation.

The provided random computations are built to be entirely deterministic on the original seed. By combining the original generators (and obviously, avoiding IO), any random variable a user creates should have this same deterministic property.

Unlike ```System.Random```, a random variable can only produce one type of value, but the type is completely generic and operations like ```map``` are supported.

A workflow builder is provided to give satisfying syntax to combine 
This F# library is intended as a generic and thread-safe alternative to System.Random, which supports operations such as "map".

##Example

Say you want to roll an attribute score in D&D 5th edition, and your DM is very generous and wants everyone to have high stats. She tells everyone that if they roll a 1, they are allowed to reroll, and they roll a fourth die, which allows them to drop their lowest roll entirely. ***fsrandomflow*** lets this be expressed as an ```RVar<int>```:

```F#
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
```