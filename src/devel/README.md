# Development Environment
This directory contains a Docker Compose definition for standing up local resources for development purposes.

Ensure you have a Docker network created named 'dftb' first:

```
docker network create dftb
```

Once done, start the requisite SQL Server and Azurite storage emulator instances by changing into the devel folder running:

```
docker-compose up
```

This will get you:

1. A SQL Server instance (password is 'P@ssw0rd!', without quotes.)
2. An Azurite Storage emulator instance 

The SQL Server instance is auto-initialized with the needed database on startup, but this database is not persisted outside the container and will be destroyed when the container is removed.

Scripts to initialize the SQL Server instance are found in the ../data/ directory.

## Event Processing
The 'eventproc' project contains the asynchronous data event processing code. This is intended to run in production in a Docker container. To build this Docker image, from the 'src' folder, run:

```
docker build -f eventproc/Dockerfile -t dftb/eventproc:devel .
```

This will build the eventproc project and generate the dftb/eventproc:devel Docker image locally.

In order to execute this Docker container and process any events in the local development Azurite storage emulator queues:

```
docker run --net dftb -e ConnectionStrings__dftb='Server=database;Database=dftb;User Id=sa;Password=P@ssw0rd!' -e ConnectionStrings__storage='AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://storage:10000/devstoreaccount1;QueueEndpoint=http://storage:10001/devstoreaccount1;TableEndpoint=http://storage:10002/devstoreaccount1;' dftb/eventproc:devel
```
