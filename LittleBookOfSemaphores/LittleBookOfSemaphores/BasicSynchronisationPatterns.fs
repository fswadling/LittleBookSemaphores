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

// Thread blocking
let barrier2 () = task {
    use barrier = new Barrier(participantCount = 5)

    let barr id = Task.Run(fun () ->
        Console.WriteLine($"Thread {id} waiting at barrier")
        barrier.SignalAndWait()
        Console.WriteLine($"Thread {id} passed the barrier")
    )

    let! _ = 
        [ 1..5 ]
        |> List.map barr
        |> Task.WhenAll

    return ()
}

let twoPhaseBarrier () = task {
    let mutable count = 0
    let nThreads = 5
    let lockSemaphore = new SemaphoreSlim(1)
    use turnstile1 = new SemaphoreSlim(0)
    use turnstile2 = new SemaphoreSlim(1)

    let thread () = task {
        for n in [1..10] do
            do! lockSemaphore.WaitAsync()
            count <- count + 1
            if (count = nThreads) then
                do! turnstile2.WaitAsync()
                do ignore (turnstile1.Release(nThreads))
            do lockSemaphore.Release() |> ignore

            do! turnstile1.WaitAsync ()

            Console.WriteLine($"CriticalSection {n}")

            do! lockSemaphore.WaitAsync()
            count <- count - 1
            if count = 0 then
                do ignore (turnstile2.Release(nThreads + 1))
            do lockSemaphore.Release() |> ignore

            do! turnstile2.WaitAsync ()
        }

    let! _ = 
        [1..nThreads]
        |> List.map (fun _ -> thread ())
        |> Task.WhenAll

    return ()
}
