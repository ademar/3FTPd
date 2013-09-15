module Commands

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Text
open System.Diagnostics
open System.Threading
open System.Security.Authentication;
open System.Xml
open System.Xml.Serialization
open System.Security.Cryptography.X509Certificates;
open System.Net.Security

open Common
//open Config 

open Suave.Log

let getPath (path:String) (client:FtpClient) = 

    if(path.StartsWith("/")) then 
        (client.HomeDirectory + path).Replace("//","/")
    else
        (client.HomeDirectory + client.CurrentDirectory + "/" + path).Replace("//","/")

let store filePath inputStream = 
    use fileStream = new FileStream(filePath,FileMode.Create,FileAccess.Write)  
    copy inputStream fileStream
    fileStream.Flush()
    fileStream.Close()
    
let CDUP (client: FtpClient) =  async {    
    let q = client.CurrentDirectory.LastIndexOf("/")
    if(q>0) then
        client.CurrentDirectory <-  client.CurrentDirectory.Substring(0,q)
    else
        client.CurrentDirectory <- "/"
    do! async_writeln (client.ControlStream) "250 CWD command successful."
}

open Ionic.Zlib

type Direction = Up | Down

let on_data_channel (client:FtpClient) (operation:Stream -> unit) (d:Direction) (serverCertificate)= async {

    if(client.Passive) then
        log "client is passive, waiting on %d port\n" client.Port
        
        client.DataClient <- client.TcpListener.AcceptTcpClient()
        client.DataStream <- client.DataClient.GetStream()
        log "client connected\n" 

    let stream = client.ControlStream
    
    if(client.DataStream<>null) then
        
        log "DataConnection is not null, writing\n" 
        
        if(client.Ssl) then
        
            let sslStream = new SslStream(client.DataStream, false);
            sslStream.AuthenticateAsServer(serverCertificate)//, false, SslProtocols.Tls, true);
            client.DataStream <- sslStream
        
        try
            use dataCon = client.DataStream

            if client.ZModeOn then

                match d with
                |Up -> use compressedStream = new ZlibStream(dataCon,CompressionMode.Compress)
                       operation compressedStream
                       compressedStream.Flush()
                |Down -> use compressedStream = new ZlibStream(dataCon,CompressionMode.Decompress)
                         operation compressedStream 
                         compressedStream.Flush()
            else 
                operation dataCon 
            
            dataCon.Flush()
            dataCon.Close()
            client.DataClient.Close()
            //TODO: release data port here
            log "operation is done" 
            
            do! async_writeln stream "226 Transfer complete."
            
        with 
        | x -> do! async_writeln stream "500 Internal server error."
               log "on_data_channel error: %A" x
        
    else
        do! async_writeln stream "500 This command must be preceded by a PORT or PASV command"  
} 

let CWD (dir:string) (client: FtpClient)  = async {

    let path = 
        if(dir.StartsWith("/")) then 
            (dir).Replace("//","/")
        else
            (client.CurrentDirectory + "/" + dir).Replace("//","/")
     
    if dir_exists path client then
        client.CurrentDirectory <- path
        do! async_writeln (client.ControlStream) "250 Directory successfully changed."
    else
        do! async_writeln (client.ControlStream) "550 Directory does not exists."       
} 

let DELE (path:string) (client: FtpClient)  = async {
    try
        let filePath = getPath path client
        
        if File.Exists(filePath) then
            File.Delete(filePath)
            do! async_writeln (client.ControlStream) "250 DELE command successful."
        else
            do! async_writeln (client.ControlStream) "550 File does not exists."
    with 
    |_ -> do! async_writeln (client.ControlStream) "500 Internal server error."
}

let LIST (client: FtpClient) serverCertificate = async {
    if dir_exists client.CurrentDirectory client then
        do! async_writeln (client.ControlStream) "150 Here comes the directory listing."
        do! on_data_channel client (fun x -> write x (dir1 (client.CurrentDirectory) client)) Up serverCertificate
    else do! async_writeln (client.ControlStream) "550 Directory does not exists."
}

let NLIST (client: FtpClient) serverCertificate = async {
    do! async_writeln (client.ControlStream) "150 Here comes the directory listing."
    do! on_data_channel client (fun x -> write x (dir2 (client.CurrentDirectory) client)) Up serverCertificate
}

let MDTM (path:string) (client: FtpClient) = async {

   let filePath = getPath path client
               
   if File.Exists(filePath) then
        let file = new  FileInfo(filePath)
        do! async_writeln (client.ControlStream) (sprintf "213 %s" (file.LastWriteTime.ToString("YYYYMMDDhhmmss")))
   else
        do! async_writeln (client.ControlStream) "550 File does not exists."
}

let local_ip (client: FtpClient) = (client.TcpClient.Client.LocalEndPoint :?> IPEndPoint).Address
   
