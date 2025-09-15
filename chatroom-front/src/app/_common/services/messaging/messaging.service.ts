import { Injectable, signal } from '@angular/core';
import { environment } from 'src/environments/environment';
import { ChatMessage } from '../../models/chat-message.model';
import { ChatRoom } from '../../models/chat-room.model';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

@Injectable({
    providedIn: 'root',
})
export class MessagingService {

    public chatRooms = signal<ChatRoom[]>([]);

    public messages = signal<ChatMessage[]>([]);

    public activeRoomId = signal<string | null>(null);
    
    private _hubConnection: HubConnection;
    private connectionPromise: Promise<void>;

    constructor() {
        this._hubConnection = new HubConnectionBuilder()
            .withUrl(environment.API_URL + '/hub/messaging')
            .withAutomaticReconnect() 
            .configureLogging(LogLevel.Information)
            .build();
        this.connectionPromise = this.startConnection();
        this.attachListeners();
    }

    private async startConnection(): Promise<void> {
        try {
            await this._hubConnection.start();
            console.log('SignalR Connection Established!');
        } catch (err) {
            console.error('SignalR Connection Error: ', err);
        }
    }
    
    private attachListeners(): void {
        this._hubConnection.on('ChatRoomCreated', (newRoom: ChatRoom) => {
            console.log('Broadcast received: New room created!', newRoom);
            this.chatRooms.update(currentRooms => [...currentRooms, newRoom]);
        });

        this._hubConnection.on('NewMessage', (newMessage: ChatMessage) => {
            console.log('New message received:', newMessage);
            if (newMessage.roomId === this.activeRoomId()) {
                this.messages.update(currentMessages => [...currentMessages, newMessage]);
            }
        });
        
    }

    private async ensureConnection(): Promise<void> {
        await this.connectionPromise;
        if (this._hubConnection.state !== HubConnectionState.Connected) {
             await this.startConnection();
        }
    }

    public async loadInitialChatRooms(): Promise<void> {
        try {
            await this.ensureConnection();
            const rooms = await this._hubConnection.invoke<ChatRoom[]>('GetAllChatRooms');
            this.chatRooms.set(rooms);
            console.log('Initial chat rooms loaded:', rooms);
        } catch (err) {
            console.error('Failed to load initial chat rooms:', err);
            console.log('Connection state at time of error:', this._hubConnection.state);
        }
    }

    public async createChatRoom(name: string): Promise<ChatRoom> {
        await this.ensureConnection();
        return await this._hubConnection.invoke<ChatRoom>('CreateChatRoomWithName', name);
    }
    public async getChatRoom(roomId: string): Promise<ChatRoom> {
        await this.ensureConnection(); 
        return await this._hubConnection.invoke<ChatRoom>('GetChatRoom', roomId);
    }

    public async joinChatRoom(roomId: string): Promise<void> {
        await this.ensureConnection(); 
         const oldRoomId = this.activeRoomId();
        if (oldRoomId) {
            await this._hubConnection.invoke('LeaveChatRoom', oldRoomId);
        }
        const history = await this._hubConnection.invoke<ChatMessage[]>('JoinChatRoom', roomId);
        
        this.activeRoomId.set(roomId);
        this.messages.set(history);
        console.log(`Joined room ${roomId} and loaded history:`, history);
    
    }

    public async leaveChatRoom(roomId: string): Promise<void> {
        await this.ensureConnection(); 
        await this._hubConnection.invoke('LeaveChatRoom', roomId);
    }
    
    public async sendMessage(roomId: string, message: string): Promise<any> {
        await this.ensureConnection(); 
        await this._hubConnection.invoke('SendMessage', roomId, message);
    }
}