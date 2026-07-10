import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppointmentsStore } from '../../appointments.store';
import {
  ShopAppointmentDto, AppointmentStatus, ShopServiceType,
  STATUS_LABELS, SERVICE_LABELS, UpdateAppointmentRequest,
} from '../../boutique.models';
import { PageHeaderComponent } from '@shared/ui/page-header/page-header.component';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared/ui/empty-state/empty-state.component';
import { ModalComponent } from '@shared/ui/modal/modal.component';
import { BadgeComponent } from '@shared/ui/badge/badge.component';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';

const DAY_START = 8;   // 08:00
const DAY_END = 20;    // 20:00
const HOUR_PX = 46;

interface DayColumn { date: Date; label: string; iso: string; }

@Component({
  selector: 'app-planning',
  standalone: true,
  imports: [
    CommonModule, FormsModule, PageHeaderComponent, SpinnerComponent, EmptyStateComponent,
    ModalComponent, BadgeComponent, HasPermissionDirective,
  ],
  template: `
    <div class="page">
      <app-page-header title="Planning RDV" subtitle="Rendez-vous prestations : pré-grading, restauration, rachat">
        <div class="view-toggle">
          <button [class.on]="view() === 'month'" (click)="view.set('month')">Mois</button>
          <button [class.on]="view() === 'week'" (click)="view.set('week')">Semaine</button>
          <button [class.on]="view() === 'list'" (click)="view.set('list')">Liste</button>
        </div>
      </app-page-header>

      <!-- Filtres -->
      <div class="filters">
        <select class="input" [ngModel]="store.filters().status ?? ''" (ngModelChange)="onStatus($event)">
          <option value="">Tous statuts</option>
          @for (s of statusKeys; track s) { <option [value]="s">{{ statusLabels[s] }}</option> }
        </select>
        <select class="input" [ngModel]="store.filters().serviceType ?? ''" (ngModelChange)="onService($event)">
          <option value="">Toutes prestations</option>
          @for (s of serviceKeys; track s) { <option [value]="s">{{ serviceLabels[s] }}</option> }
        </select>
        @if (view() === 'week') {
          <div class="week-nav">
            <button (click)="shiftWeek(-1)">‹</button>
            <span>{{ week()[0].label }} — {{ week()[6].label }}</span>
            <button (click)="shiftWeek(1)">›</button>
          </div>
        }
      </div>

      @if (store.loading()) {
        <div class="center"><app-spinner size="lg" /></div>
      }

      <!-- ── Vue MOIS (Google Calendar style) ── -->
      @if (view() === 'month') {
        <div class="month-header">
          <button (click)="shiftMonth(-1)">‹</button>
          <span class="month-label">{{ monthLabel() }}</span>
          <button (click)="shiftMonth(1)">›</button>
        </div>
        <div class="cal-month">
          <div class="month-weekdays">
            <div class="weekday">Lun</div>
            <div class="weekday">Mar</div>
            <div class="weekday">Mer</div>
            <div class="weekday">Jeu</div>
            <div class="weekday">Ven</div>
            <div class="weekday">Sam</div>
            <div class="weekday">Dim</div>
          </div>
          <div class="month-days">
            @for (day of month(); track day.iso) {
              <div class="month-day" [class.other-month]="day.date.getMonth() !== monthStart().getMonth()">
                <div class="day-number">{{ day.label }}</div>
                <div class="day-events">
                  @for (a of appointmentsForDay(day); track a.id) {
                    <div class="event-mini"
                         [style.background-color]="serviceColor(a.serviceType)"
                         (click)="openDetail(a)"
                         [title]="serviceLabels[a.serviceType] + ' — ' + a.clientName">
                      {{ a.scheduledAt | date:'HH:mm' }}
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        </div>
      }

      <!-- ── Vue LISTE ── -->
      @if (view() === 'list') {
        @if (!store.loading() && store.items().length === 0) {
          <app-empty-state icon="📅" title="Aucun rendez-vous" description="Aucun RDV ne correspond aux filtres." />
        } @else {
          <table class="tbl">
            <thead><tr><th>Client</th><th>Prestation</th><th>Date / heure</th><th>Durée</th><th>Statut</th><th></th></tr></thead>
            <tbody>
              @for (a of store.items(); track a.id) {
                <tr>
                  <td>{{ a.clientName }}<br /><span class="muted">{{ a.clientEmail }}</span></td>
                  <td>{{ serviceLabels[a.serviceType] }}</td>
                  <td>{{ a.scheduledAt | date:'EEE d MMM, HH:mm' }}</td>
                  <td>{{ a.durationMinutes }} min</td>
                  <td><app-badge [variant]="statusVariant(a.status)">{{ statusLabels[a.status] }}</app-badge></td>
                  <td class="cell-actions"><button class="ic" (click)="openDetail(a)">Détails</button></td>
                </tr>
              }
            </tbody>
          </table>
        }
      }

      <!-- ── Vue SEMAINE (grille CSS) ── -->
      @if (view() === 'week') {
        <div class="cal">
          <div class="cal-head">
            <div class="cal-corner"></div>
            @for (d of week(); track d.iso) { <div class="cal-day">{{ d.label }}</div> }
          </div>
          <div class="cal-body">
            <div class="cal-hours">
              @for (h of hours; track h) { <div class="cal-hour" [style.height.px]="hourPx">{{ h }}:00</div> }
            </div>
            @for (d of week(); track d.iso) {
              <div class="cal-col" [style.height.px]="hourPx * hoursCount"
                   (dragover)="$event.preventDefault()" (drop)="onDrop(d, $event)">
                @for (a of appointmentsForDay(d); track a.id) {
                  <div class="evt" [class]="'evt--' + a.status.toLowerCase()"
                       [style.top.px]="topPx(a)" [style.height.px]="heightPx(a)"
                       draggable="true" (dragstart)="dragId = a.id" (click)="openDetail(a)">
                    <strong>{{ a.scheduledAt | date:'HH:mm' }}</strong> {{ a.clientName }}
                    <span class="evt-svc">{{ serviceLabels[a.serviceType] }}</span>
                  </div>
                }
              </div>
            }
          </div>
        </div>
        <p class="hint">Glissez un RDV vers un autre jour pour le déplacer (l’heure est conservée).</p>
      }
    </div>

    <!-- Modale détail / édition -->
    @if (detail(); as a) {
      <app-modal title="Rendez-vous" width="560px" (closed)="detail.set(null)">
        <div class="detail">
          <div class="drow"><span class="dk">Client</span><span>{{ a.clientName }} · {{ a.clientEmail }}</span></div>
          @if (a.clientPhone) { <div class="drow"><span class="dk">Téléphone</span><span>{{ a.clientPhone }}</span></div> }
          @if (a.notesClient) { <div class="drow"><span class="dk">Note client</span><span>{{ a.notesClient }}</span></div> }

          <div class="drow"><span class="dk">Statut</span>
            <div class="status-btns" *hasPermission="'shop:write'">
              @for (s of statusKeys; track s) {
                <button class="sbtn" [class.on]="a.status === s" (click)="setStatus(a, s)">{{ statusLabels[s] }}</button>
              }
            </div>
          </div>

          <fieldset class="edit" *hasPermission="'shop:write'">
            <legend>Modifier le créneau</legend>
            <div class="grid2">
              <div class="fg"><label class="label">Date & heure</label><input class="input" type="datetime-local" [(ngModel)]="editSlot" /></div>
              <div class="fg"><label class="label">Durée (min)</label><input class="input" type="number" min="15" step="15" [(ngModel)]="editDuration" /></div>
            </div>
            <div class="fg">
              <label class="label">Prestation</label>
              <select class="input" [(ngModel)]="editService">
                @for (s of serviceKeys; track s) { <option [value]="s">{{ serviceLabels[s] }}</option> }
              </select>
            </div>
            <div class="fg"><label class="label">Notes internes</label><textarea class="input" rows="2" [(ngModel)]="editNotes"></textarea></div>
            <div class="actions">
              <button class="btn-primary" [disabled]="store.saving()" (click)="saveSlot(a)">Enregistrer le créneau</button>
            </div>
          </fieldset>
        </div>
      </app-modal>
    }
  `,
  styles: [`
    .page { display: flex; flex-direction: column; gap: var(--space-5); }
    .center { display: flex; justify-content: center; padding: var(--space-10) 0; }
    .view-toggle, .week-nav { display: inline-flex; border: 1px solid var(--color-border-default); border-radius: var(--radius-md); overflow: hidden; }
    .view-toggle button, .week-nav button { border: none; background: var(--color-surface-base); padding: var(--space-2) var(--space-3); cursor: pointer; font-size: var(--font-size-sm); }
    .view-toggle button.on { background: var(--color-brand-primary); color: var(--color-brand-on-primary); }
    .week-nav { align-items: center; }
    .week-nav span { padding: 0 var(--space-3); font-size: var(--font-size-sm); }
    .filters { display: flex; gap: var(--space-3); align-items: center; flex-wrap: wrap; }
    .input { padding: var(--space-2) var(--space-3); border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      background: var(--color-surface-base); color: var(--color-text-primary); font-size: var(--font-size-sm); width: 100%; }
    .filters .input { max-width: 200px; }
    .muted { color: var(--color-text-muted); font-size: var(--font-size-xs); }
    .tbl { width: 100%; border-collapse: collapse; font-size: var(--font-size-sm); }
    .tbl th { text-align: left; padding: var(--space-2) var(--space-3); color: var(--color-text-muted); font-weight: var(--font-weight-medium); border-bottom: 1px solid var(--color-border-subtle); }
    .tbl td { padding: var(--space-2) var(--space-3); border-bottom: 1px solid var(--color-border-subtle); vertical-align: top; }
    .cell-actions { text-align: right; }
    .ic { border: 1px solid var(--color-border-default); background: transparent; border-radius: var(--radius-sm); padding: 4px 10px; cursor: pointer; font-size: var(--font-size-sm); }

    /* Calendrier */
    .cal { border: 1px solid var(--color-border-subtle); border-radius: var(--radius-md); overflow: hidden; }
    .cal-head { display: grid; grid-template-columns: 56px repeat(7, 1fr); background: var(--color-surface-raised); }
    .cal-corner { border-right: 1px solid var(--color-border-subtle); }
    .cal-day { text-align: center; padding: var(--space-2); font-size: var(--font-size-xs); font-weight: var(--font-weight-semibold);
      border-right: 1px solid var(--color-border-subtle); text-transform: capitalize; }
    .cal-body { display: grid; grid-template-columns: 56px repeat(7, 1fr); position: relative; }
    .cal-hours { display: flex; flex-direction: column; }
    .cal-hour { font-size: 10px; color: var(--color-text-muted); text-align: right; padding-right: 4px; border-top: 1px solid var(--color-border-subtle); box-sizing: border-box; }
    .cal-col { position: relative; border-left: 1px solid var(--color-border-subtle);
      background-image: repeating-linear-gradient(var(--color-border-subtle) 0 1px, transparent 1px 46px); }
    .evt { position: absolute; left: 2px; right: 2px; border-radius: var(--radius-sm); padding: 2px 4px;
      font-size: 10px; line-height: 1.2; overflow: hidden; cursor: grab; color: #fff; background: var(--color-brand-primary); }
    .evt strong { display: block; }
    .evt-svc { opacity: .85; }
    .evt--pending { background: var(--color-semantic-warning, #b8860b); }
    .evt--confirmed { background: var(--color-semantic-info, #2563eb); }
    .evt--inprogress { background: var(--color-brand-primary); }
    .evt--completed { background: var(--color-semantic-success, #16a34a); }
    .evt--cancelled { background: var(--color-neutral-400, #9ca3af); text-decoration: line-through; }
    .hint { font-size: var(--font-size-xs); color: var(--color-text-muted); }

    .detail { display: flex; flex-direction: column; gap: var(--space-3); }
    .drow { display: flex; gap: var(--space-3); font-size: var(--font-size-sm); }
    .dk { min-width: 90px; color: var(--color-text-muted); }
    .status-btns { display: flex; flex-wrap: wrap; gap: 4px; }
    .sbtn { border: 1px solid var(--color-border-default); background: transparent; border-radius: var(--radius-full);
      padding: 2px 10px; font-size: var(--font-size-xs); cursor: pointer; }
    .sbtn.on { background: var(--color-brand-primary); color: var(--color-brand-on-primary); border-color: var(--color-brand-primary); }
    .edit { border: 1px solid var(--color-border-subtle); border-radius: var(--radius-md); padding: var(--space-3); display: flex; flex-direction: column; gap: var(--space-3); }
    .edit legend { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); padding: 0 var(--space-2); }
    .grid2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--space-3); }
    .fg { display: flex; flex-direction: column; gap: var(--space-1); }
    .label { font-size: var(--font-size-xs); color: var(--color-text-secondary); }
    .actions { display: flex; justify-content: flex-end; }
    .btn-primary { height: 34px; padding: 0 var(--space-4); border: none; border-radius: var(--radius-md);
      background: var(--color-brand-primary); color: var(--color-brand-on-primary); font-size: var(--font-size-sm); cursor: pointer; }
    .btn-primary:disabled { opacity: .6; }

    /* Calendrier mensuel */
    .month-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: var(--space-4); }
    .month-header button { border: 1px solid var(--color-border-default); background: var(--color-surface-base); padding: var(--space-2) var(--space-3); cursor: pointer; border-radius: var(--radius-md); }
    .month-label { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); text-transform: capitalize; }
    .cal-month { border: 1px solid var(--color-border-subtle); border-radius: var(--radius-md); overflow: hidden; }
    .month-weekdays { display: grid; grid-template-columns: repeat(7, 1fr); background: var(--color-surface-raised); }
    .weekday { text-align: center; padding: var(--space-2); font-size: var(--font-size-xs); font-weight: var(--font-weight-semibold);
      text-transform: uppercase; color: var(--color-text-muted); border-right: 1px solid var(--color-border-subtle); }
    .weekday:last-child { border-right: none; }
    .month-days { display: grid; grid-template-columns: repeat(7, 1fr); min-height: 600px; }
    .month-day { border: 1px solid var(--color-border-subtle); padding: var(--space-2); min-height: 100px;
      display: flex; flex-direction: column; gap: var(--space-1); background: var(--color-surface-base); }
    .month-day.other-month { background: var(--color-surface-raised); opacity: 0.5; }
    .day-number { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); }
    .day-events { display: flex; flex-direction: column; gap: 2px; flex: 1; }
    .event-mini { padding: 2px 4px; border-radius: var(--radius-sm); font-size: 10px; color: #fff;
      cursor: pointer; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; font-weight: 500; }
    .event-mini:hover { opacity: 0.8; }
  `],
})
export class PlanningComponent implements OnInit {
  readonly store = inject(AppointmentsStore);

