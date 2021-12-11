# IpcAnonymousPipes
Interprocess communication using anonymus pipes. 
The goal of this project is to provide a simple framework to send and receive byte arrays over 
anonymus pipes efficiently.  
You may use a **serialization library of your choice** on top of this project to implement messaging 
**like [IpcAnonymousPipes.Json](https://github.com/geloczigeri/ipc-anonymouspipes-json)** which is built 
on top of this library to extend it with serialization/deserialization of objects.  

- Targets .NET Standard 2.0
- Lightweight and easy to use
- Client-Server architecture
- One-to-one duplex communication
- Works with simple byte arrays
- You may use it with a serialization library of your choice


## Usage

### [NuGet package](https://www.nuget.org/packages/IpcAnonymousPipes/)
```
Install-Package IpcAnonymousPipes
```

### PipeServer

The server side PipeServer will create two anonymus pipes: input and output. The handles will be passed to the client app as process arguments.  

```
void StartServer()
{
    // Create pipe server
    Server = new PipeServer();
    
    // Start client process with command line arguments
    Process.Start("MyClient.exe", Server.GetClientArgs());
    
    // Receiving on background thread
    Server.ReceiveAsync(stream =>
    {
        Console.WriteLine(Encoding.UTF8.GetString(stream.ReadToEnd()));
    });
    
    // Wait for client connection
    Server.WaitForClient();
    
    // Say Hi to the client
    Server.Send(Encoding.UTF8.GetBytes("Hi!"));
}
```

### PipeClient

This is a minimal client implementation with a console application.
The Client will be disposed when the server sends a disconnect signal to this client.

```
static void Main(string[] args)
{
    // Create pipe client
    // The empty constructor parses command line arguments to get the pipe handles.
    using (var Client = new PipeClient())
    {
        // Receiving on background thread
        Client.ReceiveAsync(stream =>
        {
            Console.WriteLine(Encoding.UTF8.GetString(stream.ReadToEnd()));
        });

        // Read line from console, press ENTER to send
        while (Client.IsConnected)
            Client.Send(Encoding.UTF8.GetBytes(Console.ReadLine()));
    }
}
```

### Object serializers and portability

So the PipeClient runs in a standalone Process (most of the time this is the use case). In order 
to send an object, you have to serialize it to bytes and deserialize it on the other end of the 
pipe to the proper type. Somehow you gotta know the type of the object coming into your pipe.  

If you're about to implement **simple messaging (sending and receiving objects)**, you may want to 
**automatically deserialize** the received data **to the type it was** before serialization. One 
possibility is to serialize it to a format which contains type information which does not refers 
the origin assembly, so it remains portable.  

Check out my other project 
**[IpcAnonymousPipes.Json](https://github.com/geloczigeri/ipc-anonymouspipes-json)** 
which uses JSON format with type names to solve this problem. 

## Example applications

You can find two **WPF applications** in the [repository](https://github.com/geloczigeri/ipc-anonymouspipes). 
I wrote them in order to demonstrate the **two-way communication** between the server and the client.  

### [ServerWpfApp](https://github.com/geloczigeri/ipc-anonymouspipes/tree/main/Examples/ServerWpfApp)

Download the source and build the solution. Then you can start the 
[ServerWpfApp](https://github.com/geloczigeri/ipc-anonymouspipes/tree/main/Examples/ServerWpfApp)
project.  
The ServerWpfApp project **does not reference ClientWpfApp** project, they are completely independent from each other.
My goal was to run the client inside a **standalone process**, so it **lives in it's own Application Domain**. 

### [ClientWpfApp](https://github.com/geloczigeri/ipc-anonymouspipes/tree/main/Examples/ClientWpfApp)

The client
[ClientWpfApp](https://github.com/geloczigeri/ipc-anonymouspipes/tree/main/Examples/ClientWpfApp)
will be started automatically by ServerWpfApp. 
You can send messages by typing into the textbox and pressing the *Send* button.
