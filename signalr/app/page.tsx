"use client"

import { useState, useEffect, useRef } from "react"
import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr"

import { Button, BorderedContainer, LoginView } from "ui-exercices-5w5"
import ChatComponent from "@/components/chat/chat"

const serverUrl = "http://localhost:5106/"
const loginUrl = serverUrl + "api/Account"
const hubUrl = serverUrl + "chat"

export default function Home() {

  const [hubConnection, setHubConnection] = useState<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);


  function connectToHub() {
    const newHubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => sessionStorage.getItem("token")! })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    newHubConnection.on("MostPopularChannel", (msg : string) => {
      alert(msg)
    })

    newHubConnection
      .start()
      .then(() => {
        console.log("Connecté au Hub");
        setIsConnected(true);
      })
      .catch(err => console.log('Error while starting connection: ' + err))

    setHubConnection(newHubConnection);
  }

  function logout() {
    console.log("L'utilisateur se déconnecte, on arrête le HubConnection");
    if (hubConnection) {
      hubConnection.stop();
      setHubConnection(null);
    }
    setIsConnected(false);
  }

  function RenderContent() {
    if (!isConnected) {
      return (
        <div>
          <div >Pas connecté au Hub..</div>
          <br></br>
          <Button variant="secondary" onClick={connectToHub}>Se connecter au Hub</Button>
        </div>
      );
    }
    else {
      return (
        <div>
          <div>Connecté!</div>
          <ChatComponent hubConnection={hubConnection} />
        </div>
      );
    }
  }

  return (
    <div>
      <h1 className="text-3xl font-bold mb-2 p-2">Chat SignalR</h1>
      <div className="p-2 max-w-[1400px]">
        <LoginView apiUrl={loginUrl} onLogout={logout} />
        <BorderedContainer className="p-6 mt-2">
          {RenderContent()}
        </BorderedContainer>
      </div>
    </div>
  );
}
