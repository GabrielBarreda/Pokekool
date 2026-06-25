import {
  Component, input, output, inject, OnInit, ChangeDetectionStrategy, signal, computed, effect,
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RoleDto, CreateRoleRequest, UpdateRoleRequest } from '../roles.models';
import { RolesStore } from '../roles.store';
import { SpinnerComponent } from '@shared/ui/spinner/spinner.component';

@Component({
  selector: 'app-role-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, SpinnerComponent],
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()" class="form">

      <!-- Notice rôle système -->
      @if (isSystemRole()) {
        <div class="system-notice">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
          </svg>
          Rôle système — les permissions sont gérées par la plateforme et ne peuvent pas être modifiées.
        </div>
      }

      <div class="form-group">
        <label class="label" for="rf-name">Nom du rôle <span class="req">*</span></label>
        <input id="rf-name" class="input" [class.input--error]="touched('name')"
               formControlName="name" placeholder="Ex : Chef de salle" />
        @if (touched('name')) { <span class="err">Le nom est obligatoire</span> }
      </div>

      @if (mode() === 'create') {
        <div class="form-group">
          <label class="label" for="rf-code">Code <span class="req">*</span></label>
          <input id="rf-code" class="input" [class.input--error]="touched('code')"
                 formControlName="code" placeholder="chef_de_salle" />
          @if (touched('code')) { <span class="err">Code obligatoire (lettres minuscules, chiffres, underscore)</span> }
          <span class="hint">Identifiant technique, non modifiable après création.</span>
        </div>
      }

      <div class="form-group">
        <label class="label" for="rf-desc">Description</label>
        <textarea id="rf-desc" class="input textarea" formControlName="description" rows="2"
                  placeholder="Décrivez les responsabilités de ce rôle…"></textarea>
      </div>

      <!-- Matrice permissions -->
      <div class="perm-section">
        <div class="perm-header">
          <span class="perm-title">Permissions</span>
          @if (!isSystemRole()) {
            <div class="perm-controls">
              <button type="button" class="text-btn" (click)="selectAll()">Tout cocher</button>
              <span class="sep">·</span>
              <button type="button" class="text-btn" (click)="clearAll()">Tout décocher</button>
            </div>
          }
        </div>

        @for (group of rolesStore.permissionGroups(); track group.category) {
          <div class="perm-group">
            <button type="button" class="perm-group-header" (click)="toggleCat(group.category)">
              <span class="perm-cat-label">{{ group.label }}</span>
              <span class="perm-cat-count">
                {{ selectedInCat(group.category) }}/{{ group.permissions.length }}
              </span>
              <svg class="chevron" [class.chevron--open]="isCatOpen(group.category)"
                   width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <polyline points="6 9 12 15 18 9"/>
              </svg>
            </button>

            @if (isCatOpen(group.category)) {
              <div class="perm-rows">
                @for (perm of group.permissions; track perm.code) {
                  <label class="perm-row" [class.perm-row--disabled]="isSystemRole()">
                    <input type="checkbox" class="chk"
                           [checked]="selected().has(perm.code)"
                           [disabled]="isSystemRole()"
                           (change)="togglePerm(perm.code, $event)" />
                    <div class="perm-info">
                      <code class="perm-code">{{ perm.code }}</code>
                      <span class="perm-desc">{{ perm.description }}</span>
                    </div>
                  </label>
                }
              </div>
            }
          </div>
        }
      </div>

      <div class="actions">
        <button type="button" class="btn btn--ghost" (click)="cancelled.emit()">
          {{ isSystemRole() ? 'Fermer' : 'Annuler' }}
        </button>
        @if (!isSystemRole()) {
          <button type="submit" class="btn btn--primary" [disabled]="saving()">
            @if (saving()) { <app-spinner size="sm" /> }
            {{ mode() === 'create' ? 'Créer le rôle' : 'Enregistrer' }}
          </button>
        }
      </div>
    </form>
  `,
  styles: [`
    .form { display: flex; flex-direction: column; gap: var(--space-5); }
    .form-group { display: flex; flex-direction: column; gap: var(--space-1); }
    .label { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--color-text-secondary); }
    .req { color: var(--color-semantic-danger); }
    .hint { font-size: var(--font-size-xs); color: var(--color-text-muted); margin-top: 2px; }
    .err  { font-size: var(--font-size-xs); color: var(--color-semantic-danger); }

    .input {
      height: 38px; padding: 0 var(--space-3);
      border: 1px solid var(--color-border-default); border-radius: var(--radius-md);
      background: var(--color-surface-base); color: var(--color-text-primary);
      font-size: var(--font-size-sm); outline: none;
      transition: border-color var(--duration-fast);
      &:focus { border-color: var(--color-brand-primary); box-shadow: 0 0 0 3px var(--color-brand-primary-subtle); }
      &--error { border-color: var(--color-semantic-danger); }
    }
    .textarea { height: auto; padding: var(--space-2) var(--space-3); resize: vertical; min-height: 60px; }

    /* Permission matrix */
    .perm-section { display: flex; flex-direction: column; gap: var(--space-2); }
    .perm-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: var(--space-1); }
    .perm-title { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
    .perm-controls { display: flex; align-items: center; gap: var(--space-2); }
    .text-btn {
      font-size: var(--font-size-xs); color: var(--color-brand-primary);
      background: none; border: none; cursor: pointer; padding: 0;
      &:hover { text-decoration: underline; }
    }
    .sep { color: var(--color-text-muted); font-size: var(--font-size-xs); }

    .perm-group {
      border: 1px solid var(--color-border-default);
      border-radius: var(--radius-md); overflow: hidden;
    }
    .perm-group-header {
      display: flex; align-items: center; width: 100%;
      padding: var(--space-3) var(--space-4);
      background: var(--color-surface-raised); border: none; cursor: pointer;
      gap: var(--space-2); text-align: left;
      &:hover { background: var(--color-neutral-100); }
    }
    .perm-cat-label {
      flex: 1; font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary);
    }
    .perm-cat-count { font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .chevron {
      color: var(--color-text-muted);
      transition: transform var(--duration-fast);
      &--open { transform: rotate(180deg); }
    }

    .perm-rows { border-top: 1px solid var(--color-border-subtle); }
    .perm-row {
      display: flex; align-items: flex-start; gap: var(--space-3);
      padding: var(--space-2) var(--space-4); cursor: pointer;
      &:not(:last-child) { border-bottom: 1px solid var(--color-border-subtle); }
      &:hover { background: var(--color-neutral-50); }
    }
    .chk { accent-color: var(--color-brand-primary); width: 15px; height: 15px; cursor: pointer; margin-top: 2px; flex-shrink: 0; }
    .perm-info { display: flex; flex-direction: column; gap: 2px; }
    .perm-code { font-family: var(--font-family-mono); font-size: var(--font-size-xs); color: var(--color-text-primary); }
    .perm-desc { font-size: var(--font-size-xs); color: var(--color-text-muted); }
    .perm-row--disabled { opacity: 0.6; cursor: default; pointer-events: none; }

    .system-notice {
      display: flex; align-items: center; gap: var(--space-2);
      padding: var(--space-3) var(--space-4);
      background: var(--color-surface-raised);
      border: 1px solid var(--color-border-subtle);
      border-radius: var(--radius-md);
      font-size: var(--font-size-sm); color: var(--color-text-secondary);
      svg { flex-shrink: 0; color: var(--color-text-muted); }
    }

    .actions {
      display: flex; justify-content: flex-end; gap: var(--space-3);
      padding-top: var(--space-4); border-top: 1px solid var(--color-border-subtle);
    }
    .btn {
      display: inline-flex; align-items: center; gap: var(--space-2);
      height: 38px; padding: 0 var(--space-5); border-radius: var(--radius-md);
      font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); cursor: pointer; border: none;
      &:disabled { opacity: 0.55; cursor: not-allowed; }
    }
    .btn--ghost {
      border: 1px solid var(--color-border-default); background: transparent; color: var(--color-text-secondary);
      &:hover:not(:disabled) { background: var(--color-neutral-100); }
    }
    .btn--primary {
      background: var(--color-brand-primary); color: var(--color-brand-on-primary);
      &:hover:not(:disabled) { background: var(--color-brand-primary-hover); }
    }
  `],
})
export class RoleFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly rolesStore = inject(RolesStore);

  readonly mode        = input<'create' | 'edit'>('create');
  readonly initialData = input<RoleDto | null>(null);
  readonly saving      = input<boolean>(false);

  readonly submitted = output<CreateRoleRequest | UpdateRoleRequest>();
  readonly cancelled = output<void>();

  /** true si le rôle en cours d'édition est un rôle système (non modifiable) */
  readonly isSystemRole = computed(() => this.mode() === 'edit' && !!this.initialData()?.isSystem);

  form!: FormGroup;
  readonly selected = signal<Set<string>>(new Set());
  readonly openCats = signal<Set<string>>(new Set());

  /** Ouvre toutes les catégories dès que les permissions sont chargées (même après le rendu initial) */
  private readonly _openAllEffect = effect(() => {
    const groups = this.rolesStore.permissionGroups();
    if (groups.length > 0 && this.openCats().size === 0) {
      this.openCats.set(new Set(groups.map(g => g.category)));
    }
  });

  ngOnInit(): void {
    const d = this.initialData();
    this.form = this.fb.group({
      name: [d?.name ?? '', Validators.required],
      code: [d?.code ?? '',
        this.mode() === 'create'
          ? [Validators.required, Validators.pattern(/^[a-z0-9_]+$/)]
          : []],
      description: [d?.description ?? ''],
    });

    if (this.isSystemRole()) {
      this.form.disable();
    }

    this.selected.set(new Set(d?.permissions ?? []));

    // Load permissions catalogue if not yet loaded
    if (this.rolesStore.permissions().length === 0) this.rolesStore.load();

    // Open all categories now if already available (effect handles the late-load case)
    const groups = this.rolesStore.permissionGroups();
    if (groups.length > 0) {
      this.openCats.set(new Set(groups.map(g => g.category)));
    }
  }

  touched(ctrl: string): boolean {
    const c = this.form.get(ctrl);
    return !!(c?.invalid && (c.dirty || c.touched));
  }

  /* ── Category helpers ── */
  isCatOpen(cat: string): boolean { return this.openCats().has(cat); }
  toggleCat(cat: string): void {
    this.openCats.update(s => {
      const n = new Set(s);
      n.has(cat) ? n.delete(cat) : n.add(cat);
      return n;
    });
  }
  selectedInCat(cat: string): number {
    return this.rolesStore.permissionGroups()
      .find(g => g.category === cat)
      ?.permissions.filter(p => this.selected().has(p.code)).length ?? 0;
  }

  /* ── Permission toggle ── */
  togglePerm(code: string, e: Event): void {
    const chk = (e.target as HTMLInputElement).checked;
    this.selected.update(s => { const n = new Set(s); chk ? n.add(code) : n.delete(code); return n; });
  }
  selectAll(): void { this.selected.set(new Set(this.rolesStore.permissions().map(p => p.code))); }
  clearAll(): void  { this.selected.set(new Set()); }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const permissions = Array.from(this.selected());

    if (this.mode() === 'create') {
      this.submitted.emit({
        code: v.code, name: v.name,
        description: v.description || null, permissions,
      } satisfies CreateRoleRequest);
    } else {
      this.submitted.emit({
        name: v.name, description: v.description || null, permissions,
      } satisfies UpdateRoleRequest);
    }
  }
}
