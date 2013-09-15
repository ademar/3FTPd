module Runtime

open Ftp
open Model
open Common

open Suave.Json

let processes _ = 
    clients.Keys 

    |> Seq.map (fun x -> let p = clients.[x] in 
                            { pid = p.Id.ToString(); username = p.User; 
                                connectionTime = int (timer.ElapsedMilliseconds - p.ConnectionTime)/1000; 
                                ipAddress = p.RemoteIpAddress;
                                lastCommand = p.LastCommand})
    |> Seq.toArray

let uptime _ = int(timer.ElapsedMilliseconds/int64(1000))
let numberOfProcesses _ = clients.Values.Count

let processes_data _ = (toJson<HomeModel>({ uptime = uptime (); numberOfProcesses = numberOfProcesses () ; processes = processes () }))

//open Data
open System
open System.Threading

open Suave.Log

type FtpSite = { site: Site; cancellationSource: CancellationTokenSource;  }

let mutable runningSites = Map.empty<Guid,FtpSite>

let status siteid = 
    if runningSites.ContainsKey siteid then
        "running"
    else
        "stopped"

let extractIp ep = ep.ToString().Split(':').[0]

let containsBinding b bindings =
    Array.exists (fun x -> x.Equals(b)) bindings

let bindings (b:String) = 
    b.Split('\n') 
    |> Array.map (fun x -> x.Trim())
    |> Array.filter ( fun x -> not(x.Equals("")))   

let stop (siteId:Guid) = 
    let s = runningSites.[siteId]
    log "cancelling site:%s" s.site.name
    s.cancellationSource.Cancel()
    runningSites <- Map.remove siteId runningSites
    log "killing all clients"
    clients.Values 
    |> Seq.filter ( fun x -> (extractIp x.TcpClient.Client.RemoteEndPoint).Equals(s.site.ipaddress))
    |> Seq.iter( fun x -> kill x.Id)
    
let start (site:Site) serverCertificate authentication_provider =
    
    log "starting site:%s" site.name
    let cancellationSource = new CancellationTokenSource()
    let config = { 
        ipaddress = site.ipaddress; 
        certificate = serverCertificate; 
        authentication_provider = authentication_provider;
        low_port = 700;
        high_port = 8000 } 
    Async.Start(ftp_server config, cancellationSource.Token)
    
    runningSites <- Map.add site.id { site = site; cancellationSource =  cancellationSource} runningSites

let kill (con:Id) = Common.kill con.id

let killAll (con:Id) = 
    match  getClient con.id with
    |true, client -> clients.Keys 
                        |> Seq.filter (fun x -> clients.[x].User = client.User)
                        |> Seq.iter (fun x  -> kill ({id = x}))
    |false,_ -> ()