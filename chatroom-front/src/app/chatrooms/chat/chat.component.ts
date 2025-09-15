// DANS src/app/chat/chat.component.ts
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MessagingService } from 'src/app/_common/services/messaging/messaging.service';
import { ChatRoom } from 'src/app/_common/models/chat-room.model';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule], 
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit {
  private readonly messagingService = inject(MessagingService);

  public chatRooms = this.messagingService.chatRooms;
  public selectedRoom = signal<ChatRoom | null>(null);

  constructor() { }

  ngOnInit(): void {
    this.messagingService.loadInitialChatRooms();
  }

  selectRoom(room: ChatRoom): void {
    this.selectedRoom.set(room);
    console.log('Room selected:', room);
  }
}