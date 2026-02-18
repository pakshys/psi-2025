# psi-2025

Software engineering course group project - live music jam session.

## Planned features

### Room management:

- public/private party rooms

### Jam session:

- streaming services integration
- vote operations (shuffle, skip, etc.)
- volume control
- song ratings
- individual playlist creation
- songs history tracker

### User profiles:

- register/login
- view profiles

### Social features:

- chat
- friends

## Running the project

### Prerequisites

- .NET SDK 8.0.414 (or later)
- Docker
- Node.js

### Instructions

**1. Start the database (PostgreSQL via Docker)**

Open terminal and run the command:

```
docker run --name psi-database \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_DB=identity \
  -p 5432:5432 \
  -d postgres:15
```

**2. Start the backend**

Navigate to the backend folder and run the command:

```
dotnet run
```

**3. Install frontend dependencies**

Navigate to the frontent folder and install the required packages:

```
npm install
```

**4. Start the frontend**

After installation completes, run the command:

```
npm run dev
```

Now the application can be accessed locally on http://localhost:5173.

Application works best on firefox.
