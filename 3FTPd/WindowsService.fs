namespace WindowsService

open System.ComponentModel 
open System.ServiceProcess

type WindowsService() as this = 
    inherit ServiceBase()
    do 
        this.ServiceName <- "3FTPd" 
        this.EventLog.Log <- "3FTPd"

    override this.OnStart(args:string[]) = Program.main() |> Async.Start
        
    override this.OnStop() = Program.stop ()

open System.Configuration.Install 
open System.Security.Principal
open System.Security.AccessControl
open System.IO

[<RunInstaller(true)>] 
type public MyInstaller() as this = 
    inherit Installer() 
    do 
        let spi = new ServiceProcessInstaller() 

        let si = new ServiceInstaller() 

        spi.Account <- ServiceAccount.NetworkService 
        spi.Username <- null 
        spi.Password <- null

        si.DisplayName <- "3FTPd" 
        si.Description <- "The Fast Fsharp Daemon"
        si.StartType <- ServiceStartMode.Automatic 
        si.ServiceName <- "3FTPd"

        this.Installers.Add(spi) |> ignore 
        this.Installers.Add(si) |> ignore

    let reset_permissions _ =
        let di = new DirectoryInfo(Config.appDir)
        let ds = di.GetAccessControl()
        let NetworkServiceName  = (new SecurityIdentifier("S-1-5-20")).Translate(typeof<NTAccount>).ToString();
        let rule = new FileSystemAccessRule(NetworkServiceName,FileSystemRights.FullControl,InheritanceFlags.ContainerInherit &&& InheritanceFlags.ObjectInherit,PropagationFlags.InheritOnly, AccessControlType.Allow)
        ds.AddAccessRule(rule)
        di.SetAccessControl(ds)

    override this.Install(stateSaver) =
        reset_permissions ()
        base.Install(stateSaver)

module Entry = 
    [<EntryPoint>] 
    let Main(args) = 
        ServiceBase.Run(new WindowsService()) 
        0 