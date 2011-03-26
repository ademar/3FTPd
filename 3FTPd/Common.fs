
module Common

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Text
open System.Diagnostics
open System.Threading
open System.Security.Permissions
open System.Security.Principal
open System.Xml
open System.Xml.Serialization

open System.Collections.Generic

open Suave.Log

let execute cmd args =

    log  "executing: %s %s" cmd args 

    let proc = new Process();

    proc.StartInfo.FileName         <- cmd
    proc.StartInfo.CreateNoWindow   <- true
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.UseShellExecute  <- false
    proc.StartInfo.Arguments        <- args
        
    let r = proc.Start()
    proc.WaitForExit()   
    proc.StandardOutput.ReadToEnd()
    
let execute_as (username,password:string) cmd args working_dir = 
    try
        log "executing: %s %s" cmd args
        
        use securePassword = new Security.SecureString()
        Array.iter (securePassword.AppendChar) (password.ToCharArray())
        
        let proc = new Process();

        proc.StartInfo.FileName         <- cmd
        proc.StartInfo.CreateNoWindow   <- true
        proc.StartInfo.RedirectStandardOutput <- true
        proc.StartInfo.RedirectStandardInput <- true
        proc.StartInfo.UseShellExecute  <- false
        proc.StartInfo.Arguments        <- args
      
        proc.StartInfo.WorkingDirectory <- working_dir
        proc.StartInfo.UserName <- username
        proc.StartInfo.Password <- securePassword
        
        proc.Start() |> ignore
        
               
        proc//.Id
     with
     | x -> log "execute_as failed with: %A" x
            null
     

let normalize (s: string) = s.Trim()

let parsePort(s: string) = 
    try
     
     let parts = s.Split(',') |> Array.map (fun (x:string) -> x.Trim()) 
     let port = Convert.ToInt32(parts.[4])*256 + Convert.ToInt32(parts.[5])
     let ip = String.concat "." (Array.sub parts 0 4)
     (ip,port)
    with
    |_ -> failwith "alabao"

let parseExtendedPort(s: string) = 
    try
     
     let parts = s.Split('|') |> Array.map (fun (x:string) -> x.Trim()) 

     let port = Convert.ToInt32(parts.[2])
     let ip = parts.[1]
     (ip,port)
    with
    |_ -> failwith "alabao"

let rnd = new Random(DateTime.Now.Millisecond)

let random_port _ = rnd.Next(500,65235)

let posix = ((int) Environment.OSVersion.Platform = 128);
let root = (WindowsIdentity.GetCurrent().Token = IntPtr.Zero);

let bytes (s:string) =
    Encoding.ASCII.GetBytes(s)

let bytesbin (s:string) =
    Encoding.UTF8.GetBytes(s)
    
let toString ( buff: byte[], index:int, count:int) =
    Encoding.ASCII.GetString(buff,index,count)

let eol = "\r\n"
let EOL = bytes eol

let rec readTillEOL(stream: Stream, buff: byte[], count: int) =
   async{
       
       let! inp = stream.AsyncRead(1)
       
       if count>0 && buff.[count-1] = EOL.[0] && inp.[0] = EOL.[1] then
          return (count-1)
       else
          
          buff.[count] <- inp.[0]
          return! readTillEOL(stream,buff,count + 1)
   }

let async_writeln (stream:Stream) s = 
    async {
        let b = bytes s
        do! stream.AsyncWrite(b, 0, b.Length)
        do! stream.AsyncWrite(EOL, 0, 2)
        stream.Flush()
    }
    
let writeln (stream:Stream) s = 
        let b = bytes s
        stream.Write(b, 0, b.Length)
        stream.Write(EOL, 0, 2)
        stream.Flush()
        
let write (stream:Stream) s = 
        let b = bytes s
        stream.Write(b, 0, b.Length)
        stream.Flush()
        
let writebin (stream:Stream) s = 
        let b = bytesbin s
        stream.Write(b, 0, b.Length)
        stream.Flush()

//--                
let copy (input:Stream) (output:Stream) = 
    let buffer = Array.zeroCreate 32768;
    let mutable flag = true
    while flag do
        let read = input.Read (buffer, 0, buffer.Length);
        if (read <= 0) then
            flag <- false;
        else
            output.Write (buffer, 0, read);
            
  
let serialize obj = 
    let serializer = new XmlSerializer(obj.GetType())
    let sb = new StringBuilder()
    let w = new StringWriter(sb)
    serializer.Serialize(w, obj)
    sb.ToString();
    
let deserialize<'a> str (typeof: Type) : 'a= 
    let serializer = new XmlSerializer(typeof)
    let r = new StringReader(str)
    serializer.Deserialize(r) :?> 'a           
    