  readonly statusLabels = STATUS_LABELS;
  readonly serviceLabels = SERVICE_LABELS;
  readonly statusKeys = Object.keys(STATUS_LABELS) as AppointmentStatus[];
  readonly serviceKeys = Object.keys(SERVICE_LABELS) as ShopServiceType[];

  readonly view = signal<'month' | 'week' | 'list'>('month');
  readonly detail = signal<ShopAppointmentDto | null>(null);
  readonly weekStart = signal<Date>(this.mondayOf(new Date()));
  readonly monthStart = signal<Date>(this.firstDayOfMonth(new Date()));

  readonly hourPx = HOUR_PX;
  readonly hoursCount = DAY_END - DAY_START;
  readonly hours = Array.from({ length: DAY_END - DAY_START }, (_, i) => DAY_START + i);

  dragId: string | null = null;

  // Champs d'édition du créneau
  editSlot = '';
  editDuration = 30;
  editService: ShopServiceType = 'PreGrading';
  editNotes: string | null = null;

  readonly week = computed<DayColumn[]>(() => {
    const start = this.weekStart();
    return Array.from({ length: 7 }, (_, i) => {
      const d = new Date(start);
      d.setDate(start.getDate() + i);
      return {
        date: d,
        iso: d.toISOString().slice(0, 10),
        label: d.toLocaleDateString('fr-FR', { weekday: 'short', day: 'numeric', month: 'short' }),
      };
    });
  });

