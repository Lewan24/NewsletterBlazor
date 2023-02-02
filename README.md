# NewsletterBlazor
Newsletter application made in .NET 7 MVC

Application for internal bussiness problem
// Need to send mass information emails to students

Newsletter is based on Identity authorization

Application needs appsettings.json file to work
Example:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=Newsletter;User Id=root;Password=toor"
  },
  "ServerConfig": {
    "Server": "server-link",
    "Port": "587",
    "Email": "sender",
    "Password": "1234abcd"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Its default settings json with basic needed configuration to work properly.

App needs also databse, here its using postgresql
You can use docker-compose.yml like it:
```Dockerfile
version: "3.9"
services:
  postgres:
    image: postgres
    environment:
      POSTGRES_USER: root
      POSTGRES_PASSWORD: toor
      POSTGRES_DB: chat
    ports:
      - "5432:5432/tcp"
    networks:
      - demo-net
    deploy:
      restart_policy:
        condition: on-failure
  adminer:
    image: adminer:latest
    ports:
      - "8180:8080/tcp"
    networks:
      - demo-net
    deploy:
      restart_policy:
        condition: on-failure

networks:
  demo-net:
```

You can find complete docker-compose in project files.