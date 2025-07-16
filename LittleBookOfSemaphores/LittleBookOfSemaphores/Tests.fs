module Tests

open System
open Xunit

[<Fact>]
let ``Rendevous1`` () =
    let x = BasicSynchronisationPatterns.rendevous.Result
    ()

[<Fact>]
let ``Rendevous2`` () =
    let x = BasicSynchronisationPatterns.rendevous2.Result
    ()
