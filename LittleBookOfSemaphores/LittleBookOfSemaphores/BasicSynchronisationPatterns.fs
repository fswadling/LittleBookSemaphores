module BasicSynchronisationPatterns
open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent

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
    use turnstile2 = new SemaphoreSlim(0)

    let thread () = task {
        for n in [1..10] do
            do! lockSemaphore.WaitAsync()
            count <- count + 1
            if (count = nThreads) then
                do ignore (turnstile1.Release(nThreads))
            do lockSemaphore.Release() |> ignore

            do! turnstile1.WaitAsync ()

            Console.WriteLine($"CriticalSection {n}")

            do! lockSemaphore.WaitAsync()
            count <- count - 1
            if count = 0 then
                do ignore (turnstile2.Release(nThreads))
            do lockSemaphore.Release() |> ignore

            do! turnstile2.WaitAsync ()
        }

    let! _ = 
        [1..nThreads]
        |> List.map (fun _ -> thread ())
        |> Task.WhenAll

    return ()
}

//Blocking
let twoPhaseBarrier2 () = task {

    let nThreads = 5
    use turnstile1 = new Barrier(nThreads)
    use turnstile2 = new Barrier(nThreads)

    let thread () = 
        for n in [1..10] do
            do turnstile1.SignalAndWait()
            Console.WriteLine($"CriticalSection {n}")
            do turnstile2.SignalAndWait()
    
    let! _ =
        [1..nThreads]
        |> List.map (fun _ -> Task.Run(thread))
        |> Task.WhenAll

    return ()
}

let leadersAndFollowers () = task {
    use leaderQueue = new SemaphoreSlim(0)
    use followerQueue = new SemaphoreSlim(0)
    use mutex = new SemaphoreSlim(1)
    use rendevous = new SemaphoreSlim(0)

    let mutable leaders = 0
    let mutable followers = 0

    let leaderThread () = task {
        do! mutex.WaitAsync()
        if (followers > 0) then
            followers <- followers - 1
            followerQueue.Release() |> ignore
        else
            leaders <- leaders + 1
            mutex.Release () |> ignore
            do! leaderQueue.WaitAsync ()

        Console.WriteLine("Dance leader")

        do! rendevous.WaitAsync()
        do mutex.Release() |> ignore
        return ()
    }

    let followerThread () = task {
        do! mutex.WaitAsync()
        if (leaders > 0) then
            leaders <- leaders - 1
            leaderQueue.Release() |> ignore
        else
            followers <- followers + 1
            mutex.Release () |> ignore
            do! followerQueue.WaitAsync()

        Console.WriteLine("Dance follower")

        do rendevous.Release () |> ignore
    }

    let nLeaders = 10
    let nFollowers = 10

    // Interleave leader and follower threads
    let leaderTasks =
        [1..nLeaders]
        |> List.map (fun i -> leaderThread ())

    let followerTasks =
        [1..nFollowers]
        |> List.map (fun i -> followerThread())

    let! _ = Task.WhenAll(leaderTasks @ followerTasks)
    Console.WriteLine("All pairs danced.")
    return ()
}

type Dancer =
    | Leader of int
    | Follower of int

let leadersAndFollowers2 () = task {
    let mp = 
        MailboxProcessor.Start(fun inbox ->
            let rec loop (waitingLeaders: int list) (waitingFollowers: int list) =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Leader lid ->
                        match waitingFollowers with
                        | fid :: rest ->
                            Console.WriteLine($"Leader {lid} dancing with Follower {fid}")
                            return! loop waitingLeaders rest
                        | [] ->
                            return! loop (lid :: waitingLeaders) waitingFollowers

                    | Follower fid ->
                        match waitingLeaders with
                        | lid :: rest ->
                            Console.WriteLine($"Follower {fid} dancing with Leader {lid}")
                            return! loop rest waitingFollowers
                        | [] ->
                            return! loop waitingLeaders (fid :: waitingFollowers)
                }

            loop [] []
        )

    let n = 10
    let rand = Random()

    let launchDancer isLeader id = task {
        do! Task.Delay(rand.Next(10, 100)) // Simulate async arrival
        if isLeader then
            mp.Post(Leader id)
        else
            mp.Post(Follower id)
    }

    let dancers =
        [ for i in 1..n -> launchDancer true i ] @
        [ for i in 1..n -> launchDancer false i ]

    let! _ = Task.WhenAll dancers
    return ()
}

let leadersAndFollowersBlocking () =

    use waitingLeaders = new BlockingCollection<int>()
    use waitingFollowers = new BlockingCollection<int>()

    let leaderThread (id: int) =
        let thread = Thread(ThreadStart(fun () ->
            // Try to get a follower if one is waiting
            let matchedFollowerId =
                match waitingFollowers.TryTake() with
                | true, fid -> fid
                | false, _ ->
                    // No follower waiting; add self to leader queue and wait
                    waitingLeaders.Add(id)
                    let fid = waitingFollowers.Take() // blocks
                    fid

            Console.WriteLine($"Leader {id} dancing with Follower {matchedFollowerId}")
        ))
        thread.Start()
        thread

    let followerThread (id: int) =
        let thread = Thread(ThreadStart(fun () ->
            // Try to get a leader if one is waiting
            let matchedLeaderId =
                match waitingLeaders.TryTake() with
                | true, lid -> lid
                | false, _ ->
                    // No leader waiting; add self to follower queue and wait
                    waitingFollowers.Add(id)
                    let lid = waitingLeaders.Take() // blocks
                    lid

            Console.WriteLine($"Follower {id} dancing with Leader {matchedLeaderId}")
        ))
        thread.Start()
        thread

    // Launch dancers
    let num = 10
    let leaderThreads = [ for i in 1..num -> leaderThread i ]
    let followerThreads = [ for i in 1..num -> followerThread i ]

    // Wait for all to finish
    for t in leaderThreads @ followerThreads do t.Join()

    Console.WriteLine("All dances completed.")