  readonly month = computed<DayColumn[]>(() => {
    const start = this.monthStart();
    const firstDay = new Date(start);
    const firstDayOfWeek = (firstDay.getDay() + 6) % 7; // 0 = lundi
    // Décaler au lundi précédent
    firstDay.setDate(start.getDate() - firstDayOfWeek);

    // Générer 42 jours (6 semaines * 7 jours)
    return Array.from({ length: 42 }, (_, i) => {
      const d = new Date(firstDay);
      d.setDate(firstDay.getDate() + i);
      return {
        date: d,
        iso: d.toISOString().slice(0, 10),
        label: d.toLocaleDateString('fr-FR', { day: 'numeric' }),
      };
    });
  });

  readonly monthLabel = computed<string>(() => {
    const start = this.monthStart();
    return start.toLocaleDateString('fr-FR', { month: 'long', year: 'numeric' });
  });

  ngOnInit(): void { this.store.load(); }

  onStatus(v: string): void { this.store.setFilters({ status: (v || null) as AppointmentStatus | null }); }
  onService(v: string): void { this.store.setFilters({ serviceType: (v || null) as ShopServiceType | null }); }

  shiftWeek(delta: number): void {
    const d = new Date(this.weekStart());
    d.setDate(d.getDate() + delta * 7);
    this.weekStart.set(d);
  }

