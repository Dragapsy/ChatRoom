import { Injectable, signal } from '@angular/core';
import { environment } from 'src/environments/environment';
import { ChatMessage } from '../../models/chat-message.model';
import { ChatRoom } from '../../models/chat-room.model';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { UserDto } from '../../dto/user.dto';
import { Subject } from 'rxjs';

@Injectable({
    providedIn: 'root',
})
export class MessagingService {

    public chatRooms = signal<ChatRoom[]>([]);
    public joinedRooms = signal<ChatRoom[]>([]); 
    public messages = signal<ChatMessage[]>([]);
    public activeRoomId = signal<string | null>(null);
    public userActivity$ = new Subject<string>();
    
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
        } catch (err) {
            console.error('SignalR Connection Error: ', err);
        }
    }
    
    private attachListeners(): void {
        this._hubConnection.on('ChatRoomCreated', (newRoom: ChatRoom) => {
            this.chatRooms.update(currentRooms => [...currentRooms, newRoom]);
        });

        this._hubConnection.on('NewMessage', (newMessage: ChatMessage) => {
            if (newMessage.roomId === this.activeRoomId()) {
                this.messages.update(currentMessages => [...currentMessages, newMessage]);
            }
        });
        
        this._hubConnection.on('UserJoined', (roomId: string, newUser: UserDto) => {

            const updateFn = (rooms: ChatRoom[]) => rooms.map(room => {
                if (room.id === roomId && !room.participants.some(p => p.id === newUser.id)) {
                    return { ...room, participants: [...room.participants, newUser] };
                }
                return room;
            });

            this.joinedRooms.update(updateFn);
            this.chatRooms.update(updateFn);

            if (roomId === this.activeRoomId()) {
                const notificationMessage = `${newUser.firstName} a rejoint la conversation.`;
            
                this.userActivity$.next(notificationMessage);
            }
        });

        this._hubConnection.on('UserLeft', (roomId: string, userId: string) => {
            
            let userName = 'Quelqu un';
            const roomBeforeUpdate = this.joinedRooms().find(r => r.id === roomId);
            if (roomBeforeUpdate) {
                const user = roomBeforeUpdate.participants.find(p => p.id === userId);
                
                if (user && user.firstName) { 
                    userName = user.firstName; 
                }
            }
            
            const updateFn = (rooms: ChatRoom[]) => rooms.map(room => {
                if (room.id === roomId) {
                    return { ...room, participants: room.participants.filter(p => p.id !== userId) };
                }
                return room;
            });
            
            this.joinedRooms.update(updateFn);
            this.chatRooms.update(updateFn);

            if (roomId === this.activeRoomId()) {
                this.userActivity$.next(`${userName} a quitté la conversation.`);
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
        } catch (err) {
            console.error('Failed to load initial chat rooms:', err);
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

    public async joinChatRoom(room: ChatRoom): Promise<void> {
        if (this.joinedRooms().some(r => r.id === room.id)) {
            await this.switchActiveRoom(room.id);
            return;
        }
        await this.ensureConnection(); 
        const oldRoomId = this.activeRoomId();
        if (oldRoomId) {
            await this._hubConnection.invoke('LeaveChatRoom', oldRoomId);
        }
        const history = await this._hubConnection.invoke<ChatMessage[]>('JoinChatRoom', room.id);

        this.joinedRooms.update(currentRooms => [...currentRooms, room]);
        this.activeRoomId.set(room.id);
        this.messages.set(history);
    }

    public async leaveChatRoom(roomId: string): Promise<void> {
        await this.ensureConnection(); 
        await this._hubConnection.invoke('LeaveChatRoom', roomId);
    }
    
    public async sendMessage(roomId: string, message: string): Promise<any> {
        await this.ensureConnection(); 
        await this._hubConnection.invoke('SendMessage', roomId, message);
    }

    public async switchActiveRoom(roomId: string): Promise<void> {
        if (this.activeRoomId() === roomId) return;
        await this.ensureConnection();
        const history = await this._hubConnection.invoke<ChatMessage[]>('GetMessageHistory', roomId); 
        this.activeRoomId.set(roomId);
        this.messages.set(history);
    }
}