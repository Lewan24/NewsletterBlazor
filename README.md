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
	"EmailName": "Name of sender like no-reply"
  },
  "Administration": {
    "Admin": "admin's email"
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

When you download project:
- Run "docker-compose up" when in folder that contains that file
- Then Create appsetings.json with own settings with email provider information
- Place json in project folder (where is Program.cs)
- Run "docker build -t MY_IMAGE_NAME ." while being in folder that contains dockerfile
- Then run "docker run -dp OPPENED_PORT:CONTAINER_PORT MY_IMAGE_NAME --name CONTAINER_NAME"

After these actions, the app should be working in the background in docker container,
database also, remember that the app is using database with configuration and credentials that were set in
appsettings.json