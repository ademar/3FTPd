module Ftp

open System
open System.IO
open System.Net
open System.Net.Sockets

open System.Text
open System.Diagnostics
open System.Threading
open System.Security.Permissions
open System.Security.Authentication;
open System.Xml
open System.Xml.Serialization

//open OpenSSL
//open OpenSSL.X509

open System.Security.Cryptography.X509Certificates;
open System.Net.Security

open Common
open Proxy

//let serverCertificate = X509Certificate.CreateFromCertFile("ca.cer");
//let serverCertificate = new X509Certificate("codemaker.pfx","password");

//let cc = Core.BIO.File(@"C:\Users\ademar\Documents\Visual Studio 2008\Projects\vfftp\vfftp\bin\Debug\codemaker.pfx","r")
//let serverCertificate = X509Certificate.FromPKCS12(cc,"password");

open System.Diagnostics;
let timer = new Stopwatch();
timer.Start();

let banner = bytes "220 Welcome to the very fast FTP server."

let print_banner (stream: Stream)   =  async {
    do! stream.AsyncWrite(banner, 0, banner.Length)  
    do! stream.AsyncWrite(EOL, 0, 2)
}

let readCommand stream = async {
        let buf = Array.zeroCreate 1024 //max buff command
        let! count = readTillEOL(stream,buf,0)
    
        return toString(buf, 0, count)
}

open System.Configuration
open System.Security.AccessControl

//windows
let set_acl (username:string) (dir: DirectoryInfo) = 
    let sec = dir.GetAccessControl(AccessControlSections.Access)
    let rights = FileSystemRights.FullControl
    let rule = new FileSystemAccessRule(username,rights,InheritanceFlags.None,PropagationFlags.NoPropagateInherit,AccessControlType.Allow)
    
    sec.ModifyAccessRule(AccessControlModification.Set,rule) |> ignore
    let iFlags = InheritanceFlags.ContainerInherit ||| InheritanceFlags.ObjectInherit;
    
    //inheritance access rule
    let inheritance_rule = new FileSystemAccessRule(username, rights,
                                iFlags,
                                PropagationFlags.InheritOnly,
                                AccessControlType.Allow);
    sec.ModifyAccessRule(AccessControlModification.Add,inheritance_rule) |> ignore
    dir.SetAccessControl(sec)

open Commands