  shiftMonth(delta: number): void {
    const d = new Date(this.monthStart());
    d.setMonth(d.getMonth() + delta);
    this.monthStart.set(d);
  }

  appointmentsForDay(d: DayColumn): ShopAppointmentDto[] {
    return this.store.items().filter(a => new Date(a.scheduledAt).toDateString() === d.date.toDateString());
  }

  topPx(a: ShopAppointmentDto): number {
    const dt = new Date(a.scheduledAt);
    const mins = (dt.getHours() - DAY_START) * 60 + dt.getMinutes();
    return Math.max(0, (mins / 60) * HOUR_PX);
  }
  heightPx(a: ShopAppointmentDto): number {
    return Math.max(18, (a.durationMinutes / 60) * HOUR_PX);
  }

  statusVariant(s: AppointmentStatus): 'success' | 'danger' | 'warning' | 'info' | 'neutral' {
    switch (s) {
      case 'Completed': return 'success';
      case 'Cancelled': return 'danger';
      case 'Pending': return 'warning';
      case 'Confirmed': return 'info';
      default: return 'neutral';
    }
  }

  serviceColor(serviceType: ShopServiceType): string {
    switch (serviceType) {
      case 'PreGrading': return '#8B4513';    // marron — pré-gradation
      case 'Restoration': return '#D2691E';   // chocolat — restauration
      case 'Purchase': return '#4CAF50';      // vert — rachat
      default: return '#999999';              // gris — autre
    }
  }

