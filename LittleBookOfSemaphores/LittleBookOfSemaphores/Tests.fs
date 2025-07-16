module Tests

open System
open Xunit

[<Fact>]
let ``Rendevous1`` () =
    let x = (BasicSynchronisationPatterns.rendevous ()).Result
    ()

[<Fact>]
let ``Rendevous2`` () =
    let x = (BasicSynchronisationPatterns.rendevous2 ()).Result
    ()

[<Fact>]
let ``Mutex1`` () =
    let x = (BasicSynchronisationPatterns.mutex ()).Result
    ()

[<Fact>]
let ``Mutex2`` () =
    let x = (BasicSynchronisationPatterns.mutex2 ()).Result
    ()