type FtpClient(tcpclient: TcpClient, stream:Stream) = 

    let mutable port = 0
    let mutable passive = false
    
    let mutable controlStream:Stream = stream
    let mutable dataStream:Stream = null
    let mutable dataClient:TcpClient = null
    let mutable tcpListener:TcpListener = null
    let mutable user = "anonymous"
    let mutable password:String = null
    let mutable currentDirectory: String =  "/"
    let mutable homeDirectory:String = null
    let mutable ssl: bool = false
    let mutable zmode: bool = false
    let mutable id: Guid = Guid.NewGuid()
    let mutable pid: int = 0
    let mutable connectionTime : int64 = 0L
    let mutable lastCommand: String =  ""
    let mutable remoteIpAddress: String =  tcpclient.Client.RemoteEndPoint.ToString()
    
    let mutable cancellationTokenSource:CancellationTokenSource = null;
    
    member p.Id = id
        
    member p.DataStream 
        with get() = dataStream
        and  set v = dataStream <- v
        
    member p.DataClient 
        with get() = dataClient
        and  set v = dataClient <- v
        
    member p.ControlStream 
        with get() = controlStream
        and  set v = controlStream <- v   
             
    member p.Ssl 
        with get() = ssl
        and  set v = ssl <- v 

    member p.ZModeOn 
        with get() = zmode
        and  set v = zmode <- v 
        
    member p.Passive 
        with get() = passive
        and  set v = passive <- v  
        
    member p.CurrentDirectory
        with get() = currentDirectory
        and set v = currentDirectory <- v

    member p.HomeDirectory
        with get() = homeDirectory
        and set v = homeDirectory <- v  
        
    member p.TcpClient 
        with get() = tcpclient
        
    member p.TcpListener 
        with get() = tcpListener
        and  set v = tcpListener <- v 

    member p.User 
            with get() = user
            and  set v = user <- v  
            
     member p.Password 
            with get() = password
            and  set v = password <- v                   
        
    member p.Port
        with get() = port
        and  set v = port <- v 

    member p.Pid
        with get() = pid
        and  set v = pid <- v

    member p.LastCommand
        with get() = lastCommand
        and  set v = lastCommand <- v
        
    member p.ConnectionTime
        with get() = connectionTime
        and  set v = connectionTime <- v 

    member p.RemoteIpAddress
        with get() = remoteIpAddress
        and  set v = remoteIpAddress <- v 
        
    member p.CancellationTokenSource 
            with get() = cancellationTokenSource
            and  set v = cancellationTokenSource <- v  
        
type Connections = { pid:int ; port:int ; username:string }           
        
let impersonate (username:string) cmd = 
    let nonroot = new WindowsIdentity (username);
    try
        use wic = nonroot.Impersonate();
        cmd ()
    with
    |x -> log "impersonate fails: %A" x        
    
let readTillEOF (st:Stream) =
    let str = new StreamReader(st)
    str.ReadToEnd()

let readLine(stream) = 
    let cmd = readTillEOF(stream)
    log "received : %s" cmd
    cmd    
    
let dump e p =
    use sw = new StreamWriter(p, true)
    sw.WriteLine();
    sw.WriteLine("{0}:{1}", DateTime.Now, e)  
    
let stream (client:TcpClient) = client.GetStream()

let lift2 f = fun b c -> fun a -> f (b a) (c a) 

let dir_exists arg (client:FtpClient) = 
    let dir = new DirectoryInfo(client.HomeDirectory + arg)
    dir.Exists
    
let dir1 (arg:string) (client:FtpClient) = 
    let result = new StringBuilder()

    let arg' = arg.Replace("/","\\")
    let dir = new DirectoryInfo(client.HomeDirectory + arg)
       
    let filesize  (x:FileSystemInfo) =  
        if (x.Attributes ||| FileAttributes.Directory =  FileAttributes.Directory) then 
            String.Format("{0,-14}","<DIR>") //|> Some
        else 
            String.Format("{0,14}",(new FileInfo(x.FullName)).Length) //|> Some
            
    let formatdate (t:DateTime) = 
        t.ToString("MM-dd-yy") + "  " + t.ToString("hh:mmtt")

    let line (x:FileSystemInfo) = 
        try
            (formatdate(x.LastWriteTime) + "       " + filesize(x) + " " + x.Name + "\r\n") |> Some
        with _ -> None
            
    let buildLine (s:string) =  result.Append(s) |> ignore     
        
    (dir.GetFileSystemInfos()) 
    |> Array.sortBy (fun x -> x.Name) 
    |> Array.choose line
    |> Array.iter (buildLine) 
    
    result.ToString()

let dir2 arg (client:FtpClient) = 
    let result = new StringBuilder()
    
    let dir = new DirectoryInfo(client.HomeDirectory + arg)

    let buildLine (x:FileSystemInfo) =  result.Append(x.Name + "\r\n") |> ignore     

    (dir.GetFileSystemInfos()) |> Array.sortBy (fun x -> x.Name) |> Array.iter (buildLine) 
    
    result.ToString()

open System.Text
open System.Security.Cryptography

let encrypt (plainMessage:string) =
    let data = Encoding.UTF8.GetBytes(plainMessage)
    use sha = new SHA256Managed()
    let encryptedBytes = sha.TransformFinalBlock(data, 0, data.Length)
    Convert.ToBase64String(sha.Hash)

open System.Collections.Concurrent

let clients = new ConcurrentDictionary<Guid,FtpClient>()

let removeClient id = clients.TryRemove(id) |> ignore 

let getClient id = clients.TryGetValue(id);

let kill id = 
    try
        
        Suave.Log.log "killing %A ... " id 
        match getClient id with
        |true,client -> 
            if not (client.DataClient = null) then Suave.Tcp.close client.DataClient
            if not (client.TcpClient = null) then Suave.Tcp.close client.TcpClient
        |false,_ -> ()
        removeClient id
        Suave.Log.log "killed." 

    with _ -> Suave.Log.log "killing failed"

open System.Threading

let ports_lock = new Semaphore(1,1)
let mutable ports :int Set = Set.empty

let release_port port = 
    ports_lock.WaitOne() |> ignore
    ports <- Set.remove port ports
    ports_lock.Release() |> ignore
    
let rec iter_ports _ = 
    let port = random_port ()
    if Set.contains port ports then iter_ports()
    else 
        ports <- Set.add port ports
        port

let obtain_port _ =
    ports_lock.WaitOne() |> ignore
    let port = iter_ports ()
    ports_lock.Release() |> ignore
    port

let killEx (ftpClient:FtpClient) = 
    kill ftpClient.Id
    release_port ftpClient.Port