  onDrop(d: DayColumn, ev: DragEvent): void {
    ev.preventDefault();
    const id = this.dragId;
    this.dragId = null;
    if (!id) return;
    const appt = this.store.items().find(a => a.id === id);
    if (!appt) return;
    const orig = new Date(appt.scheduledAt);
    const moved = new Date(d.date);
    moved.setHours(orig.getHours(), orig.getMinutes(), 0, 0);
    const body: UpdateAppointmentRequest = {
      scheduledAt: moved.toISOString(),
      durationMinutes: appt.durationMinutes,
      serviceType: appt.serviceType,
      notesInternal: appt.notesInternal,
      assignedToUserId: appt.assignedToUserId,
    };
    this.store.updateSlot({ id, body });
  }

  openDetail(a: ShopAppointmentDto): void {
    this.detail.set(a);
    this.editSlot = this.toLocalInput(a.scheduledAt);
    this.editDuration = a.durationMinutes;
    this.editService = a.serviceType;
    this.editNotes = a.notesInternal;
  }

  setStatus(a: ShopAppointmentDto, s: AppointmentStatus): void {
    this.store.changeStatus({ id: a.id, status: s });
    this.detail.set({ ...a, status: s });
  }

  saveSlot(a: ShopAppointmentDto): void {
    const body: UpdateAppointmentRequest = {
      scheduledAt: new Date(this.editSlot).toISOString(),
      durationMinutes: this.editDuration,
      serviceType: this.editService,
      notesInternal: this.editNotes,
      assignedToUserId: a.assignedToUserId,
    };
    this.store.updateSlot({ id: a.id, body, onSuccess: () => this.detail.set(null) });
  }

  private mondayOf(date: Date): Date {
    const d = new Date(date);
    const day = (d.getDay() + 6) % 7; // 0 = lundi
    d.setDate(d.getDate() - day);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  private firstDayOfMonth(date: Date): Date {
    const d = new Date(date);
    d.setDate(1);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  private toLocalInput(iso: string): string {
    const d = new Date(iso);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }
}