let command(str1: string, client: FtpClient) serverCertificate = async {

    let stream = client.ControlStream
    
    let (control_cmd,arguments) = 
        let indexOfSpace = str1.IndexOf(' ')
        if(indexOfSpace>0) then
            (str1.Substring(0,indexOfSpace).Trim()
            ,str1.Substring(indexOfSpace).Trim())
        else
            (str1,null)

    match control_cmd.ToUpper() with
    //Aborts a file transfer currently in progress.
    |"ABOR" -> do! ABOR client
    //|"ACCT" -> ()
    //|"ADAT" -> ()
    //|"ALLO" -> ()
    //Syntax: APPE remote-filename
    //Append data to the end of a file on the remote host. If the file does not already exist, it is created. 
    //This command must be preceded by a PORT or PASV command so that the server knows where to receive data from.
    //|"APPE" -> ()
      
    //|"CCC"  -> ()
    //Syntax: CDUP
    //Makes the parent of the current directory be the current directory.
    |"CDUP" -> do! CDUP client
    //|"CONF" -> ()
    //Syntax: CWD remote-directory
    //Makes the given directory be the current directory on the remote host.
    |"CWD"  -> do! CWD arguments client
    //Syntax: DELE remote-filename
    //Deletes the given file on the remote host.
    |"DELE" ->  do! DELE arguments client
    //|"ENC"  -> ()
    //|"EPRT" -> ()
    //|"EPSV" -> ()
    |"FEAT" -> do! async_writeln stream "211-Features:\nSIZE\nMDTM\nMODE Z"
               do! async_writeln stream "211 Features okay."
    //|"LANG" -> ()
    |"HELP" -> do! async_writeln stream "214-The following commands are recognized."
               do! async_writeln stream "214 Help okay."
    //Syntax: LIST [remote-filespec]
    //If remote-filespec refers to a file, sends information about that file. 
    //If remote-filespec refers to a directory, sends information about each file in that directory. remote-filespec defaults to the current directory. 
    //This command must be preceded by a PORT or PASV command.
    |"LIST" ->  do! LIST client serverCertificate
    //|"LPRT" -> ()
    //|"LPSV" -> ()
    //Syntax: MDTM remote-filename
    //Returns the last-modified time of the given file on the remote host in the format "YYYYMMDDhhmmss": 
    //YYYY is the four-digit year, MM is the month from 01 to 12, DD is the day of the month from 01 to 31, hh is the hour from 00 to 23, mm is the minute from 00 to 59, and ss is the second from 00 to 59.
    |"MDTM" -> do! MDTM arguments client
    //|"MIC"  -> ()
    //Syntax: MKD remote-directory
    //Creates the named directory on the remote host.
    |"MKD" |"XMKD" -> do! MKD arguments client
    //|"MLSD" -> ()
    //|"MLST" -> ()
    |"MODE" -> if arguments.Equals("Z") then 
                client.ZModeOn <- true
                do! async_writeln stream "200 MODE Z ok."
    //Syntax: NLST [remote-directory]
    //Returns a list of filenames in the given directory (defaulting to the current directory), with no other information. Must be preceded by a PORT or PASV command.
    |"NLST" -> do! NLIST client serverCertificate
    //Syntax: NOOP
    //Does nothing except return a response.
    |"NOOP" -> do! async_writeln stream "200 NOOP ok."
    //|"OPTS" -> ()
    //Syntax: PASV
    //Tells the server to enter "passive mode". In passive mode, the server will wait for the client to establish a connection with it rather than attempting to connect to a client-specified port. The server will respond with the address of the port it is listening on, with a message like:
    //227 Entering Passive Mode (a1,a2,a3,a4,p1,p2)
    //where a1.a2.a3.a4 is the IP address and p1*256+p2 is the port number.
    |"PASV" -> do! PASV client 0
    |"EPSV" -> do! EPSV client
    |"PBSZ" -> do! async_writeln stream "200 PBSZ set to 0."
    //Syntax: PORT a1,a2,a3,a4,p1,p2
    //Specifies the host and port to which the server should connect for the next file transfer. This is interpreted as IP address a1.a2.a3.a4, port p1*256+p2.
    |"PORT" -> do! PORT arguments client
    |"EPRT" -> do! EPRT arguments client
    //Protect data chanel
    |"PROT" ->  client.Ssl <- true
                do! async_writeln stream "200 PROT now Private."
    //Syntax: PWD
    //Returns the name of the current directory on the remote host.
    |"PWD" |"XPWD"  -> do! async_writeln stream (sprintf "257 \"%s\"" (client.CurrentDirectory.Replace("//","/")))

    //Syntax: QUIT
    //Terminates the command connection.
    |"QUIT" -> do! async_writeln stream "221 Goodbye." 
               //we should finish this now

    //|"REIN" -> ()
    //|"REST" -> ()
    //Syntax: RETR remote-filename
    //Begins transmission of a file from the remote host. Must be preceded by either a PORT command or a PASV command to indicate where the server should send data.
    |"RETR" ->  do! RETR arguments client serverCertificate
    //Syntax: RMD remote-directory
    //Deletes the named directory on the remote host.
    |"RMD" |"XRMD"  -> do! RMD arguments client
    //Syntax: RNFR from-filename
    //Used when renaming a file. Use this command to specify the file to be renamed; follow it with an RNTO command to specify the new name for the file.
    //|"RNFR" -> ()
    //Syntax: RNTO to-filename
    //Used when renaming a file. After sending an RNFR command to specify the file to rename, send this command to specify the new name for the file.
    //|"RNTO" -> ()
    //|"SITE" -> ()
    //Syntax: SIZE remote-filename
    //Returns the size of the remote file as a decimal number.
    |"SIZE" -> do! SIZE arguments client
    //|"SMNT" -> ()
    //|"STAT" -> ()
    //Syntax: STOR remote-filename
    //Begins transmission of a file to the remote site. Must be preceded by either a PORT command or a PASV command so the server knows where to accept data from.
    |"STOR" ->  do! STOR arguments client serverCertificate
    //|"STOU" -> ()
    //|"STRU" -> ()
    |"SYST" -> do! async_writeln stream "215 Windows_NT"
    |"TYPE" -> do! async_writeln stream "200 Switching to Binary mode."
    
    |_ -> do! async_writeln stream "500 Syntax error, command unrecognized."
         
}

