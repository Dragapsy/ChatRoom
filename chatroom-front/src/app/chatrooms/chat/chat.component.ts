import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgIconComponent, provideIcons } from '@ng-icons/core';
import { saxAddSquareBulk, saxMenuBulk, saxPeopleBulk  } from '@ng-icons/iconsax/bulk';

import { MessagingService } from 'src/app/_common/services/messaging/messaging.service';
import { ChatRoom } from 'src/app/_common/models/chat-room.model';
import { CreateComponent as ChatroomsCreateComponent } from '../create/create.component';
import { Subscription } from 'rxjs';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { tablerSend } from '@ng-icons/tabler-icons';


@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, ChatroomsCreateComponent, NgIconComponent, ReactiveFormsModule ],
  providers: [provideIcons({ saxAddSquareBulk, saxMenuBulk, saxPeopleBulk, tablerSend  }) ],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit, OnDestroy  {
  private readonly messagingService = inject(MessagingService);

  public allRooms = this.messagingService.chatRooms;
  public joinedRooms = this.messagingService.joinedRooms;
  public messages = this.messagingService.messages;
  private readonly fb = inject(FormBuilder);
  public selectedRoom = signal<ChatRoom | null>(null);

  public showCreateModal = signal(false);
  public roomError = signal<string | null>(null);
  public activeView = signal<'joined' | 'all'>('joined');

  public isSidebarCollapsed = signal(false);

  public showParticipants = signal(false);

  public activityNotification = signal<string | null>(null);
  private activitySubscription: Subscription | undefined;
  private activityTimeout: any;

  public messageForm: FormGroup;

  constructor() { 
    this.messageForm = this.fb.group({
      content: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.messagingService.loadInitialChatRooms();
    this.activitySubscription = this.messagingService.userActivity$.subscribe(message => {
        this.activityNotification.set(message);
        clearTimeout(this.activityTimeout);
        this.activityTimeout = setTimeout(() => {
            this.activityNotification.set(null);
        }, 3000);
    });
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

  ngOnDestroy(): void {
      if (this.activitySubscription) {
          this.activitySubscription.unsubscribe();
      }
      clearTimeout(this.activityTimeout);
  }

  async sendMessage(): Promise<void> {
    if (this.messageForm.invalid) return;

    const selectedRoom = this.selectedRoom();
    if (!selectedRoom) return;

    const content = this.messageForm.value.content.trim();
    if (!content) return;
    
    try {
      await this.messagingService.sendMessage(selectedRoom.id, content);
      this.messageForm.reset();
    } catch (e) {
      console.error("Failed to send message:", e);
      this.roomError.set("L'envoi du message a échoué.");
    }
  }
}