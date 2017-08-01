//Copyright 2017 Barend Venter
//This code liscensed under the MIT license, see LICENSE

namespace FsRandomFlow
    //TODO: More informative interface than IEnumerator/IEnumerable
    //[<Interface>]
    //type ITwister = 
    //    member freeze : TwisterFrozen
    //    member thaw : TwisterThawed
    //    member peek : int
    //    member branchWithID : int -> TwisterFrozen
    //and
    //    [<Class>]
    //    TwisterThawed = 
    //        interface ITwister
    //        member branch : TwisterFrozen
    //        member next : int
    //and 
    //    [<Class>]
    //    TwisterFrozen = 
    //        interface ITwister
    //        member next : int * TwisterFrozen

    module Twister =
        val twister : int -> int seq