# SocketMemory

![Version](https://img.shields.io/badge/version-1.0-blue.svg)
![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blueviolet.svg)

SocketMemory is a client-server socket-based application designed to create and play a word puzzle game. The server hosts the game, while the client connects to the server to play. The difficulty of the game is adjustable by specifying different grid sizes. This project demonstrates network programming using sockets in C#, as well as handling basic game logic between a server and multiple clients.

## Features

- **Client-Server Communication**: The server accepts multiple client connections, each interacting with the server through socket communication.
- **Adjustable Difficulty**: Clients can specify the grid size for the word puzzle.
- **Command-line Interface**: The application supports arguments to configure network interfaces, ports, modes (client/server), and difficulty settings.

## Prerequisites

- .NET SDK 8.0 or higher
- A compatible terminal to run the application

## Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/NakanoMiku13/SocketMemory.git
   ```

2. Navigate to the project directory:

   ```bash
   cd SocketMemory
   ```

3. Restore the required packages:

   ```bash
   dotnet restore
   ```

## Usage

The application supports both **server** and **client** modes. By default, it runs in server mode unless specified otherwise.

### Command-line Arguments

The following options are available when running the program:

- `-m, --mode` : Choose between `server` or `client` mode (default is `server`).
- `-i, --interface` : Specify the network interface to bind the server (default is `wlan0`).
- `--ip` : IP address of the server (only used in client mode, default is `127.0.0.1`).
- `--port` : Port for server connection (default is `9595`).
- `--difficult` : Game difficulty (used to specify grid size like 4x4, 6x6, 8x8).
- `-h, --help` : Display help information.

### Running the Server

To start the server, simply run:

```bash
dotnet run --mode server --interface wlan0 --port 9595
```

Or, use default settings:

```bash
dotnet run
```

### Running the Client

To run the client and connect to the server:

```bash
dotnet run --mode client --ip 192.168.1.10 --port 9595 --difficult 6
```

### Sample Client-Server Interaction

1. The server will wait for client connections and log incoming messages.
2. The client will send a `|CLIENT|` message upon connection.
3. The server sends an acknowledgment (`|ACK|`), and the game begins.

### Terminating the Program

To disconnect, send a message like `|END|` to properly close the socket connections and clean up resources.

## Code Structure

- **Program.cs**: The main entry point where the server logic is handled.
- **ClientSocket.cs**: Contains the client-side socket implementation, handling connection and message sending.
- **Options.cs**: Command-line argument parsing class using `CommandLine` package for flexible configuration.

## Example

1. Start the server:

   ```bash
   dotnet run --mode server --interface wlan0 --port 9595
   ```

2. Connect with a client:

   ```bash
   dotnet run --mode client --ip 192.168.1.10 --port 9595 --difficult 8
   ```

## Logging

All server and client interactions are logged to the console using the `Microsoft.Extensions.Logging` package.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

### Adding Badges

- **Version**: The badge reflects the current version of the project.
- **.NET 8.0**: This shows the targeted .NET runtime for the project.
- **Build Status**: You can add continuous integration (CI) using GitHub Actions, displaying the build status for every commit and pull request. To configure this, you will need a `.github/workflows/dotnet.yml` file in your repository.
