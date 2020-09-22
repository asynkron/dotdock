# Dotdock

## dotnet multi project dockerfile generator

Are you frustrated with writing weird docker configuration files by hand?

Do you forget the syntax until the next time you need to write a new one?

You just want to write code and the other stuff should just work?

Then this is the tool for you!

I already had enough, so I wrote this thing for you!

Dotdock is a CLI wizard that will generate the dockerfile for you.

### Install:

```
❯dotnet tool install -g dotdock
```

### Example usage:

```
❯ cd protoactor-dotnet
❯ dotdock

Using /users/rogerjohansson/git/protoactor-dotnet/ProtoActor.sln

1) Chat.Messages
2) Client
3) ClusterBenchmark
4) ContextDecorators
5) EscalateSupervision
6) Futures
7) HelloWorld
8) InprocessBenchmark
9) LifecycleEvents
10) MailboxBenchmark
11) Messages
12) Middleware
13) Node1
14) Node2
15) Proto.Actor
16) Proto.Cluster
17) Proto.Cluster.Consul
18) Proto.Cluster.Kubernetes
19) Proto.Cluster.MongoIdentityLookup
20) Proto.Persistence
21) Proto.Remote
22) ProtoActorBenchmarks
23) ProtoGrainGenerator
24) ReceiveTimeout
25) Router
26) Saga
27) Server
28) SpawnBenchmark
29) Supervision

Select project to run> 28
Run tests? [Y/n] Y

1) mcr.microsoft.com/dotnet/core/sdk:3.1
2) mcr.microsoft.com/dotnet/sdk:5.0

Select build image> 1

1) mcr.microsoft.com/dotnet/aspnet:5.0
2) mcr.microsoft.com/dotnet/core/aspnet:3.1
3) mcr.microsoft.com/dotnet/core/runtime:3.1
4) mcr.microsoft.com/dotnet/runtime:5.0

Select app image> 3

Wrote docker file to 'dockerfile'
```

### Resulting dockerfile:

```
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy nugetconfigs 

# Copy csprojs 
COPY src/Proto.Actor/Proto.Actor.csproj ./src/Proto.Actor/Proto.Actor.csproj
COPY tests/Proto.Actor.Tests/Proto.Actor.Tests.csproj ./tests/Proto.Actor.Tests/Proto.Actor.Tests.csproj
COPY benchmarks/SpawnBenchmark/SpawnBenchmark.csproj ./benchmarks/SpawnBenchmark/SpawnBenchmark.csproj

# Restore project
RUN dotnet restore "benchmarks/SpawnBenchmark/SpawnBenchmark.csproj"

# Run tests
RUN dotnet test

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out "benchmarks/SpawnBenchmark/SpawnBenchmark.csproj"

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "SpawnBenchmark.dll"]
```