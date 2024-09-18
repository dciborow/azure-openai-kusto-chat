import React from 'react';
import { Stack, Text, Persona, PersonaSize } from '@fluentui/react';

interface MessageProps {
    sender: string;
    text: string;
    timestamp: string;
}

const Message: React.FC<MessageProps> = React.memo(({ sender, text, timestamp }) => {
    const isUser = sender === 'User';

    return (
        <Stack
            horizontal
            tokens={{ childrenGap: 10 }}
            styles={{
                root: {
                    marginBottom: 10,
                    flexDirection: isUser ? 'row-reverse' : 'row',
                    alignItems: 'flex-start',
                },
            }}
        >
            <Persona
                text={sender}
                size={PersonaSize.size32}
                styles={{ root: { alignSelf: 'flex-start' } }}
            />
            <Stack styles={{ root: { maxWidth: '70%' } }}>
                <Text variant="smallPlus">{timestamp}</Text>
                <Text variant="medium">{text}</Text>
            </Stack>
        </Stack>
    );
});

export default Message;
