module Config

open System
open System.IO
open System.Reflection
open System.Security.Cryptography.X509Certificates;

let appDir _ = Path.GetDirectoryName (Assembly.GetExecutingAssembly().Location)

let certPath = appDir () + "/suave.pfx"

let serverCertificate = new X509Certificate2(certPath,"easy",X509KeyStorageFlags.MachineKeySet);

open Mono.Data.Sqlite

//
//let cn = new SQLiteConnection(sprintf @"Data Source = %s/database" appDir)
//cn.Open()

let open_database _ = 
    let appDir = appDir ()
    let str = (sprintf "Data Source = %s/database" appDir)
    //let str ="ping"
    //Console.WriteLine str
    let cn = new SqliteConnection( str)
    cn.Open()
    cn