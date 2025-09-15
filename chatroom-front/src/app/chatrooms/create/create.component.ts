import { Component, EventEmitter, inject, Output, signal } from '@angular/core';
import { ReactiveFormsModule, FormControl, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ChatRoom } from 'src/app/_common/models/chat-room.model';
import { MessagingService } from 'src/app/_common/services/messaging/messaging.service';

@Component({
  selector: 'app-chatrooms-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './create.component.html',
  styleUrls: ['./create.component.scss']
})
export class CreateComponent {
  private messaging = inject(MessagingService);
  @Output() created = new EventEmitter<ChatRoom>(); 

  name = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(2)] });
  loading = signal(false);
  error   = signal<string | null>(null);
  room    = signal<ChatRoom | null>(null);

  // Dans create.component.ts

async submit() {
    if (this.name.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    this.room.set(null);

    try {
      const created = await this.messaging.createChatRoom(this.name.value);
      
      this.room.set(created);
      this.name.setValue(''); 

      setTimeout(() => {
        this.created.emit(created); 
      }, 2000); 

    } catch (e: any) {
      this.error.set(e?.message ?? 'Création impossible.');
      this.loading.set(false); 
    } 
}
}
