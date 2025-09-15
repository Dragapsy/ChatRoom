import { Component } from '@angular/core';
import { ChatComponent } from '../chat/chat.component';

@Component({
  selector: 'app-chat-page',
  standalone: true,
  imports: [ChatComponent], 
  templateUrl: './chat-page.component.html',
  styleUrls: ['./chat-page.component.scss']
})
export class ChatPageComponent { }