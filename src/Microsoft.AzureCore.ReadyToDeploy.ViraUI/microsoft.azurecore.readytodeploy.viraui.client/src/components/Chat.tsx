import { useState, useEffect, useRef } from 'react';
import { TextField, PrimaryButton, Stack, Text, MessageBar, MessageBarType } from '@fluentui/react';
import Message from './Message';
import * as signalR from '@microsoft/signalr';

interface MessageType {
    sender: string;
    text: string;
    timestamp: string;
}

function Chat() {
    const [messages, setMessages] = useState<MessageType[]>([]);
    const [userInput, setUserInput] = useState<string>('');
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const [isAssistantTyping, setIsAssistantTyping] = useState<boolean>(false);
    const [errorMessage, setErrorMessage] = useState<string>('');
    const messagesEndRef = useRef<HTMLDivElement | null>(null);

    useEffect(() => {
        const userId = getUserId();

        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`https://localhost:5173/chatHub?userId=${userId}`, {
                withCredentials: true,
            })
            .withAutomaticReconnect()
            .build();

        setConnection(newConnection);
    }, []);

    useEffect(() => {
        if (connection) {
            connection
                .start()
                .then(() => {
                    console.log('Connected to SignalR hub');

                    connection.on('ReceiveMessage', (sender: string, message: string) => {
                        const newMessage: MessageType = {
                            sender,
                            text: message,
                            timestamp: new Date().toLocaleTimeString(),
                        };
                        setMessages(prevMessages => [...prevMessages, newMessage]);
                    });

                    connection.on('AssistantTyping', (isTyping: boolean) => {
                        setIsAssistantTyping(isTyping);
                    });

                    connection.onclose(error => {
                        if (error) {
                            console.error('SignalR connection closed with error:', error);
                            setErrorMessage('Connection lost. Reconnecting...');
                        } else {
                            console.log('SignalR connection closed.');
                        }
                    });
                })
                .catch(error => {
                    console.error('SignalR connection error:', error);
                    setErrorMessage('Failed to connect to the server.');
                });
        }
    }, [connection]);

    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    const sendMessage = async () => {
        if (userInput.trim() === '' || !connection) return;

        // Add user's message to the chat
        const userMessage: MessageType = {
            sender: 'User',
            text: userInput,
            timestamp: new Date().toLocaleTimeString(),
        };
        setMessages(prevMessages => [...prevMessages, userMessage]);

        try {
            await connection.invoke('SendMessage', userInput);
            setUserInput('');
        } catch (error) {
            console.error('Error sending message:', error);
            setErrorMessage('Failed to send message.');
        }
    };

    function getUserId() {
        let userId = localStorage.getItem('userId');
        if (!userId) {
            userId = generateUUID();
            localStorage.setItem('userId', userId);
        }
        return userId;
    }

    function generateUUID() {
        // Simple UUID generator
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            const r = (Math.random() * 16) | 0,
                v = c === 'x' ? r : (r & 0x3) | 0x8;
            return v.toString(16);
        });
    }

    return (
        <Stack tokens={{ childrenGap: 10 }}>
            {errorMessage && (
                <MessageBar
                    messageBarType={MessageBarType.error}
                    isMultiline
                    onDismiss={() => setErrorMessage('')}
                    dismissButtonAriaLabel="Close"
                >
                    {errorMessage}
                </MessageBar>
            )}
            <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
                {messages.map((msg, index) => (
                    <Message
                        key={index}
                        sender={msg.sender}
                        text={msg.text}
                        timestamp={msg.timestamp}
                    />
                ))}
                {isAssistantTyping && (
                    <Text variant="small" styles={{ root: { fontStyle: 'italic' } }}>
                        Assistant is typing...
                    </Text>
                )}
                <div ref={messagesEndRef} />
            </div>
            <Stack horizontal tokens={{ childrenGap: 5 }}>
                <TextField
                    value={userInput}
                    onChange={(_, newValue) => setUserInput(newValue || '')}
                    placeholder="Type your message..."
                    styles={{ root: { width: '100%' } }}
                />
                <PrimaryButton
                    text="Send"
                    onClick={sendMessage}
                    disabled={!connection || userInput.trim() === ''}
                />
            </Stack>
        </Stack>
    );
}

export default Chat;
