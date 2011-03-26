module Data

open Suave.Data
open Suave.Json
open Config
open Model
open System

let newuid _ = Guid.NewGuid().ToString()

let users _ =
    let tx = sql cn
    tx.Enum "SELECT * FROM User"
    |> Seq.toArray<User>
    |> Array.map ( fun x -> { x with password = "" } )
    //NOTE: ^ u don't want to transmit the passwords over the wire

let anonymous_data _ = 
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

let site id :  Site option =
    let tx = sql cn
    tx { 
        let! p =  tx.Query "SELECT * FROM Site WHERE id=%s" (id.ToString()) 
        return p
    }

let sites _ =
    let tx = sql cn
    tx.Enum "SELECT id,name,ipaddress FROM Site"
    |> Seq.toArray<Guid*string*string>
    |> Array.map (fun (id,name,ipaddress) -> { id = id ;name = name; ipaddress = ipaddress;  status = ""} )

let users_data _ = (toJson<User array>(users ()))

let buildCommand p = 
    let tx = sql cn in tx.Query p

open Common

let newuser (user:User) = 
    buildCommand "INSERT INTO User (id,username,password,homeDir) VALUES(%s,%s,%s,%s)" (newuid()) user.username (encrypt(user.password)) user.homeDirectory

let updateuser (user:User) = 
    buildCommand "UPDATE User SET homeDir=%s WHERE id=%s" user.homeDirectory (user.id.ToString())

let updateuserpassword (user:User) = 
    buildCommand "UPDATE User SET password=%s WHERE id=%s" (encrypt(user.password)) (user.id.ToString())
        
let deleteuser (userId:Id) = 
    buildCommand "DELETE FROM User WHERE id=%s" (userId.id.ToString())

let getuser (userId:Id) = 
    let tx = sql cn
    tx { 
        let! p =  tx.Query "SELECT * FROM User WHERE id=%s" (userId.id.ToString()) 
        return p
    }

let newsite (site:Site) = 
    buildCommand "INSERT INTO Site (id,name,ipaddress) VALUES(%s,%s,%s)" (newuid()) site.name site.ipaddress

let deletesite (siteId:Id) = 
    buildCommand "DELETE FROM Site WHERE id=%s" (siteId.id.ToString())

let updateconfig paramName paramValue = 
    buildCommand "UPDATE Config SET parameterValue=%s WHERE parameterName=%s" paramValue paramName

let updateadminpassword (config:Config) = 
    updateconfig "adminPassword" (encrypt(config.parameterValue))

let enableanonymousaccess (config:Config) = 
    updateconfig "anonymousEnabled" config.parameterValue

let getadminpass _ : string = 
    let tx = sql cn
    let p =  tx { 
        let! p =  tx.Query "SELECT parameterValue FROM Config WHERE parameterName=%s" "adminPassword"
        return p
    }
    match p with
    |Some(pass) -> pass
    |_ -> failwith "Parameter adminPassword is missing from table Config"
