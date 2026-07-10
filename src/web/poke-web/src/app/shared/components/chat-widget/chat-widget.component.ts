import {
  Component, signal, inject, ChangeDetectionStrategy,
  ElementRef, ViewChild, AfterViewChecked
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
}

interface ChatResponse {
  sessionId: string;
  message: string;
  pendingAction: { actionId: string; summary: string } | null;
  directive: { type: string; url: string } | null;
}

@Component({
  selector: 'app-chat-widget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Bouton flottant -->
    <button class="chat-toggle" (click)="toggleOpen()" [class.active]="isOpen()" aria-label="Pik-IA-chu">
      @if (isOpen()) {
        <span class="chat-toggle__close">✕</span>
      } @else {
        <svg viewBox="0 0 64 64" width="34" height="34" aria-hidden="true">
        <!-- oreilles -->
        <polygon points="21,24 9,1 31,19" fill="#FFCB05" stroke="#8a6d1b" stroke-width="1"/>
        <polygon points="43,24 55,1 33,19" fill="#FFCB05" stroke="#8a6d1b" stroke-width="1"/>
        <polygon points="13,9 9,1 18,11" fill="#2b2b2b"/>
        <polygon points="51,9 55,1 46,11" fill="#2b2b2b"/>
        <!-- visage -->
        <circle cx="32" cy="39" r="20" fill="#FFCB05" stroke="#8a6d1b" stroke-width="1.2"/>
        <!-- joues -->
        <circle cx="17" cy="45" r="5" fill="#e23b2e"/>
        <circle cx="47" cy="45" r="5" fill="#e23b2e"/>
        <!-- yeux -->
        <circle cx="24" cy="37" r="3.4" fill="#2b2b2b"/>
        <circle cx="40" cy="37" r="3.4" fill="#2b2b2b"/>
        <circle cx="25.2" cy="35.8" r="1.1" fill="#fff"/>
        <circle cx="41.2" cy="35.8" r="1.1" fill="#fff"/>
        <!-- nez + bouche -->
        <ellipse cx="32" cy="41" rx="1.5" ry="1" fill="#2b2b2b"/>
        <path d="M27 45 Q32 50 37 45" fill="none" stroke="#2b2b2b" stroke-width="1.4" stroke-linecap="round"/>
      </svg>
      }
    </button>

    <!-- Panneau -->
    @if (isOpen()) {
      <div class="chat-panel">
        <div class="chat-header">
          <span class="chat-header__brand">
            <svg viewBox="0 0 64 64" width="22" height="22" aria-hidden="true">
        <!-- oreilles -->
        <polygon points="21,24 9,1 31,19" fill="#FFCB05" stroke="#8a6d1b" stroke-width="1"/>
        <polygon points="43,24 55,1 33,19" fill="#FFCB05" stroke="#8a6d1b" stroke-width="1"/>
        <polygon points="13,9 9,1 18,11" fill="#2b2b2b"/>
        <polygon points="51,9 55,1 46,11" fill="#2b2b2b"/>
        <!-- visage -->
        <circle cx="32" cy="39" r="20" fill="#FFCB05" stroke="#8a6d1b" stroke-width="1.2"/>
        <!-- joues -->
        <circle cx="17" cy="45" r="5" fill="#e23b2e"/>
        <circle cx="47" cy="45" r="5" fill="#e23b2e"/>
        <!-- yeux -->
        <circle cx="24" cy="37" r="3.4" fill="#2b2b2b"/>
        <circle cx="40" cy="37" r="3.4" fill="#2b2b2b"/>
        <circle cx="25.2" cy="35.8" r="1.1" fill="#fff"/>
        <circle cx="41.2" cy="35.8" r="1.1" fill="#fff"/>
        <!-- nez + bouche -->
        <ellipse cx="32" cy="41" rx="1.5" ry="1" fill="#2b2b2b"/>
        <path d="M27 45 Q32 50 37 45" fill="none" stroke="#2b2b2b" stroke-width="1.4" stroke-linecap="round"/>
      </svg>
            <span>Pik-IA-chu</span>
          </span>
          @if (isLoading()) { <span class="spinner">⏳</span> }
        </div>

        <div class="chat-messages" #messagesEl>
          @for (msg of messages(); track $index) {
            <div class="msg" [class.msg--user]="msg.role === 'user'" [class.msg--assistant]="msg.role === 'assistant'">
              <div class="msg__content">{{ msg.content }}</div>
            </div>
          }
        </div>

        <!-- Boutons confirmation -->
        @if (pendingAction()) {
          <div class="chat-confirm">
            <button class="btn-confirm" (click)="confirm()">✅ Confirmer</button>
            <button class="btn-cancel" (click)="cancel()">❌ Annuler</button>
          </div>
        }

        <div class="chat-input">
          <input
            #inputEl
            type="text"
            [(ngModel)]="inputText"
            placeholder="Posez une question ou donnez un ordre…"
            (keydown.enter)="send()"
            [disabled]="isLoading()"
          />
          <button (click)="send()" [disabled]="isLoading() || !inputText.trim()">➤</button>
        </div>
      </div>
    }
  `,
  styles: [`
    .chat-toggle {
      position: fixed; bottom: 24px; right: 24px; z-index: 9999;
      width: 54px; height: 54px; border-radius: 50%; border: 2px solid #8a6d1b;
      background: #FFCB05; color: #2b2b2b; cursor: pointer;
      display: flex; align-items: center; justify-content: center;
      box-shadow: 0 4px 12px rgba(0,0,0,.25); transition: transform .15s, box-shadow .2s;
    }
    .chat-toggle:hover { transform: scale(1.06); box-shadow: 0 6px 18px rgba(0,0,0,.3); }
    .chat-toggle.active { background: #f6b900; }
    .chat-toggle__close { font-size: 22px; font-weight: 700; line-height: 1; }
    .chat-header__brand { display: inline-flex; align-items: center; gap: 8px; }

    .chat-panel {
      position: fixed; bottom: 88px; right: 24px; z-index: 9998;
      width: 380px; max-height: 560px; display: flex; flex-direction: column;
      background: #fff; border-radius: 12px; box-shadow: 0 8px 32px rgba(0,0,0,.2);
      overflow: hidden;
    }
    .chat-header {
      background: #3b82f6; color: #fff; padding: 12px 16px;
      font-weight: 600; display: flex; justify-content: space-between;
    }
    .chat-messages {
      flex: 1; overflow-y: auto; padding: 12px; display: flex;
      flex-direction: column; gap: 8px; background: #f8fafc;
    }
    .msg { display: flex; }
    .msg--user { justify-content: flex-end; }
    .msg--assistant { justify-content: flex-start; }
    .msg__content {
      max-width: 80%; padding: 8px 12px; border-radius: 12px;
      font-size: 14px; line-height: 1.4; white-space: pre-wrap;
    }
    .msg--user .msg__content    { background: #3b82f6; color: #fff; border-bottom-right-radius: 2px; }
    .msg--assistant .msg__content { background: #e2e8f0; color: #1e293b; border-bottom-left-radius: 2px; }

    .chat-confirm {
      padding: 8px 12px; display: flex; gap: 8px; background: #fefce8; border-top: 1px solid #fde68a;
    }
    .btn-confirm, .btn-cancel {
      flex: 1; padding: 6px; border: none; border-radius: 6px; cursor: pointer; font-size: 13px;
    }
    .btn-confirm { background: #22c55e; color: #fff; }
    .btn-cancel  { background: #ef4444; color: #fff; }

    .chat-input {
      display: flex; padding: 10px; border-top: 1px solid #e2e8f0; background: #fff; gap: 8px;
    }
    .chat-input input {
      flex: 1; padding: 8px 12px; border: 1px solid #e2e8f0; border-radius: 8px;
      font-size: 14px; outline: none;
    }
    .chat-input input:focus { border-color: #3b82f6; }
    .chat-input button {
      padding: 8px 14px; background: #3b82f6; color: #fff; border: none;
      border-radius: 8px; cursor: pointer; font-size: 16px;
    }
    .chat-input button:disabled { opacity: .5; cursor: default; }
  `]
})
export class ChatWidgetComponent implements AfterViewChecked {
  @ViewChild('messagesEl') private messagesEl?: ElementRef<HTMLDivElement>;

  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly isOpen = signal(false);
  readonly messages = signal<ChatMessage[]>([]);
  readonly isLoading = signal(false);
  readonly pendingAction = signal<{ actionId: string; summary: string } | null>(null);

  inputText = '';
  private sessionId: string | null = null;

  toggleOpen(): void {
    this.isOpen.update(v => !v);
    if (this.isOpen() && this.messages().length === 0) {
      this.addAssistantMessage('⚡ Salut, moi c\'est Pik-IA-chu ! Je peux t\'aider sur le stock, le présentoir, le planning ou les prix marché. Que veux-tu faire ?');
    }
  }

  send(): void {
    const text = this.inputText.trim();
    if (!text || this.isLoading()) return;
    this.inputText = '';
    this.addUserMessage(text);
    this.callApi(text);
  }

  confirm(): void { this.callApi('oui'); }
  cancel(): void { this.callApi('non'); }

  private callApi(message: string): void {
    this.isLoading.set(true);
    this.pendingAction.set(null);

    this.http.post<ChatResponse>('/api/v1/chat', {
      sessionId: this.sessionId,
      message
    }).subscribe({
      next: (res) => {
        this.sessionId = res.sessionId;
        this.addAssistantMessage(res.message);
        this.pendingAction.set(res.pendingAction);
        this.isLoading.set(false);

        if (res.directive?.type === 'redirect_camera') {
          this.isOpen.set(false);
          this.router.navigateByUrl(res.directive.url);
        }
      },
      error: () => {
        this.addAssistantMessage('❌ Erreur de connexion. Réessayez.');
        this.isLoading.set(false);
      }
    });
  }

  private addUserMessage(content: string): void {
    this.messages.update(msgs => [...msgs, { role: 'user', content, timestamp: new Date() }]);
  }

  private addAssistantMessage(content: string): void {
    this.messages.update(msgs => [...msgs, { role: 'assistant', content, timestamp: new Date() }]);
  }

  ngAfterViewChecked(): void {
    if (this.messagesEl) {
      const el = this.messagesEl.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }
}
