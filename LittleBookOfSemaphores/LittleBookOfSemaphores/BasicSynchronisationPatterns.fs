module BasicSynchronisationPatterns
open System
open System.Threading
open System.Threading.Tasks

let rendevous = task {
    use semaphoreA = new SemaphoreSlim(0)
    use semaphoreB = new SemaphoreSlim(0)

    let a = task {
        Console.WriteLine "a1"
        let _ = semaphoreB.Release();
        let! a = semaphoreA.WaitAsync ()
        Console.WriteLine "a2"
    }

    let b = task {
        Console.WriteLine "b1"
        let _ = semaphoreA.Release();
        let! b = semaphoreB.WaitAsync ()
        Console.WriteLine "b2"
    }

    let! _ = Task.WhenAll [ a; b ]

    return ()
}

let rendevous2 = task {
    let source1 = new TaskCompletionSource()
    let source2 = new TaskCompletionSource()

    let a = task {
        Console.WriteLine "a1"
        do source2.SetResult ();
        do! source1.Task
        Console.WriteLine "a2"
    }

    let b = task {
        Console.WriteLine "b1"
        do source1.SetResult ();
        do! source2.Task
        Console.WriteLine "b2"
    }

    let! _ = Task.WhenAll [ a; b ]

    return ()
}