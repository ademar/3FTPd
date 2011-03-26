module Web

open System.Text
open System

open Suave.Combinator
open Suave.Web
open Suave.Json
open Suave.Data
open Suave.Handlers

open Model
open Data
open Runtime

let basic_auth  = 
    authenticate_basic ( fun x -> x.Username.Equals("admin") 
                                    (*&& Common.encrypt(x.Password).Equals(Data.getadminpass())*))  

let myapplication : WebPart  =
    choose [
        Console.OpenStandardOutput() |> log >>= never ; 
        
        basic_auth;
                
        meth0d "GET" >>= url "/users"  >>= ok users_data;
        meth0d "GET" >>= url "/sites"  >>= ok sites_data;
        meth0d "GET" >>= url "/connections" >>= ok processes_data;
        meth0d "GET" >>= url "/anonymous" >>= ok anonymous_data;
        meth0d "GET" >>= url "/" >>= file "index.html" ; //default 
        
        meth0d "GET" >>= browse ; 
        meth0d "GET" >>= dir ;
        
        (* users *)
        meth0d "POST" >>= url "/newuser" >>= dbCommand newuser ;
        meth0d "POST" >>= url "/updateuser" >>= dbCommand<User> updateuser ;
        meth0d "POST" >>= url "/changepassword" >>= dbCommand<User> updateuserpassword ;
        meth0d "POST" >>= url "/deleteuser" >>= dbCommand<Id> deleteuser ;
        
        meth0d "POST" >>= url "/getuser" >>= getter<Id,User> getuser ;

        (* sites *)
        meth0d "POST" >>= url "/newsite" >>= dbCommand<Site> newsite ;
        meth0d "POST" >>= url "/deletesite" >>= dbCommand<Id> deletesite ;
        meth0d "POST" >>= url "/site" >>= command<Command> siteCommand ;

        (* connections *)
        meth0d "POST" >>= url "/kill" >>= command<Id> kill ;
        meth0d "POST" >>= url "/killall" >>= command<Id> killAll ;

        (* config *)
        meth0d "POST" >>= url "/changeadminpassword" >>= dbCommand<Config> updateadminpassword ;
        meth0d "POST" >>= url "/enableanonymousaccess" >>= dbCommand<Config> enableanonymousaccess ;

        notfound (bytes "Found no handlers" |> cnst )  
        ]