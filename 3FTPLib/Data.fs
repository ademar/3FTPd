module Data

open Suave.Data
open Suave.Json
//open Config
open Model
open System

let newuid _ = Guid.NewGuid().ToString()

//i could have a function that i pass the connection and returns a record with all these functions ?

let users cn =
    let tx = sql cn
    tx.Enum "SELECT * FROM User"
    |> Seq.toArray<User>
    |> Array.map ( fun x -> { x with password = "" } )
    //NOTE: ^ u don't want to transmit the passwords over the wire

let anonymous_data cn = 
    let tx = sql cn
    let q = 
        tx { 
             let! p = tx.Query "SELECT parameterValue FROM Config where parameterName=%s" "anonymousEnabled" 
             return p
        }
    let r = match q with 
            |Some(s) -> if s.Equals("true") then "enabled" else "disabled" 
            |_ -> failwith "Key anonymousEnabled is missing from the Config table."
    toJson<Status>({ status = r })

let site cn id :  Site option =
    let tx = sql cn
    tx { 
        let! p =  tx.Query "SELECT * FROM Site WHERE id=%s" (id.ToString()) 
        return p
    }

let sites cn =
    let tx = sql cn
    tx.Enum "SELECT id,name,ipaddress FROM Site"
    |> Seq.toArray<Guid*string*string>
    |> Array.map (fun (id,name,ipaddress) -> { id = id ;name = name; ipaddress = ipaddress;  status = ""} )

let users_data cn = (toJson<User array>(users cn))

let buildCommand cn p = 
    let tx = sql cn in tx.Query p

open Common

let newuser cn (user:User) = 
    buildCommand cn "INSERT INTO User (id,username,password,homeDir) VALUES(%s,%s,%s,%s)" (newuid()) user.username (encrypt(user.password)) user.homeDirectory

let updateuser cn (user:User) = 
    buildCommand cn "UPDATE User SET homeDir=%s WHERE id=%s" user.homeDirectory (user.id.ToString())

let updateuserpassword cn (user:User) = 
    buildCommand cn "UPDATE User SET password=%s WHERE id=%s" (encrypt(user.password)) (user.id.ToString())
        
let deleteuser cn (userId:Id) = 
    buildCommand cn "DELETE FROM User WHERE id=%s" (userId.id.ToString())

let getuser cn (userId:Id) = 
    let tx = sql cn
    tx { 
        let! p =  tx.Query "SELECT * FROM User WHERE id=%s" (userId.id.ToString()) 
        return p
    }

let newsite cn (site:Site) = 
    buildCommand cn "INSERT INTO Site (id,name,ipaddress) VALUES(%s,%s,%s)" (newuid()) site.name site.ipaddress

let deletesite cn (siteId:Id) = 
    buildCommand cn "DELETE FROM Site WHERE id=%s" (siteId.id.ToString())

let updateconfig cn paramName paramValue = 
    buildCommand cn "UPDATE Config SET parameterValue=%s WHERE parameterName=%s" paramValue paramName

let updateadminpassword cn (config:Config) = 
    updateconfig cn "adminPassword" (encrypt(config.parameterValue))

let enableanonymousaccess cn (config:Config) = 
    updateconfig cn "anonymousEnabled" config.parameterValue

let getadminpass cn : string = 
    let tx = sql cn
    let p =  tx { 
        let! p =  tx.Query "SELECT parameterValue FROM Config WHERE parameterName=%s" "adminPassword"
        return p
    }
    match p with
    |Some(pass) -> pass
    |_ -> failwith "Parameter adminPassword is missing from table Config"


open Runtime
    
let site_status cn = 
    sites cn |> Array.map (fun x -> { x with status = status (x.id)} )

let sites_data cn = (toJson<Site array>(site_status cn))

(*
let siteCommand (command:Command) serverCertificate authentication_provider = 
    
    match command.command.ToLower() with
    | "stop"    -> stop command.id
    | "start"   -> match site command.id with
                    |Some(site) -> start site serverCertificate authentication_provider
                    |_ -> ()
    |_ -> ()
    
*)