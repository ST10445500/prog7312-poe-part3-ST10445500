# Smart-X

> **Student Number:** ST10445500

---

## Table of Contents

- [What is Smart-X?](#what-is-smart-x)
- [Prerequisites](#prerequisites)
- [Restore and Build](#restore-and-build)
- [Running the App](#running-the-app)
- [Finding the Features](#finding-the-features)
- [Testing the Mesh](#testing-the-mesh)
- [Final Architecture](#final-architecture)
- [Tech Stack](#tech-stack)
- [How the Brief Maps to the Code](#how-the-brief-maps-to-the-code)
- [Docker and Containerisation](#docker-and-containerisation)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [Key API Endpoints](#key-api-endpoints)
- [Notes for the Marker](#notes-for-the-marker)
- [Demo](#demo)
- [AI Assistance Declaration](#ai-assistance-declaration)

---

## What is Smart-X?

Smart-X is a telemetry gateway for IoT deployments spread across sites that do not
share a network, such as a hydroponic farm or a utility tracker. ESP32-class devices
post readings of several different types, and the gateway has to validate and store
all of them without falling over when a device misbehaves.

The awkward part of the scenario is that the readings are not all the same shape. A
moisture sensor sends a `float`, a power meter sends an `int`, and a valve sends a
`bool`. I did not want three near-identical pipelines, so the whole system is built
around one generic packet type that can carry any of them.

The PoE has three architectural pillars that switch on across the three parts. Parts 1
and 2 built the first two, ingestion and the real-time command stream. This README is for
Part 3, which switches on the last one: how devices reach the gateway when they pass each
other's traffic along instead of all talking to it directly. The first two parts are still
in the app and still work. Their READMEs are kept as I handed them in:
[README-part1.md](README-part1.md) and [README-part2.md](README-part2.md).

| Pillar | State |
|---|---|
| Sensor Data Ingestion and Telemetry | Active, from Part 1 |
| Real-Time Command Stream and History | Active, from Part 2 |
| Network Topology and Mesh Routing | Active, new in Part 3 |

What Part 3 adds:

- See the mesh as a drawing, with the cheapest route from every device to the gateway
  marked
- Pick a device and read off its hops to the gateway, with the signal on each
- Take a relay off the air and watch the devices behind it go quiet or get cut off
- Look back through a device's radio history, signal strength and hop count and duty
  cycle and packet loss, for any stretch of time
- Run the whole stack, gateway and dashboard and simulated devices, with one Docker
  command

Registering sensors, the deployment tree, encrypted files, the signal trace, the command
stream, overrides with undo and the suggestions are from Parts 1 and 2.

---

## Prerequisites

| Requirement | Version | Check with |
|---|---|---|
| .NET SDK | 10.0 or later | `dotnet --version` |
| Docker Desktop | optional, only for the Docker route | `docker --version` |

Bootstrap 5.3.3 is vendored into `SmartX.Web/wwwroot/lib/bootstrap/` rather than
loaded from a CDN, so the dashboard still looks right with no internet connection.

Part 2 adds one package, `Microsoft.AspNetCore.SignalR.Client`, to the web project.
`dotnet restore` fetches it like the rest.

---

## Restore and Build

From the repository root:

```bash
dotnet restore SmartX.slnx
dotnet build SmartX.slnx
dotnet test SmartX.Tests/SmartX.Tests.csproj
```

The solution file is `SmartX.slnx`, the newer XML solution format, not a `.sln`.

---

## Running the App

The main way to run it is Visual Studio, which starts everything with one key. Docker
is the alternative if you would rather not install anything, and plain `dotnet run`
is there as a fallback.

### With Visual Studio

Open `SmartX.slnx` and pick the **App & Web** launch profile, then press F5. That
starts the API, the dashboard and the simulator together, and Shift+F5 stops all
three. The first time the API is served over https on a machine, the ASP.NET
development certificate has to be trusted:

```bash
dotnet dev-certs https --trust
```

| URL | What it is |
|---|---|
| `https://localhost:7267` | The dashboard. Open this one. |
| `https://localhost:7002` | The gateway API |
| `https://localhost:7002/openapi/v1.json` | The OpenAPI document |

The gateway starts with a small demo facility already registered, so the fleet is
never empty. **Readings only start arriving once the simulator is running**, so the
dashboards stay quiet until it is started.

### With Docker

Everything speaks plain HTTP inside Compose, so no development certificate is needed
at all. Make sure Docker Desktop is running, then from the repository root:

```bash
docker compose up --build     # first run, or after a code change
docker compose up             # after that
docker compose down           # stop and remove the containers
docker compose logs -f simulator
```

| URL | What it is |
|---|---|
| `http://localhost:8081` | The dashboard. Open this one. |
| `http://localhost:8080` | The gateway API |

The simulator waits a few seconds for the gateway to start listening before its
first call, so give the dashboard about twenty seconds to fill up.

### With dotnet run

This needs the same trusted development certificate as Visual Studio. Then three
terminals, from the repository root:

```bash
dotnet run --project SmartX.Api          # terminal 1, the gateway
dotnet run --project SmartX.Web          # terminal 2, the dashboard
dotnet run --project SmartX.Simulator    # terminal 3, the devices
```

The URLs are the same as the Visual Studio ones above.

### What the simulator does

On its own the simulator registers twenty more devices on top of the nine the gateway
starts with, so there are 28 devices reporting. It backfills a few hours of history
per sensor and then keeps streaming about a reading a second. I did not want anyone
to need a flag to see something happen, so each device has its own behaviour and
you should see all of these within about a minute:

- soil sensors that dry out and raise a low moisture alert
- power meters that surge past 600W
- radios that fade below -90 dBm
- valves that report a fault, such as `StuckValve` or `LowBattery`
- nodes that go quiet for a while and come back
- a flood of 300 readings every 75 seconds (the first at about 20 seconds), followed
  by one very dry moisture reading, so a critical alert has a queue to jump

The flags are there if you want to repeat one of these on demand:

| Flag | What it does |
|---|---|
| `--critical <mac>` | After 5 seconds, floods the gateway and then sends 5% moisture from that sensor |
| `--fault <mac>` | That valve reports `StuckValve` on every reading |
| `--drop <mac>` | That device goes quiet 10 seconds in, for 40 seconds, then comes back |
| `--kill <mac>` | That relay goes off the air 10 seconds in, for 40 seconds, so whatever hears the gateway only through it is stranded |

For Part 3 every reading also lists the neighbours that device can hear, with the signal
from each. The mesh is built from those lists. The last device in the line only hears
the one before it on purpose, so there is one relay whose loss really does cut someone
off. Without `--kill`, a default run takes that relay off the air once, about a minute
in.

Both routes can run at the same time. Docker uses ports 8080 and 8081 and
`dotnet run` uses 7002 and 7267, so nothing collides.

---

## Finding the Features

Registering sensors, the deployment tree, file uploads, the signal trace and the Command
Stream page are from Parts 1 and 2. How to find them is in
[README-part1.md](README-part1.md) and [README-part2.md](README-part2.md), and they all
still work in this build.

### The Topology page

**Topology** in the top navigation opens `/topology`. It also fills from the simulator.

**The mesh drawing.** The gateway sits in the middle and the devices sit round a
circle. A green node is up, an orange one has gone quiet and a red one is cut off,
meaning it is not quiet itself but every way to the gateway goes through a device that
is. The thick links are the cheapest tree out from the gateway, and the toggle swaps
between *All links* and *Tree only*. The page reloads when the gateway pushes a change,
with a slow poll as the fallback.

**The route card.** Type a MAC address into the route box or click a node. The card
lists the hops from the gateway to that device with the signal on each one, and draws
the route over the mesh. For a quiet device it lists the neighbours that can still get
through, and for a cut off one it says so. Typing has to match the MAC exactly.

**The radio history card.** With a device picked, the card below the route shows its
signal strength, hop count, duty cycle or packet loss on a chart, with last minute,
last five minutes or all kept, and the newest fifteen readings in a table. The gateway
says it sends no readings.

**The tables.** The Devices table shows each device's state, with *Cut off* where the
drawing shows red. The Links table lists every link once with its signal.

---

## Testing the Mesh

Each of these needs the app running with the simulator. If you used Docker, use the
`8080` address in place of `7002` in the commands.

The queues, undo stack, sets and suggestion engine are tested in
[README-part2.md](README-part2.md#testing-the-queues-and-dictionaries). These two are the
Part 3 ones.

**1. Taking a relay off the air (graph, breadth first search, Prim's tree)**

1. Open Topology with the simulator running. A default run takes the relay the last
   device depends on off the air about a minute in. To pick it yourself:

```bash
dotnet run --project SmartX.Simulator -- --kill AA:BB:CC:DD:EE:21
```

2. After the relay goes quiet its node turns orange. The device behind it, EE:22, turns
   red and the Devices table says *Cut off*. The tree redraws without them.
3. Click EE:21. The route card lists the live neighbours that could still carry for it.
4. After 40 seconds the relay comes back and both nodes go green again.

If the gateway has only just started, `--kill` can take the relay off before the gateway
has heard any of its links. Then EE:21 shows red like EE:22 instead of orange, and the
route card has no neighbours to list. The default run waits a minute, so it does not
do this.

The API gives the same answer. `GET /api/topology/reach` lists who is reachable, quiet
and cut off, and `GET /api/topology/mst` returns the tree.

**2. Asking for a range of radio history (the AVL tree)**

1. On Topology, type a device's MAC into the route box, for example
   `AA:BB:CC:DD:EE:05`, or click its node.
2. In the radio history card press *Last minute*, then *Last 5 minutes*, then
   *All kept*. The chart and the table narrow and widen to match.
3. Or ask the API for a stretch of time directly. Swap the times for ones from the
   last few minutes, since the tree only holds what arrived after the simulator
   started and an older range comes back as an empty list:

```bash
curl -k "https://localhost:7002/api/topology/AA:BB:CC:DD:EE:05/history?from=2026-11-01T15:28:00Z&to=2026-11-01T15:33:00Z"
```

A MAC the mesh has never heard gives 404 and a start after the end gives 400.

---

## Final Architecture

The client is a standalone app, not hosted inside the API:

```text
Browser
  |
  v
SmartX.Web  (Blazor WebAssembly dashboard, runs in the browser)
  |  calls via HttpClient / JSON and one SignalR connection, allowed back by CORS
  v
SmartX.Api  (ASP.NET Core Web API, the gateway)
  |
  v
In-memory stores and the hand-written collections
```

**Why the client is standalone:** hosting the Blazor app inside the API would have
been less work, but then the two could never be deployed separately, and the brief
treats the client and the backend API as separate layers. Keeping them apart meant I
had to deal with CORS properly: the API allows the client's origin, and the client's
`HttpClient` points at the API's base URL from
`SmartX.Web/wwwroot/appsettings.json`. That is the same problem a real deployment
would have, so it felt like the more honest way to build it.

### How a reading travels in Part 3

Part 3 adds a second way into the same readings. Each reading can now say which other
devices that one can hear, and the gateway turns those lists into a picture of the
mesh. The first way, through the alert and packet queues, is in
[README-part2.md](README-part2.md#how-a-reading-travels-in-part-2).

```text
Device or simulator
  |  POST /api/telemetry   (reading + Neighbours: mac and signal of each device it hears)
  v
StreamProcessor
  |-- MeshGraph       the devices and links, as an adjacency list
  `-- TelemetryTree   one AVL tree of radio readings per device, keyed on time
                           |
TopologyController  <------'   breadth first search, Prim's tree, range queries
  |
  v
Topology page  (drawing, route card, radio history card)
```

The gateway is fixed at `AA:BB:CC:DD:EE:00`. It is a node in the graph but not a
registered sensor, and it sends no readings of its own.

**AvlTree** (`SmartX.Api/Collections/AvlTree.cs`). A binary search tree written by
hand that rebalances itself with rotations after every insert, so it can never lean
into a list. I only wrote insert and range query, because those are the only two
things the radio history needs. Readings with the same time share a node instead of
overwriting each other. Insert is O(log n) and a range is O(log n + k) for k results,
because a subtree is only entered if it can hold a key inside the range:

```csharp
// Everything to the left is smaller than this key, so skip it if the range starts at or after the key.
if (from.CompareTo(node.Key) < 0)
{
    Collect(node.Left, from, to, found);
}
```

It is not thread safe on its own, whoever owns it takes the lock.

**TelemetryTree** (`SmartX.Api/Collections/TelemetryTree.cs`). A dictionary from MAC
address to one `AvlTree<DateTime, TelemetryPoint>`, behind one lock. This is what
`api/topology/{mac}/history` reads. It never drops anything, so memory grows with the
number of readings. That is a known limit, and Part 2's `HistoryLog` (capped at 500)
still sits beside it.

**MeshGraph** (`SmartX.Api/Collections/MeshGraph.cs`). A `Dictionary<string, List<Edge>>`
adjacency list. A link is stored at both ends, and a link reported again only has its
signal refreshed. A neighbour the graph has not met becomes a node, so a device shows up
as soon as anyone hears it. I chose an adjacency list over a matrix because a mesh has
few links per device, so a matrix would be mostly empty. Every member takes one lock, and
the walks run while it is held so they never see a half-added link.

**Breadth first search** (`MeshGraph.FindReachable`). A queue walk out from the gateway
that never steps onto a quiet device. It sorts every other device into reachable, quiet
or cut off, and for each quiet device lists the live neighbours it has left. O(nodes +
edges). Breadth first suits it because it reaches the closest devices first and the only
question is whether a device is reached at all.

**Prim's minimum spanning tree** (`MeshGraph.FindSpanningTree`). Grows the tree out from
the gateway, always taking the cheapest link out of it. A link costs its signal strength
without the minus sign, so a weaker link costs more and the tree prefers strong ones.
The frontier is the built-in `PriorityQueue`, written around rather than rebuilt, the same
call as for the alert queue in Part 2. O(edges log nodes). Ties go to the lower MAC so
equal weights give the same tree every time. Only the signal is weighed for now, packet
loss and duty cycle are ignored.

**How it scales.** I measured the tree against a plain list scan, which is what
Part 2's history log does, and Prim at larger sizes. One machine, a Release build,
the mean of 10 runs after a warm-up, and the driver is not in the repo.

| Readings | Window | Results | AVL tree | List scan |
|---|---|---|---|---|
| 1,000 | last 60 seconds | 60 | 0.0024 ms | 0.0036 ms |
| 10,000 | last 60 seconds | 60 | 0.0020 ms | 0.0279 ms |
| 100,000 | last 60 seconds | 60 | 0.0011 ms | 0.1788 ms |
| 100,000 | middle 10% | 10,001 | 0.2460 ms | 0.1831 ms |

The tree wins when the window is small next to the data, which is what the *Last minute*
button asks for: about 160 times faster at 100,000 readings. At 1,000 readings there is
no real difference. When the window holds a tenth of everything the tree is slightly
slower, because both are then walking k results and the tree adds recursion on top. At
100,000 readings the tree is 17 levels high where the AVL limit is about 24, so the
rebalancing is working.

| Devices | Links | Prim | Breadth first |
|---|---|---|---|
| 29 | 90 | 0.05 ms | 0.008 ms |
| 501 | 1,963 | 1.05 ms | 0.14 ms |
| 5,001 | 19,952 | 8.08 ms | 1.76 ms |

The API rebuilds both on every request. At 8 ms for 5,000 devices that is fine, so I did
not add a cache. The meshes in that table are random links I generated, not the
simulator's layout.

### Projects in the Solution

| Project | What it is |
|---|---|
| `SmartX.Api` | ASP.NET Core Web API on .NET 10. The gateway, and the only thing that stores telemetry. |
| `SmartX.Web` | Blazor WebAssembly client on .NET 10. Talks to the API over HTTP and SignalR. |
| `SmartX.Shared` | Models and validation rules both sides need, so there is one copy of each. |
| `SmartX.Simulator` | Console app that acts like a fleet of devices and posts readings to the API. |
| `SmartX.Tests` | xUnit tests over the Part 2 collections, the stream processor and the suggestion engine, and the Part 3 tree and graph. |

---

## Tech Stack

| Layer | Technology | Why I Used It |
|---|---|---|
| **Backend** | ASP.NET Core Web API (`.NET 10`) | The brief mandates a .NET 10 backend. I used controllers rather than minimal APIs because grouping the routes by controller kept each file small enough to read. |
| **Frontend** | Blazor WebAssembly (`.NET 10`) | The frontend was free choice. I picked Blazor so the whole solution stays C# and the client can share model classes with the API instead of me redefining them in another language. |
| **Live updates** | SignalR | Polling alone made the page feel a second behind. The gateway now tells the page when something changed, and polling stays as the fallback. |
| **Shared models** | A class library both projects reference | I used this so a change to a model is a compile error on both sides straight away, rather than something I only find at runtime. |
| **Styling** | Bootstrap 5.3.3, vendored into `wwwroot` | I used Bootstrap for the grid and form styling, and copied it into the project instead of using a CDN so the dashboard still renders with no internet. |
| **Storage** | In-memory, with hand-written and built-in collections | The rubric wants specific structures, and telemetry is a stream where only the recent window matters, so memory fits the problem better than a database would. |
| **Priority queue** | The built-in `PriorityQueue`, inside my own locked class | I kept the built-in one and spent the effort on the parts around it: equal alerts leaving in the order they arrived, and sharing it safely between requests and the processor. |
| **Encryption** | `System.Security.Cryptography` AES-256 | The file upload criterion asks for full encryption. I streamed it in 64KB chunks so a large upload is never held in memory all at once. |
| **Device simulation** | Console app posting over HTTP | I needed a fleet to look at without owning any hardware, and posting through the real endpoint means the simulator is not a special case in the API. |
| **Testing** | xUnit | I used xUnit because it is the standard .NET test framework and it is what the test project template gives you. |
| **Containerisation** | Docker and Docker Compose | I used Compose so the gateway, dashboard and simulator start together with one command, and so a marker does not have to trust a certificate or open three terminals. |

---

## How the Brief Maps to the Code

The brief names specific language features and data structures it wants demonstrated.
This is where each one lives, so they are easy to find:

| Requirement | Where it lives | How it works |
|---|---|---|
| **A tree written by hand** | `SmartX.Api/Collections/AvlTree.cs` | A self-balancing binary search tree with insert and range query. Left and right rotations keep the height near log n, and equal keys share a node. |
| **Timestamped telemetry and range queries** | `SmartX.Api/Collections/TelemetryTree.cs`, `TopologyController.cs` | One AVL tree per device keyed on the time of the reading. `GET api/topology/{mac}/history` takes `from` and `to` and walks only the part of the tree that can hold them. |
| **A mesh graph** | `SmartX.Api/Collections/MeshGraph.cs` | An adjacency list of devices and links, fed by the neighbour list each reading carries. Each link carries its signal strength. |
| **Breadth first search** | `MeshGraph.FindReachable` | Walks out from the gateway without going through a quiet device, and reports reachable, quiet and cut off devices with the live neighbours each quiet one has left. |
| **Minimum spanning tree** | `MeshGraph.FindSpanningTree` | Prim's algorithm over the signal strength, so a hop on a weak link costs more than one on a strong link. |
| **Thread safety** | `MeshGraph`, `TelemetryTree` | One lock per structure. A test walks the graph while another thread keeps adding links. |
| **IoT specifics** | `SmartX.Shared/Models/TelemetryPoint.cs`, `NeighbourLink.cs` | MAC address, signal strength, hop count, duty cycle and packet loss on every radio reading. |
| **Showing it working** | `SmartX.Web/Pages/Topology/Topology.razor` | The mesh drawing, the route card and the radio history card. |

---

## Docker and Containerisation

### Why Docker

The problem Docker solves here is the *"it works on my machine"* one. Running the
project by hand needs the right SDK, a trusted https certificate and three terminals
started in the right order. With Compose it is one command and none of that setup.

### My Docker Setup

```text
docker-compose.yml
  |-- api          -> ASP.NET Core gateway, port 8080
  |-- web          -> Blazor WebAssembly build served by nginx, port 8081
  `-- simulator    -> console app posting readings, no ports
```

| Service | Image build | Ports (host:container) | Notes |
|---|---|---|---|
| `api` | `SmartX.Api/Dockerfile` | `8080:8080` | Runs in Development so the OpenAPI document is served. |
| `web` | `SmartX.Web/Dockerfile` | `8081:80` | Built with the .NET SDK, then the static output is served by `nginx:alpine`. |
| `simulator` | `SmartX.Simulator/Dockerfile` | none | `depends_on` the api, then sleeps fifteen seconds before its first post. |

The containers find each other by service name on Compose's internal network. The
simulator posts to `http://api:8080/`, which only works inside the network. The
dashboard is different: the Blazor app runs in the browser, not in the container, so
it has to reach the API at `http://localhost:8080/` from outside. That is why the
api service sets `Cors__AllowedOrigins__0=http://localhost:8081`, to allow the
dashboard's origin back.

The simulator runs with no flags in Compose, so all of its behaviours show up there
too. Because storage is in memory there are no volumes. Stopping the containers clears
the readings, which is the same thing that happens when the API restarts.

---

## Testing

Tests live in `SmartX.Tests` and use **xUnit**.

```bash
dotnet test SmartX.Tests/SmartX.Tests.csproj
```

**Latest result: 50/50 tests passing**

Part 3 follows the same rule as Part 2, with a few tests on the tree and the graph and
nothing on the page. The 50 includes the Part 2 tests, which still run.

| Test class | What it covers |
|---|---|
| The Part 2 test classes | The queues, dictionaries, sets, stream processor and suggestion engine, covered in [README-part2.md](README-part2.md#testing). |
| `AvlTreeTests` | Ascending keys staying balanced, every three-key shape rotating to height two, the height staying logarithmic under a large load, both ends of a range included, and a repeated key keeping every value in order. |
| `TelemetryTreeTests` | Two devices keeping their readings apart, and no reading lost with many threads. |
| `MeshGraphTests` | A link reported from both ends stored once, the walk from the gateway with a middle device down, a ring with one down, a split mesh, and a walk running while links are still being added. |
| `MeshGraphSpanningTreeTests` | The cheapest links being taken, a quiet device being left out with the tree going round it, and equal weights giving the same tree every time. |

---

## Project Structure

```text
SmartX/
  SmartX.slnx                 Solution file, XML format
  SmartX.slnLaunch            Visual Studio multi-project launch profile
  docker-compose.yml

  SmartX.Api/                 The gateway
    Collections/              RingBuffer, TelemetryBatches, SensorTelemetry,
                              PacketQueue, AlertQueue, CommandHistory, HistoryLog,
                              ActiveFaults, PendingCommands, TransitionModel,
                              AvlTree, TelemetryTree, MeshGraph
    Controllers/              Sensors, Telemetry, Deployment, Attachments,
                              Stream, Commands, Insights, Topology
    Hubs/                     StreamHub, the live push to the browser
    Models/
      Telemetry/              TelemetryPacket, MoistureReading, PowerReading, DTOs
      Attachments/            Attachment metadata
      Stream/                 Packets, alerts, commands and settings for Part 2
    Services/                 Stores, encryption, validator, health, demo fleet,
                              DeviceCache, AlertRules, StreamProcessor,
                              CommandService, InsightService
    uploads/                  Encrypted attachments on disk
    Program.cs                Startup, DI, CORS, the hub, demo fleet seeding
    Dockerfile

  SmartX.Web/                 The dashboard
    Pages/
      Home.razor              Landing page with the three pillars
      Ingestion/              Overview, Fleet, Deployment, Files
      CommandStream/          The Part 2 dashboard
      Topology/               The Part 3 mesh page
    Components/               SignalRibbon, Sparkline, StatTile, tree editor,
                              TimelineChart, SuggestionsPanel, UndoPanel
    Layout/                   Nav and main layout
    Services/                 Formatting, tree helpers, DashboardState
    wwwroot/
      lib/bootstrap/          Bootstrap 5.3.3, vendored
    nginx.conf                Used by the Docker image only
    Dockerfile

  SmartX.Shared/
    Models/                   DTOs both sides use

  SmartX.Simulator/           Console app acting as the device fleet
    Dockerfile

  SmartX.Tests/               xUnit tests
```

---

## Key API Endpoints

Every route is a controller action, plus one SignalR hub. `Program.cs` only calls
`MapOpenApi()` in Development, `MapControllers()` and the hub.

The Part 1 and Part 2 routes (sensors, telemetry, deployment, attachments, stream,
commands and insights) are listed in [README-part2.md](README-part2.md#key-api-endpoints).
The Part 3 routes are these:

### Topology

| Method | Route | What it does |
|---|---|---|
| `GET` | `/api/topology` | Every device and link the mesh knows about, with each device's hop count and whether it is quiet or cut off. |
| `GET` | `/api/topology/reach` | Who can reach the gateway with the quiet devices taken out, who is cut off, and the live neighbours of each quiet device. |
| `GET` | `/api/topology/mst` | The cheapest set of links from the gateway to every live device. |
| `GET` | `/api/topology/{macAddress}/history` | A device's radio readings, oldest first. Takes `from` and `to`. 404 if the mesh never heard it, 400 if the start is after the end. |

The live updates use a SignalR hub at `/hubs/stream`. The OpenAPI document is at
`/openapi/v1.json` when the API runs in Development.

---

## Notes for the Marker

- **The AES key ships in `SmartX.Api/appsettings.json` on purpose**, so attachments
  work the moment the project is cloned with nothing to configure. A real deployment
  would keep it in user secrets or an environment variable.
- **Storage is in memory.** Restarting the gateway clears every reading, override and
  attachment and reseeds the demo fleet. That is a deliberate choice, since the brief
  is about the ingestion pipeline and the data structures rather than persistence.
- **The AVL tree is written by hand, Prim's queue is not.** The tree has insert and range
  query only, since nothing needs a delete. Prim uses the built-in `PriorityQueue`.
- **Part 3 things I left rough.** The radio history has no cap, so memory grows with
  the readings, and the backfill the simulator posts at the start never reaches it, only
  live readings do. A link stays in the mesh until the gateway restarts, with whichever end
  reported it last setting its signal. Prim weighs signal only. The drawing puts devices
  on a fixed circle, which gets crowded past about 40 devices. A device's hop count is
  the last packet's, so it can differ from the length of the cheapest route on the
  page (one soil sensor says 3 and the route shows 4). A quiet device is judged by the
  gateway's 15 second limit, and a MAC typed in the route box has to match exactly.
- **I could not run Docker on the machine I wrote Part 3 on.** The compose file and the
  three Dockerfiles are unchanged from Part 2, and I checked every page with
  `dotnet run`, but `docker compose up --build` is the one thing I have not seen work
  for Part 3.

---

## Demo

Video walkthrough: *add the Part 3 video link here*

---

## AI Assistance Declaration

AI tools were used as support during the project, but I reviewed and adjusted the
final implementation and testing myself.

- **GitHub Copilot** was used to assist with the testing setup and to help define the
  scope of the automated tests.
- **ChatGPT** was used to assist with generating site style visuals and styling ideas
  for the dashboard and related frontend presentation.
- **ChatGPT** was used to assist with the formatting and style visuals of the README
  document.

---
