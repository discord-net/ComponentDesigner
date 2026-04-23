import type { APIMessageComponent } from 'discord-api-types/v10';
import './app.scss';
import './components.scss'
import DiscordComponent from './components/DiscordComponent';
import { useEffect, useState } from 'react';
import Message from './message';

const defaultComponents: APIMessageComponent[] = [
    {
        "type": 17,
        "components": [
            {
                "type": 10,
                "content": "# Hello World\n# 2"
            },
            {
                "type": 14
            },
            {
                "type": 1,
                "components": [
                    {
                        "type": 2,
                        "style": 1,
                        "label": "Hello",
                        "custom_id": "a"
                    },
                    {
                        "type": 2,
                        "style": 2,
                        "label": "Hello",
                        "custom_id": "b"
                    },
                    {
                        "type": 2,
                        "style": 3,
                        "label": "Hello",
                        "custom_id": "c"
                    },
                    {
                        "type": 2,
                        "style": 4,
                        "label": "Hello",
                        "emoji": {
                            "name": "\uD83D\uDE2D"
                        },
                        "custom_id": "d"
                    }
                ]
            },
            {
                "type": 14,
                "spacing": 2
            },
            {
                "type": 9,
                "components": [
                    {
                        "type": 10,
                        "content": "Foo"
                    }
                ],
                "accessory": {
                    "type": 11,
                    "media": {
                        "url": "https://media.discordapp.net/attachments/975787830355820564/1492966846415245463/image.png?ex=69e91eba\u0026is=69e7cd3a\u0026hm=7ce9020c5cc638d3cf3291831c80fa5fdd4fc8c2263209431c4270a1003cb847\u0026=\u0026format=webp\u0026quality=lossless\u0026width=475\u0026height=266"
                    }
                }
            }
        ]
    }
];


const App = () => {
  const [components, setComponents] = useState<APIMessageComponent[]>(defaultComponents); 

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
        <Message components={components} />
      </div>
    </main>
  );
};

export default App;
