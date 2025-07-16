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

[<Fact>]
let Barrier () = 
    let x = (BasicSynchronisationPatterns.barrier ()).Result
    ()

[<Fact>]
let Barrier2 () =
    let x = (BasicSynchronisationPatterns.barrier2 ()).Result
    ()

[<Fact>]
let TwoPhaseBarrier () =
    let x = (BasicSynchronisationPatterns.twoPhaseBarrier ()).Result
    ()

[<Fact>]
let TwoPhaseBarrier2() =
    let x = (BasicSynchronisationPatterns.twoPhaseBarrier2 ()).Result
    ()

[<Fact>]
let Queue1() =
    let x = (BasicSynchronisationPatterns.leadersAndFollowers ()).Result
    ()

[<Fact>]
let Queue2() =
    let x = (BasicSynchronisationPatterns.leadersAndFollowers2 ()).Result
    ()

[<Fact>]
let Queue3() =
    let x = (BasicSynchronisationPatterns.leadersAndFollowersBlocking ())
    ()