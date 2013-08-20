module Model

open System.Runtime.Serialization
open System.Runtime.Serialization.Json

[<DataContract>]
type ProcessModel = { 
    [<field: DataMember(Name = "pid")>]  pid:string ; 
    [<field: DataMember(Name = "username")>]  username : string
    [<field: DataMember(Name = "connectionTime")>]  connectionTime : int;
    [<field: DataMember(Name = "ipAddress")>]  ipAddress : string ;
    [<field: DataMember(Name = "lastCommand")>]  lastCommand : string 
}

[<DataContract>]
[<KnownType(typeof<ProcessModel>)>]
type HomeModel = { 
     [<field: DataMember(Name = "uptime")>] uptime : int; 
     [<field: DataMember(Name = "numberOfProcesses")>] numberOfProcesses : int
     [<field: DataMember(Name = "processes")>] processes: ProcessModel array
}

[<DataContract>]
type Id = {
    [<field: DataMember(Name = "id")>]  id : System.Guid;
}

[<DataContract>]
type User = { 
    [<field: DataMember(Name = "id")>]  id : System.Guid
    [<field: DataMember(Name = "username")>]  username : string
    [<field: DataMember(Name = "password")>]  password : string
    [<field: DataMember(Name = "homeDirectory")>]  homeDirectory : string;
}

[<DataContract>]
type Site = { 
    [<field: DataMember(Name = "id")>]  id : System.Guid
    [<field: DataMember(Name = "name")>]  name : string
    [<field: DataMember(Name = "ipaddress")>]  ipaddress : string;
    [<field: DataMember(Name = "status")>]  status : string;
}

[<DataContract>]
type Command = { 
    [<field: DataMember(Name = "id")>]  id : System.Guid
    [<field: DataMember(Name = "command")>]  command : string
}

[<DataContract>]
type Config = { 
     [<field: DataMember(Name = "parameterName")>] parameterName : string
     [<field: DataMember(Name = "parameterValue")>] parameterValue: string
}

[<DataContract>]
type Status = { 
    [<field: DataMember(Name = "status")>]  status : string
}
