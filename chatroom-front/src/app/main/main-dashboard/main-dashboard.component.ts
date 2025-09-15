import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { NgIconComponent, provideIcons, provideNgIconsConfig } from '@ng-icons/core';
import { saxCardsBulk, saxBuildingsBulk, saxMessageTextBulk } from '@ng-icons/iconsax/bulk';
import { bootstrapArrowDown, bootstrapArrowUp } from '@ng-icons/bootstrap-icons';
import { User } from 'src/app/_common/models/user.model';
import { AccountService } from 'src/app/_common/services/account/account.service';
import { ChatButtonGroupComponent } from '../../_common/components/chat-button-group/chat-button-group.component';
import { MHPButton } from 'src/app/_common/components/chat-button-group/chat-button.interface';
import { ChatButtonComponent } from '../../_common/components/chat-button/chat-button.component';
import { SITEMAP } from 'src/app/_common/sitemap';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ChatSvgIconComponent } from '../../_common/components/chat-svg-icon/chat-svg-icon.component';
import { MessagingService } from 'src/app/_common/services/messaging/messaging.service';
import { ChatComponent } from 'src/app/chatrooms/chat/chat.component';
import { ChatPageComponent } from 'src/app/chatrooms/chat-page/chat-page.component';
import { saxCloseCircleOutline, saxMaximize3Outline } from '@ng-icons/iconsax/outline';


@Component({
	selector: 'app-main-dashboard',
	standalone: true,
	imports: [
		CommonModule,
		RouterModule,
		NgIconComponent,
		ChatButtonGroupComponent,
		ChatButtonComponent,
		ChatSvgIconComponent,
		ChatComponent,
		ChatPageComponent
	],
	providers: [
		provideIcons({
			bootstrapArrowUp,
			bootstrapArrowDown,
			saxCardsBulk,
			saxBuildingsBulk,
			saxMessageTextBulk,
			saxMaximize3Outline,
			saxCloseCircleOutline 		
	}),
		provideNgIconsConfig({ size: '1.5rem' }),
	],
	styleUrl: './main-dashboard.component.scss',
	templateUrl: './main-dashboard.component.html',
})
export class MainDashboardComponent implements OnInit {
	private readonly _accountSvc = inject(AccountService);
	private readonly messagingService = inject(MessagingService); 

	public readonly sitemap = SITEMAP;

	public readonly user = computed<User | null>(this._accountSvc.user);

	isChatWidgetOpen = signal(false);

	public readonly buttons: MHPButton<number>[] = [
		{ text: 'Requests', value: 1 },
	];

	public viewSelected: number = 1;

	constructor() {}
	ngOnInit(): void {
		this.messagingService.loadInitialChatRooms();
	}

	toggleChatWidget(): void {
    this.isChatWidgetOpen.set(!this.isChatWidgetOpen());
  }
}
