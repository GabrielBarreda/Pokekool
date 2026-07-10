import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe, SlicePipe } from '@angular/common';
import { StorageService } from '../storage.service';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { ToastService } from '@shared/services/toast.service';
import { StorageMovementEventDto } from '../storage.models';

/**
 * Widget d'alertes des déplacements de cartons détectés, en attente de confirmation humaine.
 * La détection caméra n'est pas dans ce prompt — seul l'écran de confirmation est fourni.
 */
@Component({
  selector: 'app-storage-movement-alerts',
  standalone: true,
  imports: [DatePipe, SlicePipe, HasPermissionDirective],
  template: `
    <section class="panel">
      <header class="panel__head">
        <h3>Déplacements à valider</h3>
        @if (events().length > 0) { <span class="count">{{ events().length }}</span> }
      </header>

      @if (events().length === 0) {
        <p class="empty">Aucun déplacement en attente. ✓</p>
      } @else {
        <ul class="list">
          @for (e of events(); track e.id) {
            <li>
              <div class="info">
                <span class="dot"></span>
                <span>Carton {{ e.boxId ? (e.boxId | slice:0:8) : 'inconnu' }} déplacé</span>
                <time>{{ e.detectedAt | date:'dd/MM HH:mm' }}</time>
              </div>
              <div *hasPermission="'storage:write'" class="actions">
                <button class="btn-ok" (click)="resolve(e, true)" [disabled]="busy() === e.id">Confirmer</button>
                <button class="btn-ko" (click)="resolve(e, false)" [disabled]="busy() === e.id">Rejeter</button>
              </div>
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: [`
    .panel { border: 1px solid #e2e2e2; border-radius: 12px; padding: 1rem; display: flex; flex-direction: column; gap: .75rem; }
    .panel__head { display: flex; align-items: center; gap: .5rem; }
    .count { background: #f8d7da; color: #721c24; border-radius: 999px; padding: .1rem .5rem; font-size: .75rem; }
    .empty { color: #999; font-size: .85rem; }
    .list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: .4rem; }
    .list li { display: flex; justify-content: space-between; align-items: center; gap: 1rem; padding: .5rem; background: #fffdf6; border: 1px solid #f0e6c8; border-radius: 8px; font-size: .85rem; }
    .info { display: flex; align-items: center; gap: .5rem; }
    .dot { width: .6rem; height: .6rem; border-radius: 50%; background: #e0a800; }
    time { color: #999; font-size: .75rem; }
    .actions { display: flex; gap: .4rem; }
    .btn-ok, .btn-ko { border: none; border-radius: 6px; padding: .3rem .6rem; cursor: pointer; font-size: .8rem; }
    .btn-ok { background: #d4edda; color: #155724; }
    .btn-ko { background: #f8d7da; color: #721c24; }
  `],
})
export class StorageMovementAlertsComponent implements OnInit {
  private readonly service = inject(StorageService);
  private readonly toast = inject(ToastService);

  readonly events = signal<StorageMovementEventDto[]>([]);
  readonly busy = signal<string | null>(null);

  ngOnInit(): void { this.reload(); }

  private reload(): void {
    this.service.getPendingMovements().subscribe({
      next: e => this.events.set(e),
      error: () => this.toast.error('Impossible de charger les déplacements.'),
    });
  }

  resolve(evt: StorageMovementEventDto, confirm: boolean): void {
    this.busy.set(evt.id);
    this.service.confirmMovement(evt.id, confirm).subscribe({
      next: () => {
        this.busy.set(null);
        this.events.update(list => list.filter(x => x.id !== evt.id));
        this.toast.success(confirm ? 'Déplacement confirmé.' : 'Déplacement rejeté.');
      },
      error: () => { this.busy.set(null); this.toast.error('Action impossible.'); },
    });
  }
}
