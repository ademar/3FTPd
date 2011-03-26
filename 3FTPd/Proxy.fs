module Proxy

open System.IO

let mirror (clientStream:Stream) (serverStream:Stream) = async {
    while true do
        let! onebyte = clientStream.AsyncRead(1)
        do! serverStream.AsyncWrite(onebyte) 
}

let proxy (clientStream:Stream) (serverStream:Stream) = 
    try
        [| mirror clientStream serverStream; mirror serverStream clientStream |]
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore
    with
        :? EndOfStreamException -> ()