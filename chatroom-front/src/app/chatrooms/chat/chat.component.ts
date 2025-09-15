// DANS src/app/chat/chat.component.ts
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MessagingService } from 'src/app/_common/services/messaging/messaging.service';
import { ChatRoom } from 'src/app/_common/models/chat-room.model';
import { CreateComponent } from '../create/create.component';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, CreateComponent], 
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit {
  private readonly messagingService = inject(MessagingService);

  public chatRooms = this.messagingService.chatRooms;
  public selectedRoom = signal<ChatRoom | null>(null);

  public messages = this.messagingService.messages;

  public showCreateModal = signal(false);

  public roomError = signal<string | null>(null);

  constructor() { }

  ngOnInit(): void {
    this.messagingService.loadInitialChatRooms();
  }

  async selectRoom(room: ChatRoom): Promise<void> {
    this.roomError.set(null);

    if (this.selectedRoom()?.id === room.id) {
      return;
    }
    
    try {
      console.log('Joining room:', room.name);
      this.selectedRoom.set(room);
      await this.messagingService.joinChatRoom(room.id);
    } catch (e: any) {
      console.error("Failed to join room:", e);

      let friendlyErrorMessage = "Une erreur est survenue."; 
      if (e && e.message) {
        if (e.message.includes('HubException:')) {
          friendlyErrorMessage = e.message.split('HubException: ')[1];
        } else {
          friendlyErrorMessage = e.message;
        }
      }

      this.roomError.set(friendlyErrorMessage);
      this.selectedRoom.set(null);
    }
  }
  onRoomCreated(): void {
    this.showCreateModal.set(false);
  }

  openCreateModal(): void {
    this.showCreateModal.set(true);
  }
}