let asyncServiceClient (ftpClient: FtpClient) serverCertificate = async {

   do! async_writeln ftpClient.ControlStream "230 Login successful."
   
   let clientConnected = ref true
   
   while !clientConnected do
           
       let! cmd = readCommand ftpClient.ControlStream

       ftpClient.LastCommand <- cmd
       
       Suave.Log.log "received : %s \n" cmd

       if(cmd.ToUpper().Equals("QUIT")) then
        do! async_writeln ftpClient.ControlStream "221 Goodbye." 
        clientConnected := false
       else
        do! command (cmd,ftpClient) serverCertificate
    
}

open Suave.Data
//open Config

(*


*)
let rec loop serverCertificate (client: FtpClient) =  async {
        
    let! cmd = readCommand(client.ControlStream)

    Suave.Log.log "%s" cmd
        
    let parts = cmd.Split(' ') |> Array.map (fun (x:string) -> x.Trim())

    client.LastCommand <- parts.[0]

    match parts.[0] with
    
    |"AUTH" -> if (parts.[1] = "SSL" || parts.[1] = "TLS") then 
                   
                    let sslStream = new SslStream(client.ControlStream, false);
                    do! async_writeln client.ControlStream "234 Proceed with negotiation."
                    
                    sslStream.AuthenticateAsServer(serverCertificate)//, false, SslProtocols.Tls, true);
                    
                    client.ControlStream <- sslStream
                    
                else
                    do! async_writeln client.ControlStream "504 Unknown AUTH type."

               do!  loop serverCertificate client 
    
    |"USER" ->  client.User <- parts.[1]
                //TODO: look up home directory, or maybe later
                do! async_writeln client.ControlStream "331 Please specify the password."
                do! loop serverCertificate client 
               
    |"PASS" ->  client.Password <- parts.[1]
        
    |_ -> do! async_writeln client.ControlStream "530 Please login with USER and PASS."  
          do!  loop serverCertificate client 
}

    
let ftp_worker serverCertificate authentication_provider (client:TcpClient) = async {

    let ftpClient = new FtpClient(client,client.GetStream())

    ftpClient.ConnectionTime <- timer.ElapsedMilliseconds
    
    clients.TryAdd(ftpClient.Id,ftpClient) |> ignore 

    //use! dd = Async.OnCancel( fun () -> killEx(ftpClient);)

    do! print_banner ftpClient.ControlStream

    let authenticated = ref false

    try
        while not(!authenticated) do
            do! loop serverCertificate ftpClient
            match authentication_provider ftpClient  with
            |Some(_) -> authenticated := true
            |None -> do! async_writeln ftpClient.ControlStream "530 Invalid Password."
    
        do! asyncServiceClient  ftpClient serverCertificate
    finally
            killEx(ftpClient)

}

open Suave.Tcp

let ftp_server ipaddress serverCertificate authentication_provider = 
    let worker = ftp_worker serverCertificate authentication_provider
    tcp_ip_server (ipaddress,21) worker //(Suave.Combinator.cnst false)
(*
let ftp_servers bindings = 
    bindings
    |> Array.map(fun x -> tcp_ip_server (x,21) ftp_worker (Suave.Combinator.cnst false))
    |> Async.Parallel
    |> Async.Ignore
*)