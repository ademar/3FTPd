module Program

open Web
open System
open Suave.Web

open Data
open Runtime

let admin_console = web_server [|HTTP,"127.0.0.1",8080|] myapplication

let main _ = async {
        
        Suave.Log.log "3FTPd service starting as: %s"  (Environment.UserName)

        sites () |> Array.iter(fun x -> start x)

        do! admin_console 
    } 

let stop _ = Map.iter (fun k _ -> Runtime.stop k ) runningSites
 
//to run on the shell
main () |> Async.RunSynchronously |> ignore

