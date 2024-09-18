import { Stack, Text } from '@fluentui/react';
import Chat from './components/Chat';

function App() {
    return (
        <Stack tokens={{ childrenGap: 15 }} styles={{ root: { margin: '20px' } }}>
            <Text variant="xxLarge">Vira Chat</Text>
            <Chat />
        </Stack>
    );
}

export default App;
