import type { APIMessageComponent } from 'discord-api-types/v10';
import './app.scss';
import './components.scss'
import DiscordComponent from './components/DiscordComponent';
import { useEffect, useState } from 'react';


const App = () => {
  const [components, setComponents] = useState<APIMessageComponent[]>([]); 

  useEffect(() => {
    window.addEventListener('message', event => {
      console.log("Received message", event.data);
      
      if (event.data.type === 'updateComponents') {
        setComponents(event.data.components);
      }
    })
  }, []);

  return (
    <main className="main">
      <div className="preview-container">
        {components.map((c) => (
          <DiscordComponent {...c} />
        ))}
      </div>
    </main>
  );
};

export default App;