let rec PASV (client: FtpClient) retries =  async {
    
    try 
        let port = Ports.obtain_port client.Config.low_port client.Config.high_port
        
        log "waiting on port %d" port
        
        client.Port <- port
        
        
        let local_ip = local_ip client
        
        //instead of waiting on local_ip i should listen on the same ip of the client
        //well thats what the function local_ip does .. it extracts it from the connection
        
        let tcpDataConnectionListener = new TcpListener(local_ip,client.Port)
        tcpDataConnectionListener.Start()
        client.TcpListener <- tcpDataConnectionListener
    
        do! async_writeln (client.ControlStream) (sprintf "227 Entering Passive Mode (%s,%d,%d)" (local_ip.ToString().Replace(".",",")) (port / 256) (port % 256))
        client.Passive <- true
    with x -> log "ERROR: Could not open data connection: \n%A.\n" x
              do! async_writeln (client.ControlStream) "500 Server error."
   
}

let EPSV (client: FtpClient) =  async {
    let port = Ports.obtain_port client.Config.low_port client.Config.high_port
    log "waiting on port %d" port
    client.Port <- port
    
    let local_ip = local_ip client

    let tcpDataConnectionListener = new TcpListener(local_ip,client.Port)
    tcpDataConnectionListener.Start()
    client.TcpListener <- tcpDataConnectionListener
    //TODO: implement ipv6 
    log "229 Entering Extended Passive Mode (|||%d|)" port
    do! async_writeln (client.ControlStream) (sprintf "229 Entering Extended Passive Mode (|||%d|)" port)
    client.Passive <- true
}

let PORT (port:string) (client: FtpClient) = async {
   try
    let (ip,port) = parsePort (port)
   
    let tcpClient = new TcpClient(IPAddress.Parse(ip).ToString(),port)
    client.Passive <- false
    client.DataClient <- tcpClient
    client.DataStream <- tcpClient.GetStream()
    
    do! async_writeln (client.ControlStream) "200 Command okay."
   with 
   |x -> do! async_writeln (client.ControlStream) "500 Server error."
         log "PORT failed: %A" x
}

let EPRT (port:string) (client: FtpClient) = async {
   try
    let (ip,port) = parseExtendedPort (port)
   
    let tcpClient = new TcpClient(IPAddress.Parse(ip).ToString(),port)
    client.Passive <- false
    client.DataClient <- tcpClient
    client.DataStream <- tcpClient.GetStream()
    
    do! async_writeln (client.ControlStream) "200 Command okay."
   with 
   |x -> do! async_writeln (client.ControlStream) "500 Server error."
         log "PORT failed: %A" x
}

let RETR (path:string) (client: FtpClient) serverCertificate = async {

    let filePath = getPath path client

    if File.Exists(filePath) then

        do! async_writeln (client.ControlStream) "125 Data connection already open; Transfer starting."
               
        do! on_data_channel client (fun x -> 
            
                log "retrieving: %s" filePath
                use fileStream = new FileStream(filePath,FileMode.Open,FileAccess.Read,FileShare.Read)  
                copy fileStream x  ) Up serverCertificate
    else
        do! async_writeln (client.ControlStream) "550 File does not exists."
}

let SIZE (path:string) (client: FtpClient)  = async {
   
   let filePath = getPath path client
               
   if File.Exists(filePath) then
        let file = new  FileInfo(filePath)
        do! async_writeln (client.ControlStream) (sprintf "213 %d" (file.Length))
   else
        do! async_writeln (client.ControlStream) "550 File does not exists."
}

let MKD (path:string) (client: FtpClient)  = async {
   
   let filePath = getPath path client
   
   try
    Directory.CreateDirectory(filePath) |> ignore
    do! async_writeln (client.ControlStream) "257 Directory created."
   with
   |_ -> do! async_writeln (client.ControlStream) "550 Create directory failed."
}

let RMD (path:string) (client: FtpClient)  = async {
    try
        let filePath = getPath path client
        
        if Directory.Exists(filePath) then
            Directory.Delete(filePath,false)
            do! async_writeln (client.ControlStream) "250 RMD command successful."
        else
            do! async_writeln (client.ControlStream) "550 Directory does not exists."
    with 
    |_ -> do! async_writeln (client.ControlStream) "500 Internal server error."
}

let store1 path client serverCertificate = 
    on_data_channel client (fun x -> 
            
            let filePath = getPath path client
            log "storing: %s" filePath
            
            store filePath x) Down serverCertificate

let STOR (path:string) (client: FtpClient) serverCertificate = async {

    do! async_writeln (client.ControlStream) "125 Data connection already open; Transfer starting."
    
    let cancellation_token_source = new CancellationTokenSource()
    
    (*Async.Start 
        (on_data_channel client (fun x -> 
            
            let filePath = getPath path client.CurrentDirectory
            printf "storing: %s\n" filePath
            
            store filePath x))
            cancellation_token*)
            
            
    Async.Start ((store1 path client serverCertificate),cancellation_token_source.Token)
 
    client.CancellationTokenSource <- cancellation_token_source
    
    (*
    do! on_data_channel client (fun x -> 
            
            let filePath = getPath path client.CurrentDirectory
            printf "storing: %s\n" filePath
            
            store filePath x)
    *)            
}

let ABOR (client: FtpClient) = async {
    if client.CancellationTokenSource <> null then
        client.CancellationTokenSource.Cancel()
        client.CancellationTokenSource <- null
    do! async_writeln (client.ControlStream) "226 The abort command was successfully processed."
}  