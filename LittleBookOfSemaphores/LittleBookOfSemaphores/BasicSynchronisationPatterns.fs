module BasicSynchronisationPatterns
open System
open System.Threading
open System.Threading.Tasks

let rendevous () = task {
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

let rendevous2 () = task {
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

let mutex () = task {
    use semaphore1 = new SemaphoreSlim(1)

    let a = task {
        do! semaphore1.WaitAsync ()
        Console.WriteLine "a"
        let _ = semaphore1.Release ()
        return ()
    }

    let b = task {
        do! semaphore1.WaitAsync ()
        Console.WriteLine "b"
        let _ = semaphore1.Release ()
        return ()
    }

    let! _ = Task.WhenAll [ a; b ]

    return ()
}

// Blocks thread
let mutex2 () = task {
    let lockObj = Object()
    let t1 = Task.Run (fun () -> lock lockObj (fun () -> Console.WriteLine "a"))
    let t2 = Task.Run (fun () -> lock lockObj (fun () -> Console.WriteLine "b"))
    let! _ = Task.WhenAll [ t1; t2 ]
    return ()
}

let barrier () = task {
    let mutable count = 0
    use semaphore = new SemaphoreSlim(0)
    let nThreads = 5

    let barr () = task {
        let count = Interlocked.Increment(&count)
        if count = nThreads then
            ignore (semaphore.Release(count))
        Console.WriteLine("Waiting")
        do! semaphore.WaitAsync()
        Console.WriteLine("Critical section")
        return ()
    }

    let! _ =
        Task.WhenAll
            [
                barr ()
                barr ()
                barr ()
                barr ()
                barr ()
            ]
    return ()
}


