//Copyright 2017, Barend Venter
//This code is provided under the MIT license, see LICENSE

namespace FsRandomFlow

open System.Collections.Generic

[<Interface>]
///A random variable that can be sampled as needed
type RVar<'T> =
    ///Using a stream of random values, generate an example value of type 'T
    abstract member Sample : IEnumerator<int> -> 'T


[<Interface>]
type IRandomlyBuilder =
    abstract member Bind : RVar<'T> * ('T -> RVar<'U>) -> RVar<'U>
    abstract member Return : 'T -> RVar<'T>
    abstract member ReturnFrom : RVar<'T> -> RVar<'T>
    abstract member For : 'T seq * ('T -> RVar<'U>) -> RVar<'U seq>
    //abstract member Combine : RVar<unit> * RVar<'T> -> RVar<'T>