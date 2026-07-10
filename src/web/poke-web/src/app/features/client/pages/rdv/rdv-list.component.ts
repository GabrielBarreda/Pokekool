import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClientService } from '../../client.service';
import { ShopAppointmentDto } from '../../client.models';
import { ToastService } from '@shared/services/toast.service';

@Component({
  selector: 'app-rdv-list',
  standalone: true,
  imports: [RouterLink],
  template: `
    <header class="page-head">
      <div>
        <h1 class="page-head__title">Mes réservations</h1>
        <p class="page-head__sub">Historique de vos rendez-vous en boutique.</p>
      </div>
      <a routerLink="/client/rdv/new" class="btn btn--primary">Prendre un RDV</a>
    </header>

    @if (loading()) {
      <p class="muted">Chargement…</p>
    } @else if (appointments().length === 0) {
      <p class="muted">Vous n'avez aucune réservation.</p>
    } @else {
      <ul class="list">
        @for (a of appointments(); track a.id) {
          <li class="item">
            <div class="item__main">
              <span class="item__service">{{ serviceLabel(a.serviceType) }}</span>
              <span class="item__date">{{ formatDate(a.scheduledAt) }}</span>
              @if (a.notesClient) { <span class="item__notes">{{ a.notesClient }}</span> }
            </div>
            <div class="item__side">
              <span class="badge" [class]="'badge--' + statusClass(a.status)">{{ statusLabel(a.status) }}</span>
              @if (canCancel(a)) {
                <button type="button" class="btn btn--danger" [disabled]="cancelling() === a.id" (click)="cancel(a)">Annuler</button>
              }
            </div>
          </li>
        }
      </ul>
    }
  `,
  styles: [`
    .page-head { display: flex; align-items: flex-start; justify-content: space-between; gap: var(--space-4); margin-bottom: var(--space-5); }
    .page-head__title { font-size: var(--font-size-2xl); font-weight: var(--font-weight-bold); margin: 0; }
    .page-head__sub { color: var(--color-text-muted); margin: var(--space-1) 0 0; }

    .list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: var(--space-3); }
    .item { display: flex; align-items: center; justify-content: space-between; gap: var(--space-4); background: var(--color-surface-base); border: 1px solid var(--color-border-subtle); border-radius: var(--radius-lg); padding: var(--space-4); }
    .item__main { display: flex; flex-direction: column; gap: var(--space-1); }
    .item__service { font-weight: var(--font-weight-semibold); }
    .item__date { font-size: var(--font-size-sm); color: var(--color-text-secondary); }
    .item__notes { font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .item__side { display: flex; align-items: center; gap: var(--space-3); }

    .badge { font-size: var(--font-size-xs); font-weight: var(--font-weight-semibold); padding: 2px var(--space-2); border-radius: 999px; }
    .badge--pending { background: var(--color-semantic-warning-subtle, #fef3c7); color: var(--color-semantic-warning, #b45309); }
    .badge--confirmed { background: var(--color-semantic-info-subtle, #dbeafe); color: var(--color-semantic-info, #1d4ed8); }
    .badge--done { background: var(--color-semantic-success-subtle, #dcfce7); color: var(--color-semantic-success, #15803d); }
    .badge--cancelled { background: var(--color-surface-sunken); color: var(--color-text-muted); }

    .btn { padding: var(--space-2) var(--space-4); border: 1px solid var(--color-border-default); border-radius: var(--radius-md); background: var(--color-surface-base); cursor: pointer; font-size: var(--font-size-sm); text-decoration: none; color: var(--color-text-primary); }
    .btn:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn--primary { background: var(--color-brand-primary); color: #fff; border-color: var(--color-brand-primary); }
    .btn--danger { border-color: var(--color-semantic-danger); color: var(--color-semantic-danger); }
    .muted { color: var(--color-text-muted); }
  `],
})
export class RdvListComponent implements OnInit {
  private readonly service = inject(ClientService);
  private readonly toast = inject(ToastService);

  readonly appointments = signal<ShopAppointmentDto[]>([]);
  readonly loading = signal(false);
  readonly cancelling = signal<string | null>(null);

  ngOnInit(): void { this.load(); }

  private load(): void {
    this.loading.set(true);
    this.service.getMyAppointments(1, 50).subscribe({
      next: res => { this.appointments.set(res.items); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleString('fr-FR', { dateStyle: 'full', timeStyle: 'short' });
  }
  serviceLabel(t: string): string {
    return ({ PreGrading: 'Pré-gradation', Restoration: 'Restauration', Purchase: 'Rachat', Other: 'Autre' } as Record<string, string>)[t] ?? t;
  }
  statusLabel(s: string): string {
    return ({ Pending: 'En attente', Confirmed: 'Confirmé', InProgress: 'En cours', Completed: 'Terminé', Cancelled: 'Annulé' } as Record<string, string>)[s] ?? s;
  }
  statusClass(s: string): string {
    switch (s) {
      case 'Pending': return 'pending';
      case 'Confirmed': case 'InProgress': return 'confirmed';
      case 'Completed': return 'done';
      default: return 'cancelled';
    }
  }

  canCancel(a: ShopAppointmentDto): boolean {
    if (a.status !== 'Pending') return false;
    const in24h = Date.now() + 24 * 3600 * 1000;
    return new Date(a.scheduledAt).getTime() > in24h;
  }

  cancel(a: ShopAppointmentDto): void {
    this.cancelling.set(a.id);
    this.service.cancelAppointment(a.id).subscribe({
      next: () => {
        this.cancelling.set(null);
        this.appointments.update(list => list.map(x => x.id === a.id ? { ...x, status: 'Cancelled' } : x));
        this.toast.show({ message: 'Rendez-vous annulé.', type: 'success' });
      },
      error: () => {
        this.cancelling.set(null);
        this.toast.show({ message: 'Annulation impossible.', type: 'danger' });
      },
    });
  }
}
