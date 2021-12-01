# IpcAnonymousPipes
Interprocess communication using anonymus pipes. 
The goal of this project is to provide a simple framework to send and receive bytes using anonymus pipes.   

- Targets .NET Standard 2.0
- Lightweight and easy to use
- Client-Server architecture
- One-to-one duplex communication
- Unlimited data length
- Can be used with any serialization library what outputs/accepts byte arrays

## NuGet package
[Nuget package available on nuget.org](https://www.nuget.org/packages/IpcAnonymousPipes/)

```
Install-Package IpcAnonymousPipes
```

# Usage

## PipeServer

The server side PipeServer will create two anonymus pipes: input and output. The handles will be passed to the client as process arguments.  

```
void StartServer()
{
    // Create server pipe, then pass the pipe handles to the client via process arguments.
    pipeServer = new PipeServer(ReceiveAction);
    Process.Start("ClientConsoleApp.exe", string.Join(" ", pipeServer.ClientInputHandle, pipeServer.ClientOutputHandle));
    pipeServer.RunAsync();
}

void ServerReceiveAction(BlockingReadStream stream)
{
    Console.WriteLine(Encoding.UTF8.GetString(stream.ReadToEnd()));
}
```

## PipeClient

This is a minimal client implementation with a console application.

```
static void Main(string[] args)
{
    // Create client pipe using the handles from the arguments
    using (var pipe = new PipeClient(args[0], args[1], ReceiveAction))
    {
        // Start the client
        pipe.RunAsync();
        // Just type something into the console window and press ENTER
        while (pipe.IsConnected)
            pipe.Send(Encoding.UTF8.GetBytes(Console.ReadLine()));
    }
}

static void ReceiveAction(BlockingReadStream stream)
{
    // Write received messages to the console
    Console.WriteLine(Encoding.UTF8.GetString(stream.ReadToEnd()));
}
```
