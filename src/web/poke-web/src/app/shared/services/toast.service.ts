import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'danger' | 'warning' | 'info';

export interface Toast {
  id: string;
  message: string;
  type: ToastType;
  duration?: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);

  show(opts: { message: string; type?: ToastType; duration?: number }): void {
    const toast: Toast = {
      id: crypto.randomUUID(),
      message: opts.message,
      type: opts.type ?? 'info',
      duration: opts.duration ?? 4000,
    };
    this.toasts.update(ts => [...ts, toast]);

    if (toast.duration && toast.duration > 0) {
      setTimeout(() => this.dismiss(toast.id), toast.duration);
    }
  }

  dismiss(id: string): void {
    this.toasts.update(ts => ts.filter(t => t.id !== id));
  }

  success(message: string): void { this.show({ message, type: 'success' }); }
  error(message: string): void { this.show({ message, type: 'danger' }); }
  warning(message: string): void { this.show({ message, type: 'warning' }); }
  info(message: string): void { this.show({ message, type: 'info' }); }
}
