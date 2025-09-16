import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgIconComponent, provideIcons } from '@ng-icons/core';
import { saxAddSquareBulk, saxMenuBulk, saxPeopleBulk } from '@ng-icons/iconsax/bulk';
import { MessagingService } from 'src/app/_common/services/messaging/messaging.service';
import { ChatRoom } from 'src/app/_common/models/chat-room.model';
import { CreateComponent as ChatroomsCreateComponent } from '../create/create.component';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, ChatroomsCreateComponent, NgIconComponent],
  providers: [provideIcons({ saxAddSquareBulk, saxMenuBulk, saxPeopleBulk  }) ],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit {
  private readonly messagingService = inject(MessagingService);

  public allRooms = this.messagingService.chatRooms;
  public joinedRooms = this.messagingService.joinedRooms;
  public messages = this.messagingService.messages;
  
  public selectedRoom = signal<ChatRoom | null>(null);

  public showCreateModal = signal(false);
  public roomError = signal<string | null>(null);
  public activeView = signal<'joined' | 'all'>('joined');

  public isSidebarCollapsed = signal(false);

  public showParticipants = signal(false);

  constructor() { }

  ngOnInit(): void {
    this.messagingService.loadInitialChatRooms();
  }

  async joinNewRoom(room: ChatRoom): Promise<void> {
    this.roomError.set(null);
    try {
      await this.messagingService.joinChatRoom(room);
      this.selectedRoom.set(room); 
      this.activeView.set('joined');
      this.isSidebarCollapsed.set(true);
    } catch (e: any) {
      this.handleError(e);
    }
  }

  async switchRoom(room: ChatRoom): Promise<void> {
    this.roomError.set(null);
    await this.messagingService.switchActiveRoom(room.id);
    this.selectedRoom.set(room); 
    this.isSidebarCollapsed.set(true);
  }

  getAuthorName(authorId: string): string {
    const room = this.selectedRoom();
    if (!room?.participants) return '...';
    const participant = room.participants.find(p => p.id === authorId);
    return participant?.firstName ?? 'Utilisateur Inconnu';
  }

  handleError(e: any): void {
    console.error("Action failed:", e);
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

  expandSidebar(): void {
    this.isSidebarCollapsed.set(false);
  }

  toggleParticipants(): void {
    this.showParticipants.update(currentValue => !currentValue);
  }

  openCreateModal(): void { this.showCreateModal.set(true); }
  onRoomCreated(): void { this.showCreateModal.set(false); }
}