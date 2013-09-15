module Ports

open System
open System.Threading

let ports_lock = new Semaphore(1,1)
let mutable ports :int Set = Set.empty

let release_port port = 
    ports_lock.WaitOne() |> ignore
    ports <- Set.remove port ports
    ports_lock.Release() |> ignore
    
let rnd = new Random(DateTime.Now.Millisecond)

//passive ports
//let LOW_PORT = 500
//let HIGH_PORT = 65235

let random_port low high = rnd.Next(low,high)
    
let rec iter_ports low high = 
    let port = random_port low high
    if Set.contains port ports then iter_ports low high
    else 
        ports <- Set.add port ports
        port

let obtain_port low high =
    ports_lock.WaitOne() |> ignore
    let port = iter_ports low high
    ports_lock.Release() |> ignore
    port
