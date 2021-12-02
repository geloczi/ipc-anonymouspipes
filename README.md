# IpcAnonymousPipes
Interprocess communication using anonymus pipes. 
The goal of this project is to provide a simple framework to send and receive bytes using anonymus pipes.   

- Targets .NET Standard 2.0
- Lightweight and easy to use
- Client-Server architecture
- One-to-one duplex communication
- Unlimited data length
- Can be used with any serialization library what outputs/accepts byte arrays

## [NuGet package](https://www.nuget.org/packages/IpcAnonymousPipes/)
```
Install-Package IpcAnonymousPipes
```

# Usage

## PipeServer

The server side PipeServer will create two anonymus pipes: input and output. The handles will be passed to the client app as process arguments.  

```
void StartServer()
{
    // Create server pipe, then pass the pipe handles to the client via process arguments.
    pipeServer = new PipeServer(ReceiveAction);
    Process.Start("ClientConsoleApp.exe", string.Join(" ", pipeServer.ClientInputHandle, pipeServer.ClientOutputHandle));
    // Start listening for messages (on a background thread)
    pipeServer.RunAsync();
    
    // Wait for client connection
    // This will wait for maximum 5 seconds, then it throws a TimeoutException if the client is still not connected.
    pipeServer.WaitForClient(TimeSpan.FromSeconds(5));
    
    // Say Hi to the client
    pipeServer.Send(Encoding.UTF8.GetBytes("Hi!"));
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
        // Start listening for messages (on a background thread)
        // Note: If you just want to receive messages, you can use the blocking pipe.Run() instead of pipe.RunAsync()
        pipe.RunAsync();
        
        // Just type something into the console window and press ENTER
        while (pipe.IsConnected)
            pipe.Send(Encoding.UTF8.GetBytes(Console.ReadLine()));
            
        // The pipe will be disposed when the server sends a disconnect signal to this client.
    }
}

static void ReceiveAction(BlockingReadStream stream)
{
    // Write received messages to the console
    Console.WriteLine(Encoding.UTF8.GetString(stream.ReadToEnd()));
}
```

## Example applications

I included two WPF applications just to demonstrate the communication between the server and the client.  
Download the source and build the solution. Then you can start the 
[ServerWpfApp](https://github.com/geloczigeri/ipc-anonymouspipes/tree/main/Examples/ServerWpfApp)
project. The client
[ClientWpfApp](https://github.com/geloczigeri/ipc-anonymouspipes/tree/main/Examples/ClientWpfApp)
will be started automatically and you can start sending in messages. 

### References and application domains

The ServerWpfApp project does not reference ClientWpfApp project, they are completely independent from each other.
My goal was to run the client in an independent **standalone process**, so it lives in a **different Application Domain**. 

## Background
This is about my personal motivation to write this project.  
I had a scenario when I had to separate an audio processor component into a standalone process, because the 3rd party library I used (CScore) caused extreme garbage collector behaviour which ended up in GUI lagging and freezes.
So **separation of the heavy audio processing into a standalone process away from the GUI process** made perfect sense. 
Later I also moved the **audio playback service** into a standalone process, and the **buffering was done with anonymus pipes**. 
Thanks to this separation, the **audio playback was smooth** and cotninuous, the GUI also becaome perfectly smooth, and the **heavy audio conversion process** could do whatever it wanted, it **could not affect anything else** anymore since it lived in it's standalone **Application Domain**.
