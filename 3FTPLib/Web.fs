module Web

open System.Text
open System

open Suave.Http
open Suave.Web
open Suave.Json
open Suave.Data
//open Suave.Handlers

open Model
open Data
open Runtime

let basic_auth  = 
    authenticate_basic ( fun x -> x.Username.Equals("admin") 
                                    (*&& Common.encrypt(x.Password).Equals(Data.getadminpass())*))  

let myapplication mkdbconn : WebPart  =

    let runDb opp = 
        let cn = mkdbconn ()
        opp cn
        
    let dbCommand cmd (r:HttpRequest) =
        let raw = r.RawForm
        let record = fromJson raw
        let cn = mkdbconn ()
        executeCommand (cmd cn) record 
        OK "OK" r
        
    let getter cmd (r:HttpRequest) =
        let raw = r.RawForm
        let record = fromJson raw
        let cn = mkdbconn ()
        let result = cmd  cn record
        match result with
        |Some res -> ok (toJson res) r
        |None -> never r
        
    let command cmd (r:HttpRequest) =
        let raw = r.RawForm
        let record = fromJson raw
        cmd record
        OK "OK" r
        
    choose [
        Console.OpenStandardOutput() |> log >>= never ; 
        
        basic_auth;
                
        GET >>= url "/users"  >>= ok (runDb users_data);
        GET >>= url "/sites"  >>= ok (runDb sites_data);
        GET >>= url "/connections" >>= ok (runDb processes_data);
        GET >>= url "/anonymous" >>= ok (runDb anonymous_data);
        GET >>= url "/" >>= file "index.html" ; //default 
        
        GET >>= browse ; 
        GET >>= dir ;
        
        (* users *)
        meth0d "POST" >>= url "/newuser" >>= dbCommand newuser ;
        meth0d "POST" >>= url "/updateuser" >>= dbCommand updateuser ;
        meth0d "POST" >>= url "/changepassword" >>= dbCommand updateuserpassword ;
        meth0d "POST" >>= url "/deleteuser" >>= dbCommand deleteuser ;
        
        meth0d "POST" >>= url "/getuser" >>= getter getuser ;

        (* sites *)
        meth0d "POST" >>= url "/newsite" >>= dbCommand newsite ;
        meth0d "POST" >>= url "/deletesite" >>= dbCommand deletesite ;
        //meth0d "POST" >>= url "/site" >>= command<Command> siteCommand ;

        (* connections *)
        meth0d "POST" >>= url "/kill" >>= command kill ;
        meth0d "POST" >>= url "/killall" >>= command killAll ;

        (* config *)
        meth0d "POST" >>= url "/changeadminpassword" >>= dbCommand updateadminpassword ;
        meth0d "POST" >>= url "/enableanonymousaccess" >>= dbCommand enableanonymousaccess ;

        notfound "Found no handlers" 
        ]