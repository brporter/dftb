# Development Environment
This directory contains a Docker Compose definition for standing up local resources for development purposes.

1. A SQL Server instance (password is 'P@ssw0rd!', without quotes.)
2. An Azurite Storage emulator instance 

The SQL Server instance is auto-initialized with the needed database on startup, but this database is not persisted outside the container and will be destroyed when the container is removed.

Scripts to initialize the SQL Server instance are found in the data/ directory.
