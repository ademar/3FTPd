module Program

open Web
open System
open Suave.Web

open Suave.Data

open Config
open Common
open Data
open Runtime

let mkcn _ = open_database ()

let admin_console = web_server [|HTTP,"127.0.0.1",8080|] (myapplication mkcn)

let db_authentication_provider (client: FtpClient) : (FtpClient) Option= 
    let query _ = 
        let tx = sql (mkcn ())
        tx { 
            let! p = tx.Query "SELECT username,password,homedir FROM User WHERE username=%s and password=%s" client.User (encrypt(client.Password))
            return p }
    
    let result = query ()
    match result with 
    |Some(_,_,homedir) -> client.HomeDirectory <- homedir
                          Some(client)
    |None -> None

let main _ = 
    let cn = mkcn ()
    async {
        
        Suave.Log.log "3FTPd service starting as: %s"  (Environment.UserName)

        sites cn |> Array.iter(fun x -> start x Config.serverCertificate db_authentication_provider)

        do! admin_console 
    } 

let stop _ = Map.iter (fun k _ -> Runtime.stop k ) runningSites
 
//to run on the shell
main () |> Async.RunSynchronously |> ignore

