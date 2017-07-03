namespace fsrandomflow

open System.Collections.Generic

[<Interface>]
///A random variable that can be sampled as needed
type RVar<'T> =
    ///Using a stream of random values, generate an example value of type 'T
    abstract member sample : IEnumerator<int> -> 'T

