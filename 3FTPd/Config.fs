module Config

open System
open System.IO
open System.Reflection
open System.Security.Cryptography.X509Certificates;

let appDir = Path.GetDirectoryName (Assembly.GetExecutingAssembly().Location)

let certPath = appDir + @"\suave.pfx"

let serverCertificate = new X509Certificate2(certPath,"easy",X509KeyStorageFlags.MachineKeySet);

open System.Data.SQLite

let cn = new SQLiteConnection(sprintf @"Data Source = %s/database" appDir)
cn.Open()