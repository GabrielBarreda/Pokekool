import {
  Directive,
  Input,
  TemplateRef,
  ViewContainerRef,
  inject,
  effect,
} from '@angular/core';
import { PermissionStore } from '@core/auth/permission.store';

/**
 * Structural directive — removes element from DOM if user lacks the permission.
 *
 * Usage:
 *   <button *hasPermission="'menus.write'">Créer</button>
 *
 * Multiple permissions (any match):
 *   <div *hasPermission="['menus.write', 'menus.delete']">...</div>
 */
@Directive({
  selector: '[hasPermission]',
  standalone: true,
})
export class HasPermissionDirective {
  private readonly templateRef  = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly permStore    = inject(PermissionStore);

  private _required: string[] = [];
  private _hasView = false;

  constructor() {
    // effect() doit être créé dans le contexte d'injection (constructeur)
    effect(() => {
      // Lecture du signal — re-évalue automatiquement à chaque changement de permissions
      this.permStore.permissions();
      this._updateView();
    });
  }

  @Input() set hasPermission(value: string | string[]) {
    this._required = Array.isArray(value) ? value : [value];
    this._updateView();
  }

  private _updateView(): void {
    const allowed =
      this._required.length === 0 ||
      this._required.some(p => this.permStore.permissions().includes(p));

    if (allowed && !this._hasView) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this._hasView = true;
    } else if (!allowed && this._hasView) {
      this.viewContainer.clear();
      this._hasView = false;
    }
  }
